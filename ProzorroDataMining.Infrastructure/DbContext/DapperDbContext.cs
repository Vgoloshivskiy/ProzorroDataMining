using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace ProzorroDataMining.Infrastructure.DbContext
{
    public class DapperDbContext
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public DapperDbContext(IConfiguration configuration)
        {
            _configuration = configuration;

            string connectionStringTemplate =
                _configuration.GetConnectionString("PstgresConnection")!;

            _connectionString = connectionStringTemplate
                .Replace(
                    "$POSTGRES_HOST",
                    Environment.GetEnvironmentVariable("POSTGRES_HOST"))
                .Replace(
                    "$POSTGRES_PORT",
                    Environment.GetEnvironmentVariable("POSTGRES_PORT"))
                .Replace(
                    "$POSTGRES_DATABASE",
                    Environment.GetEnvironmentVariable("POSTGRES_DATABASE"))
                .Replace(
                    "$POSTGRES_USER",
                    Environment.GetEnvironmentVariable("POSTGRES_USER"))
                .Replace(
                    "$POSTGRES_PASSWORD",
                    Environment.GetEnvironmentVariable("POSTGRES_PASSWORD"));
        }

        // Return a new connection instance per call. Callers should open and dispose it.
        public NpgsqlConnection DbConnection => new NpgsqlConnection(_connectionString);
    }
}
