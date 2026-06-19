# Oryxen Backend

RESTful Web API for the **Oryxen** smart plant-care platform, built with **ASP.NET Core 9** and
**Entity Framework Core 9** over **PostgreSQL**, following a Clean Architecture / DDD layout.

## Solution structure

```
Oryxen-Backend/
├─ docker-compose.yml          # local PostgreSQL 15 container
├─ .env                        # Postgres credentials (git-ignored)
├─ Oryxen.API.slnx
└─ src/
   ├─ Oryxen.API/              # Controllers, JWT auth, Swagger, middleware (composition root)
   ├─ Oryxen.Application/      # Services, DTOs, interfaces (use cases)
   ├─ Oryxen.Domain/           # Entities, value objects, domain services, repository contracts
   └─ Oryxen.Infrastructure/   # EF Core DbContext, repositories, BCrypt, JWT generator
```

Dependency rule: `API → Infrastructure → Application → Domain`. The Domain layer has no dependencies.

## Prerequisites

- .NET SDK 9.0
- Docker Desktop (for PostgreSQL)
- EF Core CLI: `dotnet tool install --global dotnet-ef`

## Getting started (localhost)

```bash
# 1. Start PostgreSQL
docker compose up -d

# 2. Run the API (applies EF migrations automatically on startup)
dotnet run --project src/Oryxen.API

# 3. Open Swagger
#    http://localhost:5170/swagger
```

To create/apply migrations manually:

```bash
dotnet ef migrations add <Name> --project src/Oryxen.Infrastructure --startup-project src/Oryxen.API
dotnet ef database update            --project src/Oryxen.Infrastructure --startup-project src/Oryxen.API
```

## Sprint 1 endpoints

| Method | Route                       | Auth            | Description                                    |
|--------|-----------------------------|-----------------|------------------------------------------------|
| POST   | `/api/v1/auth/register`     | Anonymous       | Register account (FARMER role + Freemium plan) |
| POST   | `/api/v1/auth/login`        | Anonymous       | Login with email/password → JWT pair           |
| POST   | `/api/v1/auth/refresh`      | Anonymous       | Rotate refresh token → new JWT pair            |
| GET    | `/api/v1/auth/me`           | Bearer          | Current user identity claims                   |
| POST   | `/api/v1/telemetry`         | Anonymous*      | Ingest a Sensor Lite reading (+ Health Score)  |
| GET    | `/api/v1/telemetry/{plantId}` | FARMER / ADMIN | Recent telemetry history for a plant           |

\* Open for the Sprint 1 local IoT simulator; production will use per-device API keys.

## Security

- Passwords hashed with **BCrypt** (work factor 12).
- **JWT** access tokens signed with HS256; **RBAC** roles `FARMER`, `ADMIN`, `SUPPORT_TECHNICIAN`.
- Refresh tokens stored only as **SHA-256** hashes and rotated on every use.
