# BeautyStore.IntegrationTests

Integration tests that exercise the full stack against a real database and message bus.

What lives here:
- Repository tests against a test SQL Server / SQLite instance.
- Outbox relay tests: verify domain events are persisted and then published.
- End-to-end slice tests: POST /orders → verify DB row + event emitted.
- Idempotency tests: replay an event twice, assert side-effects occur exactly once.

Uses `WebApplicationFactory<Program>` and a dedicated test database seeded per test run.
