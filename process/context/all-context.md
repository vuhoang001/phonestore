---
name: context:all-context
description: "Repository router and source-of-truth context for PhoneStore"
keywords: repository, architecture, stack, frontend, backend, product, admin, customer, order, IMEI
related: [context:all-tests, context:all-database, context:all-container, context:all-uxui, context:all-planning]
date: 16-11-26
---
# All Context

Last updated: 2026-11-16. Last scanned: 2026-11-16. Scan mode: full scan. Repository HEAD: see `git rev-parse HEAD` (worktree contains uncommitted changes).

PhoneStore is a phone and accessories e-commerce website. It has customer workflows for discovering and comparing devices, cart, checkout, payments, account, orders, trade-in, and warranty lookup; admin workflows for catalog, orders, shipping, promotions, trade-in, and reports. IMEI/serial tracking is an intentional part of order fulfillment and warranty lookup.

## Quick Start

1. Read this file first for repository routing.
2. Load only the relevant context group from the table below.
3. For a user-facing walkthrough, read `docs/HUONG_DAN_SU_DUNG.md`.

<!-- GENERATED:routing -->
| File | Read when |
|---|---|
| `process/context/all-context.md` | any substantial planning, research, review, or implementation task |
| `process/context/container/all-container.md` | Docker Compose services, ports, volumes, and local runtime operations |
| `process/context/database/all-database.md` | EF Core, PostgreSQL, schema, seeding, and data persistence context |
| `process/context/planning/all-planning.md` | Project planning artifact locations and lifecycle |
| `process/context/tests/all-tests.md` | Frontend/backend verification commands and browser workflow coverage |
| `process/context/uxui/all-uxui.md` | Vue customer/admin screens, shared components, navigation, and styling |

## Current Context Groups

| Group | Entry point | Scope |
|---|---|---|
| `container/` | `process/context/container/all-container.md` | Docker Compose services, ports, volumes, and local runtime operations |
| `database/` | `process/context/database/all-database.md` | EF Core, PostgreSQL, schema, seeding, and data persistence context |
| `planning/` | `process/context/planning/all-planning.md` | Project planning artifact locations and lifecycle |
| `tests/` | `process/context/tests/all-tests.md` | Frontend/backend verification commands and browser workflow coverage |
| `uxui/` | `process/context/uxui/all-uxui.md` | Vue customer/admin screens, shared components, navigation, and styling |
<!-- /GENERATED:routing -->

## Task Routing

| If the task involves... | Read next |
|---|---|
| Architecture or stack | this file, then relevant source paths below |
| Browser verification or build checks | `process/context/tests/all-tests.md` |
| Data model, EF Core, database | `process/context/database/all-database.md` |
| Local services, ports, Docker | `process/context/container/all-container.md` |
| Customer/admin screens and routes | `process/context/uxui/all-uxui.md` |
| End-user feature walkthrough | `docs/HUONG_DAN_SU_DUNG.md` |

## Repository Structure

```text
backend/
  PhoneStore.sln
  src/PhoneStore.Domain/          # Domain entities and enums
  src/PhoneStore.Application/     # DTOs, interfaces, services
  src/PhoneStore.Infrastructure/  # EF Core, persistence, integrations
  src/PhoneStore.API/             # ASP.NET controllers and host
frontend/
  src/components/                 # Shared Vue components
  src/layouts/                    # Customer/admin layouts
  src/router/                     # Route map and access guards
  src/services/                   # API clients
  src/stores/                     # Pinia state
  src/views/                      # Customer/auth/admin screens
process/context/                  # Durable repository context
process/development-protocols/    # Shared RIPER workflow protocols
docs/                             # Project and user documentation
docker-compose.yml                # PostgreSQL, MinIO, API, web
```

## Technology Stack

- Frontend: Vue 3.5, TypeScript 5.6, Vite 5.4, PrimeVue 4, Pinia 2, Vue Router 4, Axios, Chart.js.
- Backend: .NET 8 / ASP.NET Core with Clean Architecture projects (Domain, Application, Infrastructure, API).
- Persistence: EF Core 8 with PostgreSQL 16.
- Integrations: JWT auth, MinIO image storage, SignalR notifications, VNPay sandbox/mock, configurable email provider.
- Local runtime: Docker Compose services for PostgreSQL, MinIO, API, and frontend.
- Frontend package manager: npm (`frontend/package-lock.json`).

## Key Patterns and Conventions

- Frontend routes are centralized in `frontend/src/router/index.ts`; views load lazily.
- `requiresAuth` routes redirect unauthenticated users to login with a `redirect` query. `requiresAdmin` routes also require admin role.
- Frontend state uses Pinia; API calls are organized under `frontend/src/services/`.
- Backend follows Domain / Application / Infrastructure / API boundaries.
- Product variants include color and storage; order fulfillment associates device IMEI/serial and warranty records.
- Environment values are supplied through `.env` and Compose variable interpolation; do not commit secrets.

## Environment and Configuration

See `.env.example` and `docker-compose.yml`. Configuration groups include database connection, MinIO credentials/endpoints, JWT issuer/audience/secret, CORS origins, seeded admin account, VNPay sandbox values and return URLs, order expiry, and SMTP/email provider. This context lists names/categories only and does not include secret values.

## Tests and Quality

See `process/context/tests/all-tests.md`. The frontend declares a typecheck-plus-build script but no test script. Use isolated test data for any workflow that creates orders, changes status, or assigns IMEIs.

## Context Group Lifecycle

Context groups are durable knowledge domains, not feature folders. Create or split a group when it has at least three durable documents, a large document has distinct subtopics, or agents repeatedly need one stable slice. Keep temporary reports and implementation plans in their designated plan/task folders. Regenerate routing from group frontmatter and run `vc-audit-context` after changing context organization.

## Key References

- `README.md` - stack, run instructions, demo account.
- `frontend/src/router/index.ts` - customer, auth, and admin route map.
- `frontend/package.json` - frontend commands and dependencies.
- `docker-compose.yml` and `.env.example` - local infrastructure/configuration.
- `backend/PhoneStore.sln` - backend solution.
- `docs/HUONG_DAN_SU_DUNG.md` - step-by-step customer/admin guide.
- `process/development-protocols/all-development-protocols.md` - project workflow rules.

## Open Questions

- Production deployment target and production payment/email configuration are not documented in the repository.
- The repository has no dedicated automated test suite in the initial scan; browser coverage should be recorded per workflow and environment.

