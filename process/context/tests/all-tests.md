---
name: context:all-tests
description: "Frontend/backend verification commands and browser workflow coverage"
keywords: test, testing, verify, verification, Playwright, E2E, browser, build, smoke check
related: [context:all-uxui, context:all-container, context:all-database]
date: 16-11-26
---

# Tests Context

## Scope

This group covers available automated checks, build verification, browser-based workflow validation, and known test gaps.

## Read When

- Verifying a frontend or backend change.
- Running a browser workflow or diagnosing a UI issue.
- Deciding what can and cannot be proven by the repository's current tests.

## Quick Routing

- Frontend typecheck and production build: from `frontend/`, run `npm run build`.
- Backend compile: run `dotnet build backend/PhoneStore.sln` from repository root (requires .NET 8 SDK and restored packages).
- Interactive E2E: use Playwright against a disposable stack and isolated database. Do not mutate the shared/main database during exploratory tests.
- Frontend package scripts: `frontend/package.json`.

There is no frontend `test` script and no test spec/config directories were found in the initial repository scan. The frontend `build` script runs `vue-tsc --noEmit` followed by `vite build`; this checks types and bundling, not business workflows. No backend test project was found in the solution scan.

## Source Paths

- `frontend/package.json`
- `backend/PhoneStore.sln`
- `docker-compose.yml`
- `docs/HUONG_DAN_SU_DUNG.md`

## Update Triggers

Update this file when test scripts, test projects, browser automation setup, CI checks, or verification procedures change.

## Known Gaps

- Automated unit/integration/E2E test suites are not currently declared in the repository scan.
- Payment, SMTP delivery, and external integrations require sandbox or real credentials; mock UI outcomes do not prove external settlement/delivery.

