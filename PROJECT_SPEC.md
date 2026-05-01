# Personal Finance Tracker - Project Specification

## User Story
As a user, I want to track my personal income and expenses so I can understand my financial flow. I should be able to register, log in, and manage my transaction records (create, read, update, delete) securely behind my own account.

## Tech Stack

### Backend
- **.NET** (latest stable version) with ASP.NET Web API using MVC pattern
- **No ORM** — use raw ADO.NET with Npgsql for PostgreSQL access
- **Database:** PostgreSQL
- **Testing:** xUnit + Moq, TDD approach, maximum coverage
- **Authentication:** JWT (access + refresh tokens) via HttpOnly cookies

### Frontend
- **Angular** with TypeScript
- **Toast notifications** for success/error feedback

## Architecture

### Clean Architecture Principles
- Strict separation of API, Business Logic, and Data Access layers
- Dependency Inversion: inner layers define interfaces, outer layers implement them
- Domain layer has zero external dependencies
- Repository interfaces live in the Domain layer; implementations live in Infrastructure
- All dependencies wired via DI in the API layer

### Backend Layers
- **Domain** — Entities and repository interfaces. Pure C#, no dependencies.
- **Application** — Business logic, services, DTOs, validators. Depends only on Domain.
- **Infrastructure** — ADO.NET repositories, password hashing, JWT service. Implements Domain interfaces.
- **API** — Controllers, middleware, DI configuration.
- **Tests** — Mirrors structure of layers above.

### Frontend Structure
- `core/` — singleton services, guards, interceptors
- `shared/` — reusable components (grid, confirmation dialog)
- `pages/` — route components (home, login, register, user profile, transaction modal)
- `models/` — TypeScript interfaces

### Solution Layout
Single Visual Studio solution containing both backend projects and the Angular frontend, so everything is visible in the IDE.

## Database Schema

### Table: User
- `userId` — PK, auto-increment
- `name` — varchar
- `email` — varchar
- `passwordHash` — varchar (bcrypt or similar)
- **Unique index on `email`** (prevents duplicate accounts)

### Table: Transaction
- `transactionId` — PK, auto-increment
- `userId` — int, FK to User
- `title` — varchar
- `amount` — decimal (always positive — sign is derived from category)
- `category` — varchar (`income` or `expense`)
- `date` — date (optional on UI; defaults to current date if not provided)
- `deleted` — bit (soft delete)
- `createdAt` — timestamp
- `updatedAt` — timestamp
- **Index on (`userId`, `deleted`, `date`)** — most common query pattern

### Table: RefreshToken
- `id` — PK, auto-increment
- `userId` — int, FK to User
- `token` — varchar
- `expiresAt` — timestamp
- `revokedAt` — timestamp (nullable)

### Seeding
Include seed data: at least one demo user with known credentials and several sample transactions for demo purposes.

## Authentication Flow

- **Access token:** 15-minute lifetime
- **Refresh token:** 5-day lifetime, stored in DB, one-time use (rotated on every refresh)
- Both tokens delivered as **HttpOnly cookies** with `Secure` and `SameSite` flags
- JavaScript cannot read tokens — eliminates XSS token theft
- `/login` sets both cookies
- `/refresh` rotates both cookies (revokes old refresh token, issues new pair)
- `/logout` revokes refresh token in DB and clears cookies

## API Design

No versioning (internal APIs).

### User API

**Unauthorized**
- `POST /register` — create user. Validate email is not already in use.
- `POST /login` — authenticate user, set token cookies.

**Authorized**
- `POST /refresh` — rotate tokens silently.
- `POST /logout` — revoke refresh token, clear cookies.
- `PUT /update` — update user information (name, email, password).
- `GET /user` — return current user data.

### Transaction API

All endpoints are authorized. All operations are scoped to the authenticated user.

- `POST /transaction` — create transaction. Validate amount is not negative.
- `PUT /transaction` — update transaction. Validate amount is not negative.
- `GET /transactions` — return paginated, filterable, sortable list. Includes full record data so the same response can populate the edit modal (no separate detail endpoint needed).
- `DELETE /transaction` — soft delete (set `deleted = true`).
- `GET /transactions/summary` — return net balance, total income, and total expense for the user.

### Validation Rules
- Email must be unique on registration
- Amount must be greater than zero
- Category must be `income` or `expense`
- Title is required

## Frontend Requirements

### Reusable Components
1. **Grid** — supports filtering, server-side pagination, and column ordering. Data source is passed in from the parent so the component is reusable.
2. **Confirmation Dialog** — used for delete confirmations, update confirmations, and password change confirmation.

### Route Components
1. **Home** — displays the transaction grid and an "Add Transaction" button.
2. **Transaction Modal** — used for both create and edit. Fields: title, amount, optional date, category. Reject negative numbers in the form.
3. **User Profile** — displays name and email; allows updates including password change.
4. **Login** and **Register** pages.

### Cross-Cutting Frontend Concerns
- **HTTP Interceptor** — handles 401 responses by silently calling `/refresh` and retrying the original request. (Cookies attach automatically; no token reading needed.)
- **Auth Guard** — protects authenticated routes.
- **Auth Service** — centralizes login, logout, and refresh logic.
- All HTTP calls must use `withCredentials: true` so cookies are sent.

## Testing Requirements
- Use TDD: write tests before implementation.
- Cover all layers: services (business logic), repositories (data access), controllers (API).
- Mock dependencies with Moq — services should be tested in isolation from the database.

## Deliverables
- README with setup instructions (database setup, running backend, running frontend, demo credentials).
- Seeded demo data and credentials.
