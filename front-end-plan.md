# Frontend Implementation Plan — Personal Finance Tracker

> TDD approach: within each phase, follow vertical slices — one test → implement it → next test → implement it. Never write all tests before any implementation.

**Status legend:** ⬜ Not started · 🔄 In progress · ✅ Done & reviewed

---

## Key Design Decisions

These decisions were settled before phasing and apply across all phases. See each phase for the concrete files they produce.

### Visual scope
- The Evergreen design guide is **inspiration only**: palette, typography, spacing/roundness/shadow scales become design tokens. Layout suggestions (side nav, top-nav search, notifications icon, social auth, dashboard mini-charts, savings goals, "Status" column, deactivate-account section) are **dropped** because the backend does not support them.
- Login and Register are **two separate routes that share one card shell** (per spec). Not a tabbed single page.

### Component library & theming
- **Angular Material (M3)** for all primitive components (`mat-table`, `mat-dialog`, `mat-form-field`, `mat-button`, `mat-icon`, `mat-snack-bar`, `mat-paginator`, `mat-sort`, `mat-card`, `mat-toolbar`, `mat-select`, `mat-datepicker`).
- **`@angular/cdk`** comes as a transitive dependency; available for overlay/a11y where needed.
- Theme is built with `mat.define-theme` from a **custom primary palette seeded from `#064E3B`** (Emerald 900). Tertiary from Emerald 500 (`#10B981`). Typography via `mat.define-typography-config` using **Manrope**, loaded via Google Fonts `<link>` in `index.html`.
- Stylesheet language switches from CSS → **SCSS** (Material's theming requires it). Tokens live in `src/styles/_tokens.scss`; theme in `src/styles/_theme.scss`; typography in `src/styles/_typography.scss`.

### Layout shell & routing
- **Two-layout pattern.** `MainLayoutComponent` for authenticated routes (`mat-toolbar` with brand, Profile link, Logout button + `<router-outlet/>`). `AuthLayoutComponent` for `/login` and `/register` (centered card, no toolbar).
- **Routing tree:**
  ```
  []
  ├── '' (MainLayoutComponent, canActivate: authGuard)
  │   ├── 'home'    → HomePageComponent
  │   ├── 'profile' → ProfilePageComponent
  │   └── ''        → redirect to 'home'
  └── '' (AuthLayoutComponent)
      ├── 'login'    → LoginPageComponent
      └── 'register' → RegisterPageComponent
  ```

### Auth state & bootstrap
- Tokens are HttpOnly — JavaScript cannot read them. State authority lives in `AuthService` as a signal.
- **Cold-start bootstrap probe**: on app init (`provideAppInitializer`), call `GET /user`. 200 → populate `currentUser` signal. 401 → interceptor silently calls `/refresh` and retries; if that also 401s, signal stays null and the guard redirects to `/login`.
- App rendering **blocks** on the bootstrap call (single fast request; avoids a Login flash).
- `AuthService` shape:
  ```ts
  private _currentUser = signal<UserDto | null>(null);
  readonly currentUser = this._currentUser.asReadonly();
  readonly isAuthenticated = computed(() => this._currentUser() !== null);
  bootstrap(): Promise<void>;
  login(req): Promise<void>;
  register(req): Promise<void>;
  logout(): Promise<void>;
  refresh(): Observable<void>;   // single-flight; used by interceptor
  ```

### HTTP interceptor (single-flight refresh)
- Backend rotates refresh tokens on every `/refresh` call (revokes the old one). Concurrent 401s would cause the second `/refresh` to fail using a now-revoked token. Single-flight is **required for correctness**, not just performance.
- `AuthService.refresh()` caches its in-flight observable via `shareReplay(1)`. Concurrent callers subscribe to the same observable; cleared when it completes.
- **`HttpContext` tokens** (`core/http/http-context-tokens.ts`):
  - `SKIP_AUTH_REFRESH` — set on `/login`, `/register`, `/refresh`, `/logout` calls so the interceptor doesn't retry them (avoids infinite loops).
  - `ALREADY_RETRIED` — set on the retry attempt so a still-failing 401 logs out instead of looping.
  - `SKIP_ERROR_TOAST` — opt out of automatic error toasts when the page renders the error inline (login/register).
- Every request is sent with `withCredentials: true` so cookies attach.

### Reactive primitive
- **Signals for app state**, RxJS only at the HTTP boundary and the interceptor.
- All components: `changeDetection: ChangeDetectionStrategy.OnPush`. No `async` pipes — components read signals directly in templates.
- Standalone components everywhere (Angular 21 default).

### Grid component contract
- `<app-grid>` accepts a declarative columns config and a fetcher callback. Grid owns paging/sort/filter state internally and calls the fetcher on changes.
- "Column ordering" interpreted as **server-side sortable headers** (not drag-to-reorder).
- Server-side pagination (page is 1-based to match backend).
- Filter slot above the table; on filter change, grid resets to page 1 and re-fetches.
- Types:
  ```ts
  interface GridColumn<T> {
    key: string;
    label: string;
    sortable: boolean;
    cell?: (row: T) => string;
    template?: TemplateRef<unknown>;
  }
  interface GridQuery {
    page: number;       // 1-based
    pageSize: number;
    sortBy?: string;
    sortOrder?: 'asc' | 'desc';
    filters: Record<string, unknown>;
  }
  ```
- `GridQuery` maps 1:1 onto the backend's `TransactionQueryParams`.
- **Home filters**: `category` (All / Income / Expense) + `dateFrom..dateTo`. No title text search (backend doesn't support it).

### Confirmation dialog
- `ConfirmationService.confirm(opts): Promise<boolean>` — single typed entry point.
- `ConfirmOptions`: `title`, `message`, optional `confirmText`/`cancelText`, optional `variant: 'default' | 'danger'` (renders confirm button in red for destructive actions).
- Internally opens one `ConfirmationDialogComponent` (mat-dialog) and resolves the promise from `afterClosed()`.

### Notifications & error handling
- `NotificationService` exposes `success(msg)`, `error(msg)`, `info(msg)`. Wraps `MatSnackBar` with token-styled `panelClass` (`.toast-success`, `.toast-error`, `.toast-info`). Auto-dismiss 4s with a "Close" action.
- **Automatic error toasts** via `errorToastInterceptor`. It catches `HttpErrorResponse`, parses `ProblemDetails` (`detail` → `title` → generic fallback; for 400 with `errors` dictionary, joins field messages), and calls `notification.error(...)`. **Skips** when status is 401 (auth interceptor handles it) or when the request set `SKIP_ERROR_TOAST`.
- Success toasts are **manual** — components call `notification.success(...)` after meaningful actions only.
- Angular `ErrorHandler` is registered as a secondary safety net for unexpected uncaught errors.

### Forms & validation
- **Reactive Forms** with typed `FormGroup`. No `ngModel`.
- Client-side validators **mirror backend rules** (instant feedback; server is still authoritative):
  - Email: `required`, `email`.
  - Password (register/change): `required`, `minLength(8)`.
  - Title: `required`, `maxLength(200)`.
  - Amount: `required`, `min(0.01)`.
  - Category: `mat-select` with `'income' | 'expense'` options (validation implicit).
  - Profile password change: `FormGroup`-level cross-field validator — if `newPassword` is set, `currentPassword` must be set.
- **Per-field server errors**: backend returns `ProblemDetails` with an `errors` dictionary on validation failures. A `core/utils/apply-server-errors.ts` helper maps field names onto matching controls via `setErrors({ server: 'msg' })`, rendered through `<mat-error>`. Login and Register set `SKIP_ERROR_TOAST` so errors appear inline rather than as toasts.

### Models / DTO sync
- TypeScript interfaces are **hand-written** in `src/app/models/`, mirroring `Application/DTOs/` on the backend. No codegen.
- Typing rules:
  - `Date` fields → `string` (ISO 8601). Parsed/formatted in templates with `DatePipe`.
  - `decimal` (amount) → `number`.
  - `Category` → `'income' | 'expense'` literal union.
  - `Deleted` is server-side only; not on `TransactionDto` in TS.

### Folder structure
```
src/app/
├── core/
│   ├── interceptors/         (auth, error-toast)
│   ├── guards/               (auth)
│   ├── services/             (auth, user, transactions, notification)
│   ├── http/                 (context tokens, api-config)
│   ├── utils/                (apply-server-errors, format)
│   └── error-handler.ts
├── shared/
│   ├── components/
│   │   ├── grid/             (component + grid.types.ts)
│   │   └── confirmation-dialog/
│   └── services/             (confirmation.service)
├── layouts/
│   ├── main-layout/
│   └── auth-layout/
├── pages/
│   ├── home/
│   │   └── transaction-modal/
│   ├── profile/
│   ├── login/
│   └── register/
├── models/                   (auth, user, transaction, paged-result, problem-details)
├── app.config.ts
├── app.routes.ts
└── app.ts

src/styles/
├── _tokens.scss
├── _theme.scss
└── _typography.scss

src/styles.scss
```

### Testing
- **Vitest** (already in `devDependencies`). If the Angular 21 + Vitest bridge is not viable, fall back to Karma/Jasmine — decided in Phase 0.
- **Deep tests** for: every service in `core/services/`, both interceptors, `core/utils/`, the `Grid` component (paging/sort/filter behavior), `ConfirmationService` (boolean resolution paths), and `TransactionModal` form logic (validation, create vs edit branching, date default).
- **No tests** for page components (Home, Profile, Login, Register) — they only compose services and reusables.
- **No e2e for v1.** README ships a manual smoke checklist instead.
- No coverage % gate. Rule: "every service method has a test; every interceptor branch has a test; Grid behavior tests cover paging/sort/filter."

---

## Phase 0 — Project setup ⬜

**Goal:** Migrate the Angular 21 scaffold to SCSS + Angular Material with Evergreen tokens, scaffold non-logic pieces (models, app config), confirm the test runner works.

**Actions:**
- [ ] Add `@angular/material` (CDK comes transitively).
- [ ] Rename `src/styles.css` → `src/styles.scss`; update `angular.json` (`inlineStyleLanguage: scss`, `styles: ["src/styles.scss"]`, schematics defaults).
- [ ] Create `src/styles/_tokens.scss` — raw Evergreen tokens (colors, spacing 8/16/24/32, radius 8/12, shadow `0 1px 3px rgba(0,0,0,0.05)`).
- [ ] Create `src/styles/_typography.scss` — Manrope scale.
- [ ] Create `src/styles/_theme.scss` — `mat.define-theme` with custom primary palette seeded from `#064E3B`, tertiary from `#10B981`, M3 typography from `_typography.scss`. Apply via `mat.all-component-themes`.
- [ ] `index.html` `<link>` Manrope from Google Fonts.
- [ ] `src/styles.scss` imports `_theme.scss` + sets globals (background, font family, base text color).
- [ ] Create all DTO interface files in `src/app/models/` per Q12 typing rules.
- [ ] Create empty placeholders for `core/`, `shared/`, `layouts/`, `pages/` folders.
- [ ] `app.config.ts`: `provideRouter`, `provideHttpClient(withInterceptors([]))`, `provideAnimations`, `provideAppInitializer` placeholder, API base URL token (env-config-friendly).
- [ ] Verify Vitest + Angular 21 bridge runs at least one trivial test. If not viable, switch to Karma/Jasmine and document the change in `frontend/README.md`.

**Files:**

| File | Responsibility |
|---|---|
| `src/styles.scss` | Global stylesheet entry; imports theme + typography; sets body background and font |
| `src/styles/_tokens.scss` | Raw Evergreen color/spacing/radius/shadow tokens |
| `src/styles/_typography.scss` | Manrope-based M3 typography config |
| `src/styles/_theme.scss` | M3 theme definition built from `_tokens.scss` |
| `src/app/models/auth.models.ts` | `RegisterRequest`, `LoginRequest` |
| `src/app/models/user.models.ts` | `UserDto`, `UpdateUserRequest` |
| `src/app/models/transaction.models.ts` | `TransactionDto`, `CreateTransactionRequest`, `UpdateTransactionRequest`, `TransactionQueryParams`, `TransactionSummaryDto` |
| `src/app/models/paged-result.models.ts` | Generic `PagedResult<T>` |
| `src/app/models/problem-details.models.ts` | `ProblemDetails` shape |
| `src/app/core/http/api-config.ts` | `API_BASE_URL` injection token |
| `src/app/app.config.ts` | App-wide providers |
| `src/app/app.routes.ts` | Empty route tree to be filled in Phase 2 |

**Checkpoint:** App compiles, `ng serve` shows a Manrope-rendered placeholder on the Evergreen surface background. `npm test` runs and passes a trivial sanity test.

---

## Phase 1 — Core HTTP + Auth scaffolding ⬜

**Goal:** Build all auth, interceptor, notification, and utility plumbing with full test coverage. No UI yet.

**Files:**

| File | Responsibility |
|---|---|
| `src/app/core/http/http-context-tokens.ts` | `SKIP_AUTH_REFRESH`, `ALREADY_RETRIED`, `SKIP_ERROR_TOAST` |
| `src/app/core/services/auth.service.ts` | Signals (`currentUser`, `isAuthenticated`); `bootstrap`, `login`, `register`, `logout`, `refresh` (single-flight via `shareReplay(1)`) |
| `src/app/core/services/user.service.ts` | `getCurrentUser()`, `updateUser()` (used by Phase 4 too) |
| `src/app/core/services/notification.service.ts` | `success`, `error`, `info` over `MatSnackBar` with token-styled `panelClass` |
| `src/app/core/interceptors/auth.interceptor.ts` | Sets `withCredentials: true`; handles 401 → single-flight refresh → retry once; respects `SKIP_AUTH_REFRESH` and `ALREADY_RETRIED` |
| `src/app/core/interceptors/error-toast.interceptor.ts` | Catches errors, parses `ProblemDetails`, fires `notification.error(...)`. Skips on 401 and `SKIP_ERROR_TOAST` |
| `src/app/core/utils/apply-server-errors.ts` | Maps `ProblemDetails.errors` dictionary onto `FormGroup` controls via `setErrors({ server: msg })` |
| `src/app/core/utils/format.ts` | Currency + date formatting helpers |
| `src/app/core/error-handler.ts` | Custom `ErrorHandler` safety net — logs and toasts unexpected errors |
| `src/app/app.config.ts` (update) | Wire interceptors, `provideAppInitializer` calling `AuthService.bootstrap()`, custom `ErrorHandler` |

**Test classes:**

| Test class | # tests |
|---|---|
| `core/services/auth.service.spec.ts` | bootstrap success / 401 path / refresh-then-retry path; login / register success and failure; logout clears signal; refresh single-flight (concurrent calls share one HTTP request); refresh failure clears state |
| `core/services/notification.service.spec.ts` | each method opens snack bar with the right `panelClass` |
| `core/interceptors/auth.interceptor.spec.ts` | 200 passes through; 401 triggers refresh + retry; `SKIP_AUTH_REFRESH` request is not retried; `ALREADY_RETRIED` request is not retried; refresh failure propagates and triggers logout |
| `core/interceptors/error-toast.interceptor.spec.ts` | non-401 error → toast; 401 → no toast; `SKIP_ERROR_TOAST` → no toast; `ProblemDetails.detail` used; `errors` dictionary joined; missing fields fall back to generic message |
| `core/utils/apply-server-errors.spec.ts` | maps fields correctly; ignores unknown fields; sets `server` error key |

**Checkpoint:** All tests pass. App still loads (bootstrap probe runs against backend; if `GET /user` 401s, app boots with `currentUser = null`).

---

## Phase 2 — Layout shells + routing tree ⬜

**Goal:** Implement both layout components, the auth guard, and the full routing tree. Pages are placeholders.

**Files:**

| File | Responsibility |
|---|---|
| `src/app/core/guards/auth.guard.ts` | Functional guard — reads `authService.currentUser()`; redirects to `/login` if null |
| `src/app/layouts/main-layout/main-layout.component.ts` | `mat-toolbar` (brand left, Profile link + Logout button right), centered max-width content container, `<router-outlet/>` |
| `src/app/layouts/auth-layout/auth-layout.component.ts` | Centered card on Evergreen surface background; `<router-outlet/>` inside the card |
| `src/app/pages/home/home-page.component.ts` | Placeholder ("Home") |
| `src/app/pages/profile/profile-page.component.ts` | Placeholder ("Profile") |
| `src/app/pages/login/login-page.component.ts` | Placeholder ("Login") |
| `src/app/pages/register/register-page.component.ts` | Placeholder ("Register") |
| `src/app/app.routes.ts` (update) | Two-layout tree from "Layout shell & routing" |

**Tests:** none (composition only — exercised in later phases).

**Checkpoint:** Manually verify routing: unauthenticated user is redirected from `/home` and `/profile` to `/login`. Toolbar Logout calls `AuthService.logout()` and routes to `/login`.

---

## Phase 3 — Login + Register pages ⬜

**Goal:** First end-to-end auth flow against the real backend.

**Files:**

| File | Responsibility |
|---|---|
| `src/app/pages/login/login-page.component.ts` | Reactive form (email, password); validators; submit calls `authService.login()` with `SKIP_ERROR_TOAST`; on error → `applyServerErrors`; on success → success toast + navigate `/home` |
| `src/app/pages/register/register-page.component.ts` | Reactive form (name, email, password); same submit pattern as Login |

**Test classes:**

| Test class | # tests |
|---|---|
| `pages/login/login-page.component.spec.ts` | client-side validators; server `errors` dictionary populates per-field `<mat-error>`; success navigates to `/home` (logic-level — not a render snapshot) |
| `pages/register/register-page.component.spec.ts` | same shape as Login |

> Note: these are the only page-component tests; their value is validating the inline server-error path. Future page components (Home, Profile) skip component tests per the testing strategy.

**Checkpoint:** Register a new user → land on `/home` placeholder. Logout → log back in. 409 on duplicate email shows under the email field, no toast.

---

## Phase 4 — Profile page + Confirmation Dialog ⬜

**Goal:** Profile editing including password change. Builds the reusable confirmation dialog (used here and in Phase 6).

**Files:**

| File | Responsibility |
|---|---|
| `src/app/shared/components/confirmation-dialog/confirmation-dialog.component.ts` | mat-dialog UI: title, message, two buttons; `mat-warn` on confirm when `variant === 'danger'` |
| `src/app/shared/services/confirmation.service.ts` | `confirm(opts): Promise<boolean>` |
| `src/app/pages/profile/profile-page.component.ts` | Reactive form (name, email, currentPassword, newPassword); cross-field validator; password change opens confirmation dialog before submit; submits via `userService.updateUser()`; success toast + form reset of password fields |

**Test classes:**

| Test class | # tests |
|---|---|
| `shared/services/confirmation.service.spec.ts` | resolves `true` on confirm click; resolves `false` on cancel; resolves `false` on backdrop close; passes options through to dialog data; `variant: 'danger'` is propagated |

> `ProfilePageComponent` is not unit-tested per testing strategy. Its cross-field validator is a pure function and tested as part of the form module if extracted; otherwise covered by manual smoke.

**Checkpoint:** Update name → success toast, toolbar reflects (if displayed). Change password with mismatched current password → 401 surfaces via toast (no `SKIP_ERROR_TOAST` here). Cancel confirmation dialog → no submit.

---

## Phase 5 — Reusable Grid component ⬜

**Goal:** Build the reusable `<app-grid>` with declarative columns and a fetcher callback.

**Files:**

| File | Responsibility |
|---|---|
| `src/app/shared/components/grid/grid.types.ts` | `GridColumn<T>`, `GridQuery`, `GridFilterDef`, `GridFilterValue` |
| `src/app/shared/components/grid/grid.component.ts` | `<app-grid [columns] [fetch] [filters] [pageSize]>`. Wraps `mat-table` + `mat-sort` + `mat-paginator`. Owns internal state (page, sortBy/sortOrder, filterValues). Re-fetches on any state change. Filter changes reset to page 1. |
| `src/app/shared/components/grid/grid.component.html` | Material table markup |
| `src/app/shared/components/grid/grid.component.scss` | Token-driven styling |

**Test class:** `shared/components/grid/grid.component.spec.ts`

| # | Test |
|---|---|
| 1 | `Initial render calls fetch with default query` |
| 2 | `Page change calls fetch with new page` |
| 3 | `Page size change calls fetch and resets to page 1` |
| 4 | `Sort change calls fetch with sortBy and sortOrder` |
| 5 | `Filter change calls fetch with filters and resets to page 1` |
| 6 | `Fetcher rejection surfaces empty rows + does not crash` |
| 7 | `Custom cell function renders returned string` |
| 8 | `Non-sortable column header is not clickable` |

**Checkpoint:** All grid tests pass. Component used in Phase 6 against the real `/transactions` endpoint.

---

## Phase 6 — Home page + TransactionsService ⬜

**Goal:** Wire the Grid to `/transactions`, render summary cards from `/transactions/summary`, support row-level Delete with confirmation.

**Files:**

| File | Responsibility |
|---|---|
| `src/app/core/services/transactions.service.ts` | Signals: `transactions`, `summary`, `loading`. Methods: `getPaged(query)`, `getSummary()`, `create(req)`, `update(req)`, `delete(id)` |
| `src/app/pages/home/home-page.component.ts` | Three summary cards (net balance / total income / total expense, formatted as currency); filter UI (category select + date range); `<app-grid>` with transaction columns; "Add Transaction" button (opens modal in Phase 7); row Delete action (confirmation → `delete()` → success toast → grid refresh + summary refresh) |
| `src/app/pages/home/home-page.component.html` | Layout |
| `src/app/pages/home/home-page.component.scss` | Token styling for summary cards (Emerald 500 for positive, Red 500 for expense card) |

**Test class:** `core/services/transactions.service.spec.ts`

| # | Test |
|---|---|
| 1 | `getPaged() calls GET /transactions with query params and updates signal` |
| 2 | `getSummary() calls GET /transactions/summary and updates summary signal` |
| 3 | `create() POSTs payload and refreshes list` |
| 4 | `update() PUTs payload and refreshes list` |
| 5 | `delete() DELETEs id and refreshes list` |
| 6 | `loading signal toggles around requests` |

**Checkpoint:** Home renders real data. Filter + sort + pagination all hit the backend with correct query params. Delete opens confirmation, removes the row, updates both grid and summary cards.

---

## Phase 7 — Transaction Modal (create + edit) ⬜

**Goal:** Single modal form reused for create and edit, opened from Home.

**Files:**

| File | Responsibility |
|---|---|
| `src/app/pages/home/transaction-modal/transaction-modal.component.ts` | Reactive form (title, amount, category, date); supports `create` mode (no input) and `edit` mode (initialized from a `TransactionDto`); injects `MAT_DIALOG_DATA` for the optional initial transaction; on submit calls `transactionsService.create()` or `update()`; success toast + close dialog with the saved entity |
| `src/app/pages/home/transaction-modal/transaction-modal.component.html` | Form layout |
| `src/app/pages/home/home-page.component.ts` (update) | "Add Transaction" button opens modal in create mode; row Edit action opens it in edit mode; on close with a saved entity → refresh grid + summary |

**Test class:** `pages/home/transaction-modal/transaction-modal.component.spec.ts`

| # | Test |
|---|---|
| 1 | `Create mode: empty form, date defaults to today` |
| 2 | `Edit mode: form pre-fills from MAT_DIALOG_DATA` |
| 3 | `Validators: title required, amount > 0, category required` |
| 4 | `Submit in create mode calls create() and closes with saved entity` |
| 5 | `Submit in edit mode calls update() and closes with saved entity` |
| 6 | `Negative amount is rejected client-side` |
| 7 | `Server errors map onto fields via applyServerErrors` |

**Checkpoint:** Full CRUD works end to end. Add → row appears, summary updates. Edit → row updates in place. Delete → row gone.

---

## Phase 8 — Polish + README ⬜

**Goal:** Loading/empty states, finalized toast styling, documentation.

**Actions:**
- [ ] Loading states: spinner over grid during fetch; submit buttons disabled while in-flight.
- [ ] Empty state for the grid ("No transactions yet — add your first one").
- [ ] Verify `.toast-success`, `.toast-error`, `.toast-info` `panelClass` styles match Evergreen tokens (Emerald 500 / Red 500 / Slate 500 backgrounds, white text, 8px radius, soft shadow).
- [ ] Update `frontend/README.md`: dev setup (`npm install`, `npm start`, backend dependency), API base URL config, demo credentials (`demo@example.com` / `demo1234`), test command, Vitest-vs-Jasmine note if applicable.
- [ ] Manual smoke checklist in README:
  1. Register a new user → arrives on Home.
  2. Logout → land on Login.
  3. Login as `demo@example.com` → seeded transactions visible.
  4. Filter by category, by date range, sort by amount, paginate.
  5. Add transaction → appears, summary updates.
  6. Edit transaction → updates in place.
  7. Delete transaction (confirm) → removed; cancel confirmation → not removed.
  8. Profile: change name → toolbar reflects; change password (with confirmation) → next login uses new password.
  9. Hard-refresh on `/home` → bootstrap probe keeps user logged in.
  10. Wait for access token to expire (15 min) → next request silently refreshes.

**Checkpoint:** App is demo-ready.

---

## Progress Summary

| Phase | Description | Status |
|---|---|---|
| 0 | Project setup (SCSS, Material, tokens, models, app.config) | ⬜ |
| 1 | Core HTTP + Auth scaffolding (services, interceptors, utils) | ⬜ |
| 2 | Layout shells + routing tree | ⬜ |
| 3 | Login + Register pages | ⬜ |
| 4 | Profile page + Confirmation Dialog | ⬜ |
| 5 | Reusable Grid component | ⬜ |
| 6 | Home page + TransactionsService | ⬜ |
| 7 | Transaction Modal (create + edit) | ⬜ |
| 8 | Polish + README | ⬜ |
