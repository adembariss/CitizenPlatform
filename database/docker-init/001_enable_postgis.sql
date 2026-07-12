-- Runs automatically via docker-entrypoint-initdb.d on first container start,
-- before any EF Core migration has created tables. Only extension setup belongs
-- here; index/seed scripts in database/main-db must run after `dotnet ef database
-- update` because they reference tables that migrations create.
CREATE EXTENSION IF NOT EXISTS postgis;
