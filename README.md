
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
- Render PostgreSQL database created (`testify-db`)
- External connection string configured in `api/appsettings.json`
- Connection tested (network connectivity working)

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

**Last Updated:** September 24, 2026