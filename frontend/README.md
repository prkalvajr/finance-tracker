# Personal Finance Tracker Frontend

Angular frontend for the Personal Finance Tracker. The app uses Angular Material, reactive forms, signals for local app state, and HttpOnly cookie authentication against the .NET backend.

## Prerequisites

- Node.js and npm
- The backend API running locally
- PostgreSQL configured for the backend seed data

The default API base URL is `http://localhost:5283`. Update `src/app/core/http/api-config.ts` if your backend runs on a different URL.

## Setup

```bash
npm install
npm start
```

Open `http://localhost:4200/`.

The backend must be running for login, register, profile, and transaction requests to work. Cookies are sent with `withCredentials: true`, so keep the frontend and backend CORS settings aligned.

## Demo Credentials

- Email: `demo@example.com`
- Password: `demo1234`

## Tests

```bash
npm test
```

This project uses the Angular 21 unit-test builder with Vitest.

## Manual Smoke Checklist

1. Register a new user and confirm the app routes to Home.
2. Logout and confirm the app routes to Login.
3. Login as `demo@example.com` and confirm seeded transactions are visible.
4. Filter by category and date range, sort by amount, and paginate.
5. Add a transaction and confirm it appears in the grid and summary totals update.
6. Edit a transaction and confirm the row updates.
7. Delete a transaction after confirming, then retry delete and cancel to confirm it stays.
8. Update profile name and confirm the toolbar reflects it.
9. Change password with confirmation and confirm the next login uses the new password.
10. Hard-refresh on `/home` and confirm the bootstrap probe keeps the session.
11. Wait for the 15-minute access token to expire, then make a request and confirm silent refresh succeeds.
