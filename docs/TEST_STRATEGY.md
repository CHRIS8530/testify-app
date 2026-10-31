# Testify Test Strategy

This document says what is tested at each level, why, and what is not covered. It describes the tests as they are today, and it is updated when they change.

## Approach

Testing follows risk. The highest risk in a multi user tracker is one user seeing or changing another user's data, so authorization and token handling get the most tests. The pass rate is the most visible number, so it has one rule in one place with its own unit tests. The browser tests are slow and brittle, so they cover only the critical paths a user takes, not every field.

API changes are written test first. The test is run before the change and must fail, then the change is made and the test must pass. The decision log records the evidence (for example the test counts before and after).

## Levels

| Level | What it covers | Where | Why this level |
|---|---|---|---|
| Static checks | Formatting, type errors, lint | `dotnet format`, `tsc`, `oxlint`, run in CI | Cheap, catches a whole class of mistakes before any test runs. A frontend that did not compile once reached main because CI did not type-check. |
| Unit | The pass rate rule (`RunStats.PassRate`), including empty runs and exact halves | `api.tests/RunStatsTests.cs` | Pure arithmetic with edge cases (12.5 must round to 13 everywhere), so it is tested directly and fast. |
| API integration | The real API through HTTP with `WebApplicationFactory` and an in-memory database: authorization for every role, the audit log scope, token hashing and rotation, input validation, CORS, run summaries, the dashboard, defect rules | `api.tests/` (72 tests) | This is where the business rules live. Each test arranges its own users and projects, so tests do not depend on each other. |
| End to end | A real browser against the real API and web app | `frontend/tests/e2e/` (8 tests, Playwright, Chromium) | Proves the screens, the API and the sign in flow work together, which no lower level can. |

## End to end tests

1. A new user can register and lands on the Projects page
2. A registered user can sign in
3. A wrong password is rejected with a clear message
4. A user can create a project
5. A user can create a test case
6. A user can execute a run (Pass, Fail, Blocked), see the live pass rate, and complete the run
7. A user can raise a defect from a failed result and find it on the Defects tab
8. The dashboard shows the pass rate and open defects by severity

Each test creates its own user with a unique email, so there is no shared seeded account and the tests can run in parallel. Setup is done through the API (fast and reliable) and the behaviour under test is done through the browser. Passwords and emails in the tests are throwaway values for a throwaway API.

## Running the tests

- API tests: `dotnet test api.tests`
- Browser tests: start the API in the Test environment (in memory database, higher rate limit), then run `npx playwright test` in `frontend`. The exact commands are in the README.

## Test data

API tests use the in-memory database and a fresh user per test. Browser tests run against an API started in the Test environment, so no real database is touched and nothing needs cleaning up afterwards.

## What is not covered yet

- **A real PostgreSQL.** The in-memory database behaves differently in ways that hid bugs before: string comparison is case sensitive on PostgreSQL, a negative offset is a SQL error, and raw SQL does not run. Integration tests against a real PostgreSQL in a container are planned.
- **Browser tests in CI.** They run locally today. Running them in the pipeline, and requiring them before a merge, is planned.
- **Rate limiting.** The limit exists but has no automated test, and the browser tests raise it.
- **Other browsers.** Only Chromium is used.
- **Role boundaries in the browser.** Editor and viewer limits are tested at the API level only, because there is no screen to invite a member yet.
- **Email.** It is mocked, so password reset and invitations are tested only up to the point the mock receives them.
- **Accessibility and load.** There is no automated check for either.

## Exit criteria

A pull request can merge when the `api` and `frontend` checks are green. Once the browser tests run in CI they join that gate.