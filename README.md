# finance-tracker

## Architecture Notes

### Error Handling — Global Exception Middleware

All errors are handled in a single middleware class (`ExceptionHandlingMiddleware`) registered in the API layer. Controllers contain only happy-path logic — no try/catch.

**How it works:**
1. Domain exceptions are defined in the Application layer (no dependencies): `NotFoundException`, `ConflictException`, `ValidationException`, `UnauthorizedException`.
2. Services throw these typed exceptions when something goes wrong (e.g., email already taken → `ConflictException`).
3. The middleware catches any unhandled exception and maps it to an HTTP response using `ProblemDetails` (RFC 7807 standard):
   - `NotFoundException` → 404
   - `ConflictException` → 409
   - `ValidationException` → 400
   - `UnauthorizedException` → 401
   - Anything else → 500
4. The response body is always a `ProblemDetails` JSON object: `{ "title": "...", "status": 404, "detail": "..." }`.

**Why this approach:**
- One place to change error-to-HTTP mapping — impossible to forget in a specific controller.
- Controllers are clean and readable (only the success path).
- `ProblemDetails` is the ASP.NET Core standard; clients get a consistent error shape.