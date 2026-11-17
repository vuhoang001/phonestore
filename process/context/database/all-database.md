---
name: context:all-database
description: "EF Core, PostgreSQL, schema, seeding, and data persistence context"
keywords: database, postgres, PostgreSQL, EF Core, entity, migration, seeding, schema, persistence
related: [context:all-container, context:all-tests]
date: 16-11-26
---

# Database Context

## Scope

This group covers PostgreSQL persistence, EF Core entities/configuration, database seeding, and schema changes. It does not cover feature-specific product behavior or Docker operations beyond database connection details.

## Read When

- Changing entities, relationships, indexes, or database configuration.
- Investigating seed data, persistence, or schema startup behavior.
- Designing isolated test data or database environments.

## Quick Routing

- EF Core and domain entities: `backend/src/PhoneStore.Domain/` and `backend/src/PhoneStore.Infrastructure/`
- DbContext and entity configuration: search `backend/src/PhoneStore.Infrastructure/` for `DbContext`, `EntityTypeConfiguration`, and `Migrations`
- Database connection and service configuration: `backend/src/PhoneStore.API/Program.cs`, `docker-compose.yml`, `.env.example`

## Source Paths

- `backend/src/PhoneStore.Domain/`
- `backend/src/PhoneStore.Infrastructure/`
- `backend/src/PhoneStore.API/`
- `docker-compose.yml`

## Update Triggers

Update this entrypoint when the EF Core version, provider, migration procedure, seed strategy, or database service configuration changes.

