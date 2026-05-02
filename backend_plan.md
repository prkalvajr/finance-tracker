# Backend Implementation Plan — Personal Finance Tracker

> TDD approach: within each phase, follow vertical slices — one test → implement it → next test → implement it. Never write all tests before any implementation.

**Status legend:** ⬜ Not started · 🔄 In progress · ✅ Done & reviewed

---

## Phase 0 — Solution Restructure ✅

**Goal:** Replace the boilerplate layout with the Clean Architecture project structure.

**Actions:**
- [x] Delete `WeatherForecast.cs` and `WeatherForecastController.cs`
- [x] Rename existing project to `FinanceTracker.Api` (directory + `.csproj` + namespace)
- [x] Create `src/FinanceTracker.Domain/FinanceTracker.Domain.csproj`
- [x] Create `src/FinanceTracker.Application/FinanceTracker.Application.csproj`
- [x] Create `src/FinanceTracker.Infrastructure/FinanceTracker.Infrastructure.csproj`
- [x] Create `tests/FinanceTracker.Application.Tests/FinanceTracker.Application.Tests.csproj`
- [x] Create `tests/FinanceTracker.Infrastructure.Tests/FinanceTracker.Infrastructure.Tests.csproj`
- [x] Create `tests/FinanceTracker.Api.Tests/FinanceTracker.Api.Tests.csproj`
- [x] Add all projects to `finance-tracker.sln`
- [x] Wire project references:
  - Domain ← (none)
  - Application → Domain
  - Infrastructure → Application, Domain
  - Api → Application, Infrastructure
  - *.Tests → their matching layer + Domain

**NuGet packages:**
- Infrastructure: `Npgsql`, `BCrypt.Net-Next`, `System.IdentityModel.Tokens.Jwt`, `Microsoft.IdentityModel.Tokens`
- Api: `Microsoft.AspNetCore.Authentication.JwtBearer`
- *.Tests: `xunit`, `xunit.runner.visualstudio`, `Moq`, `FluentAssertions`, `Microsoft.NET.Test.Sdk`

**Checkpoint:** All projects compile, solution loads cleanly. No logic yet.

---

## Phase 1 — Domain Layer ✅

**Goal:** Define entities and repository interfaces. Pure C#, zero external dependencies. No tests needed.

**Files:**

| File | Responsibility | Done |
|---|---|---|
| `src/FinanceTracker.Domain/Entities/User.cs` | `UserId`, `Name`, `Email`, `PasswordHash` | ✅ |
| `src/FinanceTracker.Domain/Entities/Transaction.cs` | `TransactionId`, `UserId`, `Title`, `Amount`, `Category`, `Date`, `Deleted`, `CreatedAt`, `UpdatedAt` | ✅ |
| `src/FinanceTracker.Domain/Entities/RefreshToken.cs` | `Id`, `UserId`, `Token`, `ExpiresAt`, `RevokedAt?` | ✅ |
| `src/FinanceTracker.Domain/Repositories/IUserRepository.cs` | `GetByIdAsync`, `GetByEmailAsync`, `CreateAsync`, `UpdateAsync` | ✅ |
| `src/FinanceTracker.Domain/Repositories/ITransactionRepository.cs` | `GetPagedAsync`, `GetSummaryAsync`, `GetByIdAsync`, `CreateAsync`, `UpdateAsync`, `SoftDeleteAsync` | ✅ |
| `src/FinanceTracker.Domain/Repositories/IRefreshTokenRepository.cs` | `CreateAsync`, `GetByTokenAsync`, `RevokeAsync` | ✅ |

**Checkpoint:** Domain compiles with zero NuGet dependencies.

---

## Phase 2 — Application: AuthService ✅

**Goal:** Registration, login, token refresh, and logout.

**Support files (no tests):**

| File | Responsibility |
|---|---|
| `src/FinanceTracker.Application/Exceptions/NotFoundException.cs` | Domain exception → 404 |
| `src/FinanceTracker.Application/Exceptions/ConflictException.cs` | Domain exception → 409 |
| `src/FinanceTracker.Application/Exceptions/ValidationException.cs` | Domain exception → 400 |
| `src/FinanceTracker.Application/Exceptions/UnauthorizedException.cs` | Domain exception → 401 |
| `src/FinanceTracker.Application/DTOs/Auth/RegisterRequest.cs` | `Name`, `Email`, `Password` |
| `src/FinanceTracker.Application/DTOs/Auth/LoginRequest.cs` | `Email`, `Password` |
| `src/FinanceTracker.Application/DTOs/Auth/AuthTokens.cs` | `AccessToken`, `RefreshToken`, `AccessTokenExpiry`, `RefreshTokenExpiry` |
| `src/FinanceTracker.Application/Services/IPasswordHasher.cs` | `Hash`, `Verify` |
| `src/FinanceTracker.Application/Services/IJwtService.cs` | `GenerateTokens` |
| `src/FinanceTracker.Application/Services/IAuthService.cs` | `RegisterAsync`, `LoginAsync`, `RefreshAsync`, `LogoutAsync` |
| `src/FinanceTracker.Application/Services/AuthService.cs` | Implements `IAuthService` |

**Test class:** `tests/FinanceTracker.Application.Tests/Services/AuthServiceTests.cs`

| # | Test | Status |
|---|---|---|
| 1 | `Register_WithValidData_CreatesUserAndReturnsTokens` | ✅ |
| 2 | `Register_WithDuplicateEmail_ThrowsConflictException` | ✅ |
| 3 | `Register_WithEmptyEmail_ThrowsValidationException` | ✅ |
| 4 | `Register_WithEmptyName_ThrowsValidationException` | ✅ |
| 5 | `Register_WithEmptyPassword_ThrowsValidationException` | ✅ |
| 6 | `Login_WithValidCredentials_ReturnsTokens` | ✅ |
| 7 | `Login_WithUnknownEmail_ThrowsUnauthorizedException` | ✅ |
| 8 | `Login_WithWrongPassword_ThrowsUnauthorizedException` | ✅ |
| 9 | `Refresh_WithValidToken_RevokesOldAndReturnsNewTokens` | ✅ |
| 10 | `Refresh_WithExpiredToken_ThrowsUnauthorizedException` | ✅ |
| 11 | `Refresh_WithRevokedToken_ThrowsUnauthorizedException` | ✅ |
| 12 | `Refresh_WithUnknownToken_ThrowsUnauthorizedException` | ✅ |
| 13 | `Logout_WithValidToken_RevokesToken` | ✅ |
| 14 | `Logout_WithUnknownToken_ThrowsUnauthorizedException` | ✅ |

**Checkpoint:** 14 tests pass. AuthService fully covered.

---

## Phase 3 — Application: UserService ✅

**Support files (no tests):**

| File | Responsibility |
|---|---|
| `src/FinanceTracker.Application/DTOs/Users/UserDto.cs` | `UserId`, `Name`, `Email` |
| `src/FinanceTracker.Application/DTOs/Users/UpdateUserRequest.cs` | `Name?`, `Email?`, `CurrentPassword?`, `NewPassword?` |
| `src/FinanceTracker.Application/Services/IUserService.cs` | `GetCurrentUserAsync`, `UpdateUserAsync` |
| `src/FinanceTracker.Application/Services/UserService.cs` | Implements `IUserService` |

**Test class:** `tests/FinanceTracker.Application.Tests/Services/UserServiceTests.cs`

| # | Test | Status |
|---|---|---|
| 1 | `GetCurrentUser_WithValidUserId_ReturnsUserDto` | ✅ |
| 2 | `GetCurrentUser_WithUnknownUserId_ThrowsNotFoundException` | ✅ |
| 3 | `UpdateUser_WithNewName_UpdatesNameOnly` | ✅ |
| 4 | `UpdateUser_WithNewEmail_UpdatesEmailOnly` | ✅ |
| 5 | `UpdateUser_WithDuplicateEmail_ThrowsConflictException` | ✅ |
| 6 | `UpdateUser_WithNewPassword_VerifiesCurrentPasswordAndUpdates` | ✅ |
| 7 | `UpdateUser_WithWrongCurrentPassword_ThrowsUnauthorizedException` | ✅ |
| 8 | `UpdateUser_WithNewPasswordButNoCurrentPassword_ThrowsValidationException` | ✅ |

**Checkpoint:** 8 tests pass. UserService fully covered.

---

## Phase 4 — Application: TransactionService ✅

**Support files (no tests):**

| File | Responsibility |
|---|---|
| `src/FinanceTracker.Application/DTOs/Transactions/TransactionDto.cs` | Maps all Transaction entity fields |
| `src/FinanceTracker.Application/DTOs/Transactions/CreateTransactionRequest.cs` | `Title`, `Amount`, `Category`, `Date?` |
| `src/FinanceTracker.Application/DTOs/Transactions/UpdateTransactionRequest.cs` | `TransactionId`, `Title`, `Amount`, `Category`, `Date?` |
| `src/FinanceTracker.Application/DTOs/Transactions/TransactionQueryParams.cs` | `Page=1`, `PageSize=20`, `SortBy="date"`, `SortOrder="desc"`, `Category?`, `DateFrom?`, `DateTo?` |
| `src/FinanceTracker.Application/DTOs/Transactions/PagedResult.cs` | Generic: `Items`, `TotalCount`, `Page`, `PageSize`, `TotalPages` |
| `src/FinanceTracker.Application/DTOs/Transactions/TransactionSummaryDto.cs` | `NetBalance`, `TotalIncome`, `TotalExpense` |
| `src/FinanceTracker.Application/Services/ITransactionService.cs` | `CreateAsync`, `UpdateAsync`, `GetPagedAsync`, `GetSummaryAsync`, `SoftDeleteAsync` |
| `src/FinanceTracker.Application/Services/TransactionService.cs` | Implements `ITransactionService` |

**Test class:** `tests/FinanceTracker.Application.Tests/Services/TransactionServiceTests.cs`

| # | Test | Status |
|---|---|---|
| 1 | `Create_WithValidData_ReturnsTransactionDto` | ✅ |
| 2 | `Create_WithNegativeAmount_ThrowsValidationException` | ✅ |
| 3 | `Create_WithZeroAmount_ThrowsValidationException` | ✅ |
| 4 | `Create_WithEmptyTitle_ThrowsValidationException` | ✅ |
| 5 | `Create_WithInvalidCategory_ThrowsValidationException` | ✅ |
| 6 | `Create_WithNoDate_DefaultsToToday` | ✅ |
| 7 | `Update_WithValidData_ReturnsUpdatedDto` | ✅ |
| 8 | `Update_WithTransactionBelongingToOtherUser_ThrowsNotFoundException` | ✅ |
| 9 | `Update_WithNegativeAmount_ThrowsValidationException` | ✅ |
| 10 | `Update_WithInvalidCategory_ThrowsValidationException` | ✅ |
| 11 | `GetPaged_ReturnsCorrectPageAndTotalCount` | ✅ |
| 12 | `GetPaged_WithCategoryFilter_PassesFilterToRepository` | ✅ |
| 13 | `GetPaged_WithDateRange_PassesDateRangeToRepository` | ✅ |
| 14 | `GetSummary_ReturnsNetBalanceTotalIncomeTotalExpense` | ✅ |
| 15 | `GetSummary_WithNoTransactions_ReturnsZeros` | ✅ |
| 16 | `SoftDelete_WithValidId_CallsRepository` | ✅ |
| 17 | `SoftDelete_WithTransactionBelongingToOtherUser_ThrowsNotFoundException` | ✅ |

**Checkpoint:** 17 tests pass. TransactionService fully covered.

---

## Phase 5 — Infrastructure: PasswordHasher + JwtService ✅

**Goal:** Implement and unit-test the two stateless infrastructure services (no DB required).

| File | Responsibility |
|---|---|
| `src/FinanceTracker.Infrastructure/Services/PasswordHasher.cs` | Implements `IPasswordHasher` via BCrypt.Net-Next |
| `src/FinanceTracker.Infrastructure/Services/JwtService.cs` | Implements `IJwtService`; reads config from `IConfiguration` |

**Test class:** `tests/FinanceTracker.Infrastructure.Tests/Services/PasswordHasherTests.cs`

| # | Test | Status |
|---|---|---|
| 1 | `Hash_ReturnsDifferentHashEachCall` | ✅ |
| 2 | `Verify_WithCorrectPassword_ReturnsTrue` | ✅ |
| 3 | `Verify_WithWrongPassword_ReturnsFalse` | ✅ |

**Test class:** `tests/FinanceTracker.Infrastructure.Tests/Services/JwtServiceTests.cs`

| # | Test | Status |
|---|---|---|
| 1 | `GenerateTokens_ReturnsNonEmptyAccessToken` | ✅ |
| 2 | `GenerateTokens_AccessTokenExpiresIn15Minutes` | ✅ |
| 3 | `GenerateTokens_RefreshTokenExpiresIn5Days` | ✅ |
| 4 | `GenerateTokens_AccessTokenContainsCorrectUserId` | ✅ |
| 5 | `GenerateTokens_AccessTokenContainsCorrectEmail` | ✅ |

**Checkpoint:** 8 tests pass. Both services covered.

---

## Phase 6 — Infrastructure: Repositories ✅

**Goal:** Implement and integration-test the three ADO.NET repositories against a real PostgreSQL database.

> These are integration tests. Each test class implements `IAsyncLifetime` to set up and tear down a clean schema per run. Configure the test DB via `appsettings.Test.json` or an environment variable.

| File | Responsibility |
|---|---|
| `src/FinanceTracker.Infrastructure/Migrations/001_InitialSchema.sql` | Creates all tables and indexes |
| `src/FinanceTracker.Infrastructure/Data/DbConnectionFactory.cs` | Wraps `NpgsqlConnection`; reads connection string from `IConfiguration` |
| `src/FinanceTracker.Infrastructure/Repositories/UserRepository.cs` | Implements `IUserRepository` |
| `src/FinanceTracker.Infrastructure/Repositories/TransactionRepository.cs` | Implements `ITransactionRepository` |
| `src/FinanceTracker.Infrastructure/Repositories/RefreshTokenRepository.cs` | Implements `IRefreshTokenRepository` |
| `src/FinanceTracker.Infrastructure/Seeding/DatabaseSeeder.cs` | Idempotent; inserts `demo@example.com` / `demo1234` + 10 sample transactions |

**Test class:** `tests/FinanceTracker.Infrastructure.Tests/Repositories/UserRepositoryTests.cs`

| # | Test | Status |
|---|---|---|
| 1 | `CreateAsync_InsertsUserAndReturnsGeneratedId` | ✅ |
| 2 | `GetByIdAsync_WithExistingId_ReturnsUser` | ✅ |
| 3 | `GetByIdAsync_WithUnknownId_ReturnsNull` | ✅ |
| 4 | `GetByEmailAsync_WithExistingEmail_ReturnsUser` | ✅ |
| 5 | `GetByEmailAsync_WithUnknownEmail_ReturnsNull` | ✅ |
| 6 | `UpdateAsync_PersistsChangedFields` | ✅ |

**Test class:** `tests/FinanceTracker.Infrastructure.Tests/Repositories/TransactionRepositoryTests.cs`

| # | Test | Status |
|---|---|---|
| 1 | `CreateAsync_InsertsTransactionAndReturnsId` | ✅ |
| 2 | `GetByIdAsync_WithExistingId_ReturnsTransaction` | ✅ |
| 3 | `GetByIdAsync_WithUnknownId_ReturnsNull` | ✅ |
| 4 | `GetPagedAsync_ReturnsPaginatedResults` | ✅ |
| 5 | `GetPagedAsync_ExcludesSoftDeletedRecords` | ✅ |
| 6 | `GetPagedAsync_FiltersByCategory` | ✅ |
| 7 | `GetPagedAsync_FiltersByDateRange` | ✅ |
| 8 | `GetPagedAsync_SortsByDateDescendingByDefault` | ✅ |
| 9 | `GetPagedAsync_OnlyReturnsTransactionsForRequestedUser` | ✅ |
| 10 | `UpdateAsync_PersistsChangedFields` | ✅ |
| 11 | `SoftDeleteAsync_SetsDeletedFlag` | ✅ |
| 12 | `SoftDeleteAsync_DoesNotPhysicallyRemoveRow` | ✅ |
| 13 | `GetSummaryAsync_CalculatesCorrectTotals` | ✅ |

**Test class:** `tests/FinanceTracker.Infrastructure.Tests/Repositories/RefreshTokenRepositoryTests.cs`

| # | Test | Status |
|---|---|---|
| 1 | `CreateAsync_InsertsToken` | ✅ |
| 2 | `GetByTokenAsync_WithExistingToken_ReturnsToken` | ✅ |
| 3 | `GetByTokenAsync_WithUnknownToken_ReturnsNull` | ✅ |
| 4 | `RevokeAsync_SetsRevokedAt` | ✅ |

**Checkpoint:** 23 tests pass. All repositories covered.

---

## Phase 7 — API: AuthController ✅

**Goal:** Implement the four auth endpoints and the middleware they depend on.

**Support files (no tests):**

| File | Responsibility |
|---|---|
| `src/FinanceTracker.Api/Middleware/ExceptionHandlingMiddleware.cs` | Maps domain exceptions to `ProblemDetails` HTTP responses |
| `src/FinanceTracker.Api/Middleware/CookieTokenMiddleware.cs` | Reads JWT from `access_token` cookie, sets `Authorization: Bearer` header |
| `src/FinanceTracker.Api/Helpers/CookieHelper.cs` | `SetTokenCookies` and `ClearTokenCookies` |
| `src/FinanceTracker.Api/Controllers/AuthController.cs` | POST `/register`, POST `/login`, POST `/refresh`, POST `/logout` |

**Test class:** `tests/FinanceTracker.Api.Tests/Controllers/AuthControllerTests.cs`
*(Use `WebApplicationFactory` with mocked application services)*

| # | Test | Status |
|---|---|---|
| 1 | `Register_WithValidData_Returns201AndSetsCookies` | ✅ |
| 2 | `Register_WithDuplicateEmail_Returns409` | ✅ |
| 3 | `Register_WithMissingFields_Returns400` | ✅ |
| 4 | `Login_WithValidCredentials_Returns200AndSetsCookies` | ✅ |
| 5 | `Login_WithBadCredentials_Returns401` | ✅ |
| 6 | `Refresh_WithValidCookie_Returns200AndRotatesCookies` | ✅ |
| 7 | `Refresh_WithMissingCookie_Returns401` | ✅ |
| 8 | `Logout_WithValidCookie_Returns200AndClearsCookies` | ✅ |

**Checkpoint:** 8 tests pass. Auth endpoints and middleware covered.

---

## Phase 8 — API: UserController ✅

| File | Responsibility |
|---|---|
| `src/FinanceTracker.Api/Controllers/UserController.cs` | GET `/user`, PUT `/update`; reads `userId` from JWT claims |

**Test class:** `tests/FinanceTracker.Api.Tests/Controllers/UserControllerTests.cs`

| # | Test | Status |
|---|---|---|
| 1 | `GetUser_Authenticated_Returns200WithUserDto` | ✅ |
| 2 | `GetUser_Unauthenticated_Returns401` | ✅ |
| 3 | `UpdateUser_WithValidData_Returns200WithUpdatedDto` | ✅ |
| 4 | `UpdateUser_WithDuplicateEmail_Returns409` | ✅ |
| 5 | `UpdateUser_Unauthenticated_Returns401` | ✅ |

**Checkpoint:** 5 tests pass. User endpoints covered.

---

## Phase 9 — API: TransactionController ✅

| File | Responsibility |
|---|---|
| `src/FinanceTracker.Api/Controllers/TransactionController.cs` | POST `/transaction`, PUT `/transaction`, GET `/transactions`, DELETE `/transaction`, GET `/transactions/summary` |

**Test class:** `tests/FinanceTracker.Api.Tests/Controllers/TransactionControllerTests.cs`

| # | Test | Status |
|---|---|---|
| 1 | `Create_WithValidData_Returns201WithDto` | ✅ |
| 2 | `Create_WithNegativeAmount_Returns400` | ✅ |
| 3 | `Create_Unauthenticated_Returns401` | ✅ |
| 4 | `Update_WithValidData_Returns200WithDto` | ✅ |
| 5 | `Update_WithOtherUsersTransaction_Returns404` | ✅ |
| 6 | `Update_Unauthenticated_Returns401` | ✅ |
| 7 | `GetTransactions_Returns200WithPagedResult` | ✅ |
| 8 | `GetTransactions_WithFilters_PassesFiltersToService` | ✅ |
| 9 | `GetTransactions_Unauthenticated_Returns401` | ✅ |
| 10 | `Delete_WithValidId_Returns204` | ✅ |
| 11 | `Delete_WithOtherUsersTransaction_Returns404` | ✅ |
| 12 | `Delete_Unauthenticated_Returns401` | ✅ |
| 13 | `GetSummary_Returns200WithSummaryDto` | ✅ |
| 14 | `GetSummary_Unauthenticated_Returns401` | ✅ |

**Checkpoint:** 14 tests pass. All transaction endpoints covered.

---

## Phase 10 — DI Wiring + Program.cs ✅

**Goal:** Wire all registrations, middleware, and startup logic. No new tests — controller tests via `WebApplicationFactory` already exercise the DI graph.

**Files:**

| File | Responsibility |
|---|---|
| `src/FinanceTracker.Api/Program.cs` | Register all services, repositories, middleware, JWT auth, and run seeder on startup |
| `src/FinanceTracker.Api/appsettings.json` | Jwt section (`Issuer`, `Audience`, `AccessTokenExpiryMinutes: 15`, `RefreshTokenExpiryDays: 5`), `ConnectionStrings` |
| `src/FinanceTracker.Api/appsettings.Development.json` | Local PostgreSQL connection string |

> `JWT__Secret` is set only via environment variable — never written to any config file.

**Checkpoint:** `dotnet run` starts cleanly, Swagger loads, seeder inserts demo data, all existing tests still pass.

---

## Progress Summary

| Phase | Description | Status |
|---|---|---|
| 0 | Solution restructure | ✅ |
| 1 | Domain entities + interfaces | ✅ |
| 2 | Application: AuthService (14 tests) | ✅ |
| 3 | Application: UserService (8 tests) | ✅ |
| 4 | Application: TransactionService (17 tests) | ✅ |
| 5 | Infrastructure: PasswordHasher + JwtService (8 tests) | ✅ |
| 6 | Infrastructure: Repositories (23 tests) | ✅ |
| 7 | API: AuthController + middleware (8 tests) | ✅ |
| 8 | API: UserController (5 tests) | ✅ |
| 9 | API: TransactionController (14 tests) | ✅ |
| 10 | DI wiring + Program.cs | ✅ |

**Total: ~97 test cases across 8 test classes.**
