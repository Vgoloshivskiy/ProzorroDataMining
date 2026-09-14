using Dapper;
using Microsoft.Extensions.Logging;
using ProzorroDataMining.Application.ApplicationContracts;
using ProzorroDataMining.Core.Entities;
using ProzorroDataMining.Core.Entities.DTOs;
using ProzorroDataMining.Application.RepositoryContracts;
using ProzorroDataMining.Infrastructure.DbContext;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace ProzorroDataMining.Infrastructure.Repositories
{
    using Dapper;
    using Npgsql;
    using System.Data;

    public class TenderRepository : ITenderRepository
    {
        private readonly DapperDbContext _dbContext;

        public TenderRepository(DapperDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task UpsertTendersAsync(
            IReadOnlyCollection<TenderImportModel> tenders,
            CancellationToken cancellationToken = default)
        {
            if (tenders == null || tenders.Count == 0)
            {
                return;
            }

            await using var connection = _dbContext.DbConnection;
            await connection.OpenAsync(cancellationToken);

            await using (var transaction = await connection.BeginTransactionAsync(
                cancellationToken))
            {
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        @"
                    CREATE TEMP TABLE tmp_tender
                    (
                        external_id VARCHAR(32) NOT NULL,
                        cpv_code VARCHAR(20),
                        status VARCHAR(50),
                        procuring_entity_name VARCHAR,
                        starting_amount NUMERIC(19, 4),
                        contract_total NUMERIC(19, 4),
                        savings NUMERIC(19, 4),
                        data_hash VARCHAR(64),
                        date_created timestamptz,
                        date_modified timestamptz
                    ) ON COMMIT DROP;

                    CREATE TEMP TABLE tmp_business_organisation
                    (
                        tender_external_id VARCHAR(32) NOT NULL,
                        name VARCHAR NOT NULL
                    ) ON COMMIT DROP;
                    ",
                        transaction: transaction,
                        cancellationToken: cancellationToken));

                await CopyTendersAsync(
                    connection,
                    transaction,
                    tenders,
                    cancellationToken);

                await CopyBusinessOrganisationsAsync(
                    connection,
                    transaction,
                    tenders,
                    cancellationToken);

                await connection.ExecuteAsync(
                    new CommandDefinition(
                        @"
                    INSERT INTO procuring_entity (name)
                    SELECT DISTINCT procuring_entity_name
                    FROM tmp_tender
                    WHERE procuring_entity_name IS NOT NULL
                      AND procuring_entity_name <> ''
                    ON CONFLICT (name) DO NOTHING;
                    ",
                        transaction: transaction,
                        cancellationToken: cancellationToken));

                await connection.ExecuteAsync(
                    new CommandDefinition(
                        @"
                    INSERT INTO business_organisation (name)
                    SELECT DISTINCT name
                    FROM tmp_business_organisation
                    ON CONFLICT (name) DO NOTHING;
                    ",
                        transaction: transaction,
                        cancellationToken: cancellationToken));

                await connection.ExecuteAsync(
                    new CommandDefinition(
                        @"
                    INSERT INTO tender
                    (
                        external_id,
                        cpv_code,
                        status,
                        procuring_entity_id,
                        starting_amount,
                        contract_total,
                        savings,
                        data_hash,
                        date_created,
                        date_modified
                    )
                    SELECT
                        t.external_id,
                        t.cpv_code,
                        t.status,
                        pe.id,
                        t.starting_amount,
                        t.contract_total,
                        t.savings,
                        t.data_hash,
                        date_trunc('second', t.date_created),
                        date_trunc('second', t.date_modified)
                    FROM (
                        SELECT DISTINCT ON (external_id)
                            external_id,
                            cpv_code,
                            status,
                            procuring_entity_name,
                            starting_amount,
                            contract_total,
                            savings,
                            data_hash,
                            date_created,
                            date_modified
                        FROM tmp_tender
                        ORDER BY external_id, date_modified DESC NULLS LAST
                    ) t
                    LEFT JOIN procuring_entity pe
                        ON pe.name = t.procuring_entity_name
                    ON CONFLICT (external_id)
                    DO UPDATE SET
                        cpv_code = EXCLUDED.cpv_code,
                        status = EXCLUDED.status,
                        procuring_entity_id = EXCLUDED.procuring_entity_id,
                        starting_amount = EXCLUDED.starting_amount,
                        contract_total = EXCLUDED.contract_total,
                        savings = EXCLUDED.savings,
                        data_hash = EXCLUDED.data_hash,
                        date_created = date_trunc('second', EXCLUDED.date_created),
                        date_modified = date_trunc('second', EXCLUDED.date_modified)
                    WHERE EXCLUDED.data_hash IS DISTINCT FROM tender.data_hash;
                    ",
                        transaction: transaction,
                        cancellationToken: cancellationToken));

                await connection.ExecuteAsync(
                    new CommandDefinition(
                        @"
                    INSERT INTO tender_business_organisation
                    (
                        tender_id,
                        business_organisation_id
                    )
                    SELECT
                        t.id,
                        bo.id
                    FROM tmp_business_organisation tmp
                    INNER JOIN tender t
                        ON t.external_id = tmp.tender_external_id
                    INNER JOIN business_organisation bo
                        ON bo.name = tmp.name
                    ON CONFLICT DO NOTHING;
                    ",
                        transaction: transaction,
                        cancellationToken: cancellationToken));

                await transaction.CommitAsync(cancellationToken);
            }
            // ensure connection disposed/closed by awaiting its disposal
        }

        private async Task CopyTendersAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            IReadOnlyCollection<TenderImportModel> tenders,
            CancellationToken cancellationToken)
        {
            using (var writer = await connection.BeginBinaryImportAsync(
                @"
            COPY tmp_tender
            (
                external_id,
                cpv_code,
                status,
                procuring_entity_name,
                starting_amount,
                contract_total,
                savings,
                data_hash,
                date_created,
                date_modified
            )
            FROM STDIN (FORMAT BINARY)
            ",
                cancellationToken))
            {
                foreach (var tender in tenders)
                {
                    await writer.StartRowAsync(cancellationToken);

                    await writer.WriteAsync(
                        tender.ExternalId,
                        NpgsqlTypes.NpgsqlDbType.Varchar,
                        cancellationToken);

                    await WriteNullableAsync(
                        writer,
                        tender.CPVCode,
                        NpgsqlTypes.NpgsqlDbType.Varchar,
                        cancellationToken);

                    await WriteNullableAsync(
                        writer,
                        tender.Status,
                        NpgsqlTypes.NpgsqlDbType.Varchar,
                        cancellationToken);

                    await WriteNullableAsync(
                        writer,
                        tender.ProcuringEntityName,
                        NpgsqlTypes.NpgsqlDbType.Varchar,
                        cancellationToken);

                    await WriteNullableAsync(
                        writer,
                        tender.StartingAmount,
                        NpgsqlTypes.NpgsqlDbType.Numeric,
                        cancellationToken);

                    await WriteNullableAsync(
                        writer,
                        tender.ContractTotal,
                        NpgsqlTypes.NpgsqlDbType.Numeric,
                        cancellationToken);

                    await WriteNullableAsync(
                        writer,
                        tender.Savings,
                        NpgsqlTypes.NpgsqlDbType.Numeric,
                        cancellationToken);

                    await WriteNullableAsync(
                        writer,
                        tender.DataHash,
                        NpgsqlTypes.NpgsqlDbType.Varchar,
                        cancellationToken);

                    await WriteNullableAsync(
                        writer,
                        tender.DateCreated,
                        NpgsqlTypes.NpgsqlDbType.TimestampTz,
                        cancellationToken);

                    await WriteNullableAsync(
                        writer,
                        tender.DateModified,
                        NpgsqlTypes.NpgsqlDbType.TimestampTz,
                        cancellationToken);
                }

                await writer.CompleteAsync(cancellationToken);
            }
        }

        public async Task<DateTimeOffset?> GetTenderDateModifiedAsync(string externalId, CancellationToken cancellationToken = default)
        {
            var sql = @"
SELECT date_modified FROM tender WHERE external_id = @ExternalId LIMIT 1;";

            using var conn = _dbContext.DbConnection;
            await conn.OpenAsync(cancellationToken);

            var result = await conn.QueryFirstOrDefaultAsync<DateTime?>(sql, new { ExternalId = externalId });

            if (result.HasValue)
            {
                // Interpret DB timestamptz as UTC and truncate to seconds to match mapper normalization
                var dt = DateTime.SpecifyKind(result.Value, DateTimeKind.Utc);
                var truncated = new DateTime(dt.AddTicks(-(dt.Ticks % TimeSpan.TicksPerSecond)).Ticks, DateTimeKind.Utc);
                return new DateTimeOffset(truncated);
            }

            return null;
        }

        private async Task CopyBusinessOrganisationsAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            IReadOnlyCollection<TenderImportModel> tenders,
            CancellationToken cancellationToken)
        {
            using (var writer = await connection.BeginBinaryImportAsync(
                @"
            COPY tmp_business_organisation
            (
                tender_external_id,
                name
            )
            FROM STDIN (FORMAT BINARY)
            ",
                cancellationToken))
            {
                foreach (var tender in tenders)
                {
                    if (tender.BusinessOrganisationNames == null)
                    {
                        continue;
                    }

                    foreach (var name in tender.BusinessOrganisationNames)
                    {
                        if (string.IsNullOrWhiteSpace(name))
                        {
                            continue;
                        }

                        await writer.StartRowAsync(cancellationToken);

                        await writer.WriteAsync(
                            tender.ExternalId,
                            NpgsqlTypes.NpgsqlDbType.Varchar,
                            cancellationToken);

                        await writer.WriteAsync(
                            name,
                            NpgsqlTypes.NpgsqlDbType.Varchar,
                            cancellationToken);
                    }
                }

                await writer.CompleteAsync(cancellationToken);
            }
        }

        private async Task WriteNullableAsync<T>(
            NpgsqlBinaryImporter writer,
            T value,
            NpgsqlTypes.NpgsqlDbType type,
            CancellationToken cancellationToken)
        {
            if (value == null)
            {
                await writer.WriteNullAsync(cancellationToken);
                return;
            }

            await writer.WriteAsync(
                value,
                type,
                cancellationToken);
        }

        public async Task<IReadOnlyCollection<ProzorroDataMining.Application.RepositoryContracts.TenderListItemDto>> GetTenderListAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 100;

            var sql = @"
SELECT t.external_id AS ExternalId, COALESCE(pe.name, '') AS Name
FROM tender t
LEFT JOIN procuring_entity pe ON pe.id = t.procuring_entity_id
ORDER BY t.id DESC
OFFSET @Offset ROWS
LIMIT @Limit;";

            var offset = (page - 1) * pageSize;

            using var conn = _dbContext.DbConnection;
            await conn.OpenAsync(cancellationToken);

            var rows = await conn.QueryAsync<ProzorroDataMining.Application.RepositoryContracts.TenderListItemDto>(sql, new { Offset = offset, Limit = pageSize });
            return rows.AsList();
        }
    }
}
