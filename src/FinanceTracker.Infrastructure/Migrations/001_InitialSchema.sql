-- 001_InitialSchema.sql
-- Initial schema for FinanceTracker.

CREATE TABLE IF NOT EXISTS users (
    user_id        SERIAL       PRIMARY KEY,
    name           VARCHAR(200) NOT NULL,
    email          VARCHAR(320) NOT NULL,
    password_hash  VARCHAR(200) NOT NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_users_email
    ON users (email);

CREATE TABLE IF NOT EXISTS transactions (
    transaction_id SERIAL       PRIMARY KEY,
    user_id        INTEGER      NOT NULL REFERENCES users (user_id) ON DELETE CASCADE,
    title          VARCHAR(200) NOT NULL,
    amount         NUMERIC(18,2) NOT NULL,
    category       VARCHAR(20)  NOT NULL,
    date           DATE         NOT NULL,
    deleted        BOOLEAN      NOT NULL DEFAULT FALSE,
    created_at     TIMESTAMPTZ  NOT NULL DEFAULT (NOW() AT TIME ZONE 'UTC'),
    updated_at     TIMESTAMPTZ  NOT NULL DEFAULT (NOW() AT TIME ZONE 'UTC')
);

CREATE INDEX IF NOT EXISTS ix_transactions_user_deleted_date
    ON transactions (user_id, deleted, date);

CREATE TABLE IF NOT EXISTS refresh_tokens (
    id          SERIAL       PRIMARY KEY,
    user_id     INTEGER      NOT NULL REFERENCES users (user_id) ON DELETE CASCADE,
    token       VARCHAR(512) NOT NULL,
    expires_at  TIMESTAMPTZ  NOT NULL,
    revoked_at  TIMESTAMPTZ  NULL
);

CREATE INDEX IF NOT EXISTS ix_refresh_tokens_token
    ON refresh_tokens (token);
