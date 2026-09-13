# ProzorroDataMining

ProzorroDataMining is a small data ingestion and analytics service that pulls procurement data from the public ProZorro API, stores it in PostgreSQL and exposes analytic endpoints. The project follows a Clean/Onion architecture (Core / Application / Infrastructure / API) and uses Dapper for fast bulk writes.

This README documents how to build and run the project, the core design decisions, and recommendations for operating at scale (millions of tenders).

## Projects
- ProzorroDataMining.Api — ASP.NET Core Web API, controllers and middleware
- ProzorroDataMining.Application — application services and repository contracts
- ProzorroDataMining.Infrastructure — implementations, data access, HttpClients, DI
- ProzorroDataMining.Core — domain DTOs and mappers

## Run (Docker Compose)
Prerequisites: Docker Desktop.

1. Build and start services:

   docker compose up --build

   The docker-compose file starts two services:
   - prozorrodatamining.api — the API service
   - postgresdb — PostgreSQL used for storage

2. The database init SQL is mounted into Postgres and runs on first startup to create the schema.

3. API endpoints

   - POST /api/datasync/refresh — trigger ingestion (synchronization)
   - GET /api/analytics/tender/{externalId}/savings — budget savings for a tender
   - GET /api/analytics/top/procuring-entities?top=5 — top procuring entities by contract_total
   - GET /api/analytics/top/suppliers?top=5 — top suppliers by contract_total
   - GET /api/tenders?page=1&pageSize=100 — paginated list of tender external ids and names

## Design notes and metrics

1. Database: Efficiency of structure and queries

   - Schema uses normalized tables: `tender`, `procuring_entity`, `business_organisation`, `tender_business_organisation`.
   - Important columns and types:
	 - `tender.external_id` VARCHAR(32) (unique)
	 - `tender.starting_amount`, `tender.contract_total`, `tender.savings` as NUMERIC(19,4)
	 - `tender.data_hash` VARCHAR(64) — SHA256 hex digest of key tender payload fields used to skip unchanged updates.
   - Indexes:
	- Unique index on `external_id`
   - Indexes on `procuring_entity_id` and on the mapping table for supplier aggregation.
   - Note: indexes on `cpv_code` and `status` were removed from the DB initialization script because the application intentionally filters out tender rows where `status = 'complete'` and where `cpv_code = '09310000-5'`. Maintaining btree indexes for columns that are commonly excluded with inequality filters (status != ... or cpv_code != ...) caused high insert overhead without providing useful selectivity for our workload; removing these indexes improves bulk insert throughput. If you later need fast point-lookup or range queries on these columns, consider adding targeted partial indexes that match the query predicates.
   - Aggregations use server-side SQL and Dapper; queries are written to use indexes where possible. For very large datasets (millions of rows) consider:
	 - Partitioning the `tender` table by date range (monthly/yearly) if queries filter by date.
	 - Adding indexes tailored to common query predicates (e.g., contract_total ranges, date fields) and using EXPLAIN ANALYZE to refine.

2. Code Quality

   - Layered architecture: Controllers call Application services which rely on repository contracts implemented in Infrastructure.
   - Naming is kept consistent and small helper types (DTOs) are used for cross-layer contracts.
   - README and docker-compose are included for local setup.

3. Concurrency

   - HttpClientFactory is used for outgoing HTTP requests.
   - Polly policies and a RateLimitHandler are configured to respect the external API limits and apply sensible retry/backoff.

4. Architecture

   - Clean/Onion style separation:
	 - ProzorroDataMining.Core — domain DTOs and mapping logic (the innermost layer)
	 - ProzorroDataMining.Application — application services and repository contracts (use-cases)
	 - ProzorroDataMining.Infrastructure — concrete implementations (data access, HttpClients) and DI bootstrapping
	 - ProzorroDataMining.Api — ASP.NET Core Web API, controllers and middleware (outermost layer)

   - The Infrastructure project registers concrete services in DependencyInjections.cs and implements repository contracts defined in the Application project. This keeps domain and application logic free of framework concerns and allows easier testing and substitution of implementations.

5. Error handling

   - Network errors are retried with exponential backoff; 429 responses are handled with respect to Retry-After where possible.
   - Incoming JSON is validated in mapping code; the mapper returns null for missing/invalid inputs which callers handle.

## Tests

- Unit tests are included under `ProzorroDataMining.Api.Tests` (NUnit + Moq).

