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

The API reads its settings from configuration, so nothing secret is committed to the repo. Two values are needed:

- `ConnectionStrings__DefaultConnection`: the PostgreSQL connection string (Npgsql format)
- `JWT_SECRET`: the key used to sign access tokens

If the connection string is missing, the API stops at startup and says so.

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

Set `ConnectionStrings__DefaultConnection` and `JWT_SECRET` as environment variables on the service.

## Running Tests

### Backend Unit Tests

```bash
cd api.tests
dotnet test
```

Expected: 4/4 passing

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

See `docs/decisions/Milestone5.md` for full technical details.

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

## Known Issues

### Linting
- 6 ESLint warnings remain (exhaustive-deps on useEffect hooks)
- These are non-critical and can be addressed post-deadline
- All production code compiles and runs without errors

### Dependencies
- 2 npm audit moderate vulnerabilities (Microsoft.OpenApi)
- No active security exploits; recommended for post-deadline update

## Decision Log

All significant decisions documented in `docs/decisions/`:
- **Milestone1.md** — Schema design, auth strategy
- **Milestone2.md** — API architecture, endpoint structure
- **Milestone3.md** — Auth implementation, security decisions
- **Milestone4.md** — Frontend framework choices, component design
- **Milestone5.md** — Deployment strategy, infrastructure decisions

## Support & Feedback

This project was built as part of an AI-Assisted Engineering Challenge. Questions or feedback should be directed to the project owner.

---

**Last Updated:** October 17, 2026