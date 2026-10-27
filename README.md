# Testify

![CI](https://github.com/CHRIS8530/testify-app/actions/workflows/ci.yml/badge.svg)

Testify is a lightweight test run and defect tracker. You create projects, write test cases, run a selection of them and record Pass, Fail or Blocked, raise defects from failed results, and see a dashboard per project. The API is C# on .NET 10 with PostgreSQL, the web app is React with TypeScript.

Built for an AI-assisted engineering challenge. Every significant decision is recorded in [docs/DECISIONS.md](docs/DECISIONS.md) and the security model in [docs/SECURITY.md](docs/SECURITY.md).

**Status:** the API covers all the core features and is tested. The web app is not finished, see Known Limitations below.

## API Endpoints

### Auth
- `POST /api/v1/auth/register` — Register new user
- `POST /api/v1/auth/login` — Login user
- `POST /api/v1/auth/refresh` — Refresh access token
- `POST /api/v1/auth/logout` — Logout user

### Projects
- `GET /api/v1/projects` — List all projects
- `POST /api/v1/projects` — Create project
- `GET /api/v1/projects/:id` — Get project details
- `PATCH /api/v1/projects/:id` — Update project
- `DELETE /api/v1/projects/:id` — Delete project

### Test Cases
- `GET /api/v1/projects/:id/test-cases` — List test cases
- `POST /api/v1/projects/:id/test-cases` — Create test case
- `GET /api/v1/projects/:id/test-cases/:caseId` — Get test case
- `PATCH /api/v1/projects/:id/test-cases/:caseId` — Update test case
- `DELETE /api/v1/projects/:id/test-cases/:caseId` — Delete test case

### Test Runs
- `GET /api/v1/projects/:id/test-runs` — List test runs
- `POST /api/v1/projects/:id/test-runs` — Create test run
- `DELETE /api/v1/projects/:id/test-runs/:runId` — Delete test run

### Test Results
- `GET /api/v1/projects/:id/test-runs/:runId/results` — Get results for run
- `POST /api/v1/projects/:id/test-runs/:runId/results` — Add result
- `PATCH /api/v1/projects/:id/test-runs/:runId/results/:resultId` — Update result

### Defects
- `GET /api/v1/projects/:id/defects` — List defects
- `POST /api/v1/projects/:id/defects` — Create defect
- `PATCH /api/v1/projects/:id/defects/:defectId` — Update defect
- `DELETE /api/v1/projects/:id/defects/:defectId` — Delete defect

### Dashboard
- `GET /api/v1/projects/:id/dashboard` — Get project dashboard metrics

## Configuration

The API reads its settings from configuration, so nothing secret is committed to the repo. These values are needed:

- `ConnectionStrings__DefaultConnection`: the PostgreSQL connection string (Npgsql format)
- `JWT_SECRET`: the key used to sign access tokens
- `CORS_ALLOWED_ORIGINS`: a comma separated list of the frontend addresses allowed to call the API (never `*`). Local development gets `http://localhost:5173` from `api/appsettings.Development.json`. Every other environment must set it.

If the connection string or the CORS origins are missing, the API stops at startup and says so.

### Local development (`dotnet run` or F5)

Store both values with user secrets. They live in your user profile, outside the repo, and only load in the Development environment:

```bash
cd api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=<host>;Port=5432;Database=<db>;Username=<user>;Password=<password>;SSL Mode=Require"
dotnet user-secrets set "JWT_SECRET" "<a long random string>"
```
### Frontend

The frontend reads the API address from `VITE_API_URL`: the origin only, with no path, for example `http://localhost:5137`. For local development it comes from `frontend/.env.development`, which points at `dotnet run` and holds no secrets. Anywhere else, set `VITE_API_URL` in the environment when the frontend is built or the dev server starts. If it is missing, the app stops at startup with a message saying so.

### Docker Compose

Optionally copy `.env.example` to `.env`. Compose passes the connection string to the API container and points it at the Postgres container in the compose file. The defaults in `.env.example` are for local use only. Running the whole stack with Compose is still being fixed (see `docs/CODE_REVIEW.md`, item 1.4).

### Deployed (Render)

Set `ConnectionStrings__DefaultConnection`, `JWT_SECRET` and `CORS_ALLOWED_ORIGINS` (the address of the deployed frontend) as environment variables on the service.

## Running Tests

### Backend Unit Tests

```bash
cd api.tests
dotnet test
```

Expected: all tests passing

### Frontend E2E Tests

```bash
cd frontend
npm run test        # Headless mode
npm run test:ui     # Interactive mode with UI
```

Tests cover:
- User registration and login
- Project creation and listing
- Test case CRUD operations
- Defect creation
- Tab navigation

## Cloud Deployment (Render)

### Status
Partial setup complete. PostgreSQL database provisioned on Render, connection string configured. EF Core migrations require additional troubleshooting.

### What's Done
- Render PostgreSQL database created (`testify-db-2`)
- Connection string supplied through configuration and never committed (see Configuration above)
- Migrations apply automatically when the API starts

### What's Blocked
- EF Core 10.0.12 runtime vs 10.0.1 tools version mismatch
- Migration validation fails on empty cloud database
- Requires either: updating EF Core tools, or creating fresh migration in cloud environment

### Next Steps (Post-Deadline)
1. Update EF Core tools to match runtime version
2. Re-run `dotnet ef database update` against Render
3. Deploy API to Render Web Service
4. Deploy frontend to Vercel
5. Configure HTTPS and custom domain

See [docs/DECISIONS.md](docs/DECISIONS.md) (Decisions 25 to 29) for the technical details.

## Architecture

### Authentication Flow
1. User registers/logs in → API validates credentials
2. API returns JWT access token + httpOnly refresh cookie
3. Frontend stores access token in React state (not localStorage)
4. All API requests include `Authorization: Bearer <token>`
5. On token expiry, frontend silently refreshes via cookie
6. On 401, user redirected to login

### Data Flow
1. Frontend calls API via `useApi` hook (centralized fetch wrapper)
2. Hook handles Bearer token injection, credentials mode, error handling
3. API authenticates, validates, queries database via Entity Framework
4. Response returned as JSON
5. Frontend updates React state via useState/context

## Known Limitations

- The web app is incomplete. Sign in and the project list work. Creating a project from the screen, editing a test case (the screen sends PATCH, the API expects PUT), and the Test Runs, Defects and Dashboard tabs do not work yet. The API behind all of them is implemented and tested.
- `docker compose up` does not run end to end yet: the API port mapping and build context need fixing, and the frontend image still runs the Vite dev server. Docker Desktop would not start on the development machine, so this is to be proven in CI instead.
- The OpenAPI specification is not served yet.
- The Cloud Deployment section below is out of date.
- 6 lint warnings (oxlint, React hook dependencies) are known and not fixed.
- A high severity advisory in `Microsoft.OpenApi` 2.0.0 shows as build warning NU1903, and `npm audit` reports 3 vulnerabilities in the frontend. Neither is triaged yet.
- Security specific limitations are listed in [docs/SECURITY.md](docs/SECURITY.md).

## Decision Log

Every significant decision is recorded in [docs/DECISIONS.md](docs/DECISIONS.md): what I was trying to do, what I asked the AI, what changed and why, and what I did not understand at first.

## Support & Feedback

This project was built as part of an AI-Assisted Engineering Challenge. Questions or feedback should be directed to the project owner.

---

**Last Updated:** October 27, 2026