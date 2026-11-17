---
name: context:all-container
description: "Docker Compose services, ports, volumes, and local runtime operations"
keywords: Docker, Docker Compose, container, compose, ports, volumes, local runtime, MinIO, PostgreSQL
related: [context:all-database, context:all-tests]
date: 16-11-26
---

# Container Context

## Scope

This group covers the local Docker Compose stack, service dependencies, exposed ports, volumes, and operational commands. It does not cover production deployment procedures, which are not documented in the repository.

## Read When

- Starting or stopping the local PhoneStore stack.
- Diagnosing container health, port conflicts, or service connectivity.
- Changing Dockerfiles, Compose services, ports, or persistent volumes.

## Quick Routing

- Start/rebuild: `docker compose up -d --build`
- Inspect: `docker compose ps` and `docker compose logs -f api`
- Stop services while preserving data: `docker compose down`
- Default ports and volumes: `docker-compose.yml`

The Compose file defines PostgreSQL 16, MinIO, ASP.NET API, and frontend services. Defaults include web 5174, API 8081, database 5433, and MinIO 9002/9003. Check for port conflicts before startup; use an isolated Compose project and distinct ports for disposable browser testing.

## Source Paths

- `docker-compose.yml`
- `backend/Dockerfile`
- `frontend/Dockerfile`
- `.env.example`

## Update Triggers

Update this entrypoint when services, port mappings, volume persistence, health checks, or local runtime commands change.

