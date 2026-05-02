# Finance Tracker

A personal income/expense tracker. ASP.NET Core (.NET 10) backend with raw ADO.NET against PostgreSQL, Angular frontend, JWT auth via HttpOnly cookies.

## Prerequisites

- .NET 10 SDK
- Node.js 18+ and npm
- Docker (for local PostgreSQL)

## 1. Database

A PostgreSQL 16 container is used for both the dev database and the integration-test database (separate database names on the same instance).

```bash
docker run -d --name finance-tracker-postgres \
  -e POSTGRES_PASSWORD=postgres \
  -e POSTGRES_DB=finance_tracker_test \
  -p 5433:5432 \
  postgres:16

# Create the dev database alongside the test one:
docker exec finance-tracker-postgres psql -U postgres -c "CREATE DATABASE finance_tracker;"
```

Schema and seed data are applied automatically on backend startup. No manual migration step.

## 2. Backend

Configuration:

- `appsettings.json` holds the JWT issuer/audience and token lifetimes.
- `appsettings.Development.json` holds the local connection string.
- **`JWT__Secret` is read only from an environment variable** — never written to a config file. The app refuses to start without it (outside the `Testing` environment).

Run:

```bash
cd src/FinanceTracker.Api

# bash / WSL
export JWT__Secret="dev-only-secret-please-replace-with-32+chars"
dotnet run

# PowerShell
$env:JWT__Secret = "dev-only-secret-please-replace-with-32+chars"
dotnet run
```

The API listens on the URL printed by Kestrel (default `http://localhost:5xxx`). Swagger UI is at `/swagger`.

On first run the backend:
1. Applies the SQL migration (`001_InitialSchema.sql`).
2. Seeds a demo user and 10 sample transactions (idempotent — re-runs are no-ops if the demo user already exists).

### Demo credentials

| Email | Password |
|---|---|
| `demo@example.com` | `demo1234` |

## 3. Frontend

```bash
cd frontend
npm install
npm start
```

Runs on `http://localhost:4200`. The backend's CORS policy already allows this origin with credentials. All HTTP calls use `withCredentials: true` so the auth cookies attach automatically.

## 4. Running tests

```bash
# All tests
dotnet test

# Just unit tests (no DB required)
dotnet test tests/FinanceTracker.Application.Tests
dotnet test tests/FinanceTracker.Api.Tests

# Integration tests (requires PostgreSQL container running on port 5433)
dotnet test tests/FinanceTracker.Infrastructure.Tests
```

Test counts: 48 Application + 31 Infrastructure + 27 API = 106.

## API surface

### Auth (unauthenticated)

| Method | Path | Notes |
|---|---|---|
| POST | `/register` | Body: `{ name, email, password }`. Returns 201, sets `access_token` + `refresh_token` cookies. |
| POST | `/login` | Body: `{ email, password }`. Returns 200, sets cookies. |

### Auth (cookie-driven; no `Authorization` header needed)

| Method | Path | Notes |
|---|---|---|
| POST | `/refresh` | Reads `refresh_token` cookie, rotates both tokens. |
| POST | `/logout` | Reads `refresh_token` cookie, revokes it, clears both cookies. |

### User (`[Authorize]`)

| Method | Path | Notes |
|---|---|---|
| GET | `/user` | Returns the current user. |
| PUT | `/update` | Body: `{ name?, email?, currentPassword?, newPassword? }`. Email change checks for conflicts. Password change requires `currentPassword`. |

### Transactions (`[Authorize]`)

| Method | Path | Notes |
|---|---|---|
| POST | `/transaction` | Body: `{ title, amount, category, date? }`. `category` is `income` or `expense`. Returns 201 + the created DTO. |
| PUT | `/transaction` | Body: `{ transactionId, title, amount, category, date? }`. |
| GET | `/transactions` | Query: `page`, `pageSize`, `sortBy`, `sortOrder`, `category`, `dateFrom`, `dateTo`. Returns a `PagedResult`. |
| DELETE | `/transaction?id={id}` | Soft-delete. Returns 204. |
| GET | `/transactions/summary` | Returns `{ netBalance, totalIncome, totalExpense }`. |

All errors flow through `ExceptionHandlingMiddleware` and come back as RFC 7807 `ProblemDetails` JSON with the appropriate status code (400 / 401 / 404 / 409 / 500).

## Architecture

Clean Architecture with four layers:

- **Domain** — entities and repository interfaces. Zero NuGet dependencies.
- **Application** — services, DTOs, validation, domain exceptions. Depends only on Domain.
- **Infrastructure** — ADO.NET repositories (`Npgsql`), `BCrypt`-based password hashing, JWT issuance, migration runner, seeder.
- **API** — controllers, middleware, DI wiring.

### Auth flow

- Access token: 15-minute lifetime, JWT, signed HS256.
- Refresh token: 5-day lifetime, opaque base64 random, stored in DB, **single-use** (rotated and revoked on every `/refresh`).
- Both tokens delivered as `HttpOnly` + `Secure` + `SameSite=Strict` cookies. JavaScript cannot read them — eliminates XSS token theft.
- A small `CookieTokenMiddleware` copies the `access_token` cookie into an `Authorization: Bearer` header so the standard `JwtBearer` handler can validate it. This keeps the auth stack stock ASP.NET Core while still using cookies on the wire.

### Error handling

Global `ExceptionHandlingMiddleware` registered in the API layer. Controllers contain only happy-path logic — no try/catch.

- Domain exceptions live in the Application layer: `NotFoundException`, `ConflictException`, `ValidationException`, `UnauthorizedException`.
- Services throw the typed exception that fits the failure (e.g. duplicate email → `ConflictException`).
- The middleware maps each one to an HTTP status and writes a `ProblemDetails` body. Anything else becomes 500.
- One place to change error mapping; controllers stay readable; clients get a consistent error shape.
