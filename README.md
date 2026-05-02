# Finance Tracker

Personal income and expense tracker built with an ASP.NET Core backend, raw ADO.NET against PostgreSQL, and an Angular frontend. Authentication uses JWT access and refresh tokens delivered through HttpOnly cookies.

## How to Run the Application

### Requirements

Install these before running the app locally:

- Git
- Docker Desktop
- .NET 10 SDK
- Node.js 18+ and npm
- PowerShell, Windows Terminal, or another terminal that can run the commands below

### 1. Clone the repository

```powershell
git clone <repository-url>
cd finance-tracker
```

Replace `<repository-url>` with the GitHub URL for this repository.

### 2. Install dependencies

From the repository root:

```powershell
npm run setup
```

This installs the root development tools and the Angular frontend dependencies.

### 3. Start the app

From the repository root:

```powershell
npm run dev
```

This single command:

- Starts PostgreSQL with Docker Compose.
- Waits for the database port to be available.
- Starts the ASP.NET Core backend.
- Starts the Angular frontend.

The database container is named `finance-tracker-db` and runs on local port `5433`.

The backend automatically applies the SQL migration and seeds demo data when it starts. No manual migration command is required.

Backend URL:

```text
http://localhost:5283
```

Swagger:

```text
http://localhost:5283/swagger
```

Frontend URL:

```text
http://localhost:4200
```

The frontend is configured to call the backend at `http://localhost:5283`.

Development configuration already includes a local JWT secret in `src/FinanceTracker.Api/appsettings.Development.json`. For non-development environments, configure `JWT__Secret` as an environment variable.

To stop the database container:

```powershell
npm run db:stop
```

### Demo Seeded User

Use these credentials to try the app without creating a new account:

```text
email: demo@example.com
password: demo1234
```

## Tracking the Database, Records, and Tables

Use `psql` inside the running Docker container to inspect the database.

Run on PowerShell:

```powershell
docker exec -it finance-tracker-db psql -U postgres -d finance_tracker
```

Useful `psql` commands:

```sql
\dt+ -- Shows existing tables
\d -- Lists relations in the current database
\d users -- Shows column types, indexes, and table details for users
\d transactions -- Shows column types, indexes, and table details for transactions
\d refresh_tokens -- Shows column types, indexes, and table details for refresh_tokens
\q -- Quit psql
```

Useful SQL queries:

```sql
SELECT * FROM users;
SELECT * FROM transactions;
SELECT * FROM refresh_tokens;
```

Main application tables:

| Table | Purpose |
|---|---|
| `users` | Stores registered users, emails, and password hashes. |
| `transactions` | Stores income and expense records. Deleted transactions are soft-deleted with the `deleted` column. |
| `refresh_tokens` | Stores refresh tokens used by the HttpOnly cookie auth flow. |

## Running Tests

From the repository root:

```powershell
dotnet test
```

The infrastructure tests use a separate database named `finance_tracker_test`. Create it once if it does not exist yet:

```powershell
docker exec -it finance-tracker-db createdb -U postgres finance_tracker_test
```

Run specific backend test projects:

```powershell
dotnet test tests/FinanceTracker.Application.Tests
dotnet test tests/FinanceTracker.Api.Tests
dotnet test tests/FinanceTracker.Infrastructure.Tests
```

Run frontend tests:

```powershell
cd frontend
npm test
```

## API Surface

### Auth

| Method | Path | Notes |
|---|---|---|
| POST | `/register` | Creates a user and sets `access_token` and `refresh_token` cookies. |
| POST | `/login` | Authenticates a user and sets cookies. |
| POST | `/refresh` | Rotates the refresh token and issues a new access token. |
| POST | `/logout` | Revokes the refresh token and clears cookies. |

### User

| Method | Path | Notes |
|---|---|---|
| GET | `/user` | Returns the current authenticated user. |
| PUT | `/update` | Updates name, email, or password. |

### Transactions

| Method | Path | Notes |
|---|---|---|
| POST | `/transaction` | Creates a transaction. |
| PUT | `/transaction` | Updates a transaction. |
| GET | `/transactions` | Returns paginated, filterable, sortable transactions. |
| DELETE | `/transaction?id={id}` | Soft-deletes a transaction. |
| GET | `/transactions/summary` | Returns net balance, total income, and total expense. |

## Architecture

Clean Architecture with four backend layers:

- **Domain**: entities and repository interfaces.
- **Application**: services, DTOs, validation, and domain exceptions.
- **Infrastructure**: ADO.NET repositories, Npgsql, BCrypt password hashing, JWT issuance, migrations, and seeding.
- **API**: controllers, middleware, dependency injection, CORS, Swagger, and auth wiring.

The Angular frontend lives in `frontend/` and uses Angular Material, reactive forms, route guards, an auth service, and an HTTP interceptor that retries requests after silent refresh.
