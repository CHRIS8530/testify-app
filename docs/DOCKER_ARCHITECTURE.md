# Testify Docker Architecture

Local development environment using Docker Compose.
All services (API, Frontend, Database) run in containers for consistency.

Generated during decomposition phase before coding.

---

## Overview

The Testify application runs in 3 containers locally:

1. **API Container** — ASP.NET Core application (C#/.NET)
   - Listens on port 5000
   - Connects to PostgreSQL database
   - Runs health check endpoint

2. **Frontend Container** — React application (Node.js)
   - Listens on port 3000
   - Calls API on localhost:5000
   - Built with Vite or Create React App

3. **Database Container** — PostgreSQL
   - Listens on port 5432
   - Persists data to volume
   - Runs migrations automatically on startup

---

## Container Details

### API Container

**Base Image:** mcr.microsoft.com/dotnet/aspnet:8.0 (runtime, multi-stage build)

**Dockerfile Structure:**
- Stage 1 (builder): Build the .NET project, output published files
- Stage 2 (runtime): Copy only published files, run as non-root user
- Final image size: ~200-300 MB (no build tools, source code, or git history)

**Environment Variables:**
- DATABASE_URL: postgres://postgres:password@postgres:5432/testify
- ASPNETCORE_ENVIRONMENT: Development
- JWT_SECRET: (set via .env file, never committed to repo)
- ASPNETCORE_URLS: http://+:5000

**Runs as:** appuser (non-root)

**Exposed Port:** 5000

**Health Check Endpoint:** GET /api/v1/health (returns { status: "ok", database: "connected" })

**Depends On:** postgres service (waits for database to be ready)

**Volume:** None (stateless)

---

### Frontend Container

**Base Image:** node:18-alpine (for build and runtime)

**Dockerfile Structure:**
- Single stage or multi-stage (depends on tooling)
- Install dependencies, build production bundle
- Serve with simple HTTP server or Vite dev server
- Final image size: ~100-150 MB

**Environment Variables:**
- VITE_API_URL: http://localhost:5000/api/v1 (or React env equivalent)
- NODE_ENV: development

**Runs as:** node (non-root, built into node image)

**Exposed Port:** 3000

**Depends On:** api service (frontend waits for API to be healthy before starting)

**Volume:** None (stateless)

---

### Database Container

**Base Image:** postgres:15-alpine (lightweight)

**Environment Variables:**
- POSTGRES_DB: testify
- POSTGRES_USER: postgres
- POSTGRES_PASSWORD: (from .env file)

**Exposed Port:** 5432

**Volume:** postgres_data (persists database files across container restarts)

**Initialization:**
- On first run, creates testify database
- Optionally runs init script to create schemas

**Migrations:** Applied by API container on startup (Entity Framework Core migrations)

---

## Networking

**Docker Network:** testify-network (created by docker-compose)

**Service Hostnames:**
- api: reachable as `http://api:5000` from other containers
- frontend: reachable as `http://frontend:3000` from other containers
- postgres: reachable as `postgres:5432` from other containers

**From Host Machine:**
- API: http://localhost:5000
- Frontend: http://localhost:3000
- Database: localhost:5432 (only for direct access, not needed for app)

**From Frontend Container:**
- Calls API at: `http://api:5000/api/v1` (internal network)
- Browser still calls: `http://localhost:5000/api/v1` (external URL)

---

## Volumes

**postgres_data Volume:**
- Persists PostgreSQL data across container restarts
- Location on host: Docker managed (automatic, hidden from user)
- Survives `docker-compose down` (must explicitly delete with `docker volume rm`)

**Usage in docker-compose.yml:**

In the `volumes:` section at the bottom of the file:

```yaml
volumes:
  postgres_data:
    driver: local
```

Then in the postgres service section, add a `volumes:` subsection:

```yaml
services:
  postgres:
    volumes:
      - postgres_data:/var/lib/postgresql/data
```

---

## Environment Variables

**File:** .env (in project root, gitignored)

**Example contents:**
DATABASE_URL=postgres://postgres:testify_local_password@postgres:5432/testify
ASPNETCORE_ENVIRONMENT=Development
JWT_SECRET=super_secret_key_change_this_in_production
POSTGRES_PASSWORD=testify_local_password


**Never commit .env to git.** Create .env.example with placeholder values, commit that instead.

---

## How Services Start

**Order of Operations:**

1. User runs: `docker-compose up`
2. Docker Compose creates testify-network
3. Docker Compose creates postgres_data volume (if doesn't exist)
4. **Postgres Container Starts**
   - PostgreSQL initializes
   - testify database created
   - Listens on 5432
   - Health check: can connect to port 5432

5. **API Container Starts**
   - Depends on postgres (waits for it)
   - Downloads/runs dotnet runtime
   - Runs Entity Framework Core migrations (creates tables)
   - Starts ASP.NET server on port 5000
   - Health check: GET /api/v1/health returns 200

6. **Frontend Container Starts**
   - Depends on api (waits for health check)
   - Installs npm dependencies
   - Builds React bundle
   - Starts dev server (Vite) on port 3000
   - Ready to serve on localhost:3000

**User can access:** http://localhost:3000

---

## Database Persistence

**During Development:**
- Data persists in postgres_data volume
- Survives `docker-compose stop` and `docker-compose start`
- Only lost if you run `docker volume rm testify_postgres_data`

**Migrations:**
- EF Core migrations run automatically on API startup
- If schema changes, new migration is created as code
- On next run, migrations auto-apply to existing database
- No manual SQL needed

**Resetting Database (for testing):**
- Option 1: `docker-compose down -v` (removes all volumes, fresh database on next up)
- Option 2: Via API endpoint (if we add one) to drop/recreate schema
- Option 3: Direct SQL (not needed for MVP)

---

## Multi-Stage Build (API)

**Why Multi-Stage:**
- Final image contains only runtime, not build tools
- Reduces image size: 1.5GB (build) → 300MB (runtime)
- Faster deployment, safer (no source code in production image)

**Dockerfile Stages:**

Stage 1 (Builder):

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS builder
WORKDIR /app
COPY . .
RUN dotnet publish -c Release -o out
```

Stage 2 (Runtime):

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
RUN useradd -m appuser && chown -R appuser:appuser /app
COPY --from=builder /app/out .
USER appuser
EXPOSE 5000
CMD ["dotnet", "Testify.Api.dll"]
```

Result: Builder stage discarded, final image only has runtime + published binaries.

---

## docker-compose.yml Structure

Complete docker-compose.yml file:

```yaml
version: '3.8'

services:
  postgres:
    image: postgres:15-alpine
    environment:
      POSTGRES_DB: testify
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 10s
      timeout: 5s
      retries: 5
    networks:
      - testify-network

  api:
    build:
      context: ./api
      dockerfile: Dockerfile
    environment:
      DATABASE_URL: ${DATABASE_URL}
      ASPNETCORE_ENVIRONMENT: ${ASPNETCORE_ENVIRONMENT}
      JWT_SECRET: ${JWT_SECRET}
    ports:
      - "5000:5000"
    depends_on:
      postgres:
        condition: service_healthy
    networks:
      - testify-network

  frontend:
    build:
      context: ./frontend
      dockerfile: Dockerfile
    environment:
      VITE_API_URL: ${VITE_API_URL}
    ports:
      - "3000:3000"
    depends_on:
      api:
        condition: service_healthy
    networks:
      - testify-network

volumes:
  postgres_data:
    driver: local

networks:
  testify-network:
    driver: bridge
```

---

## Common Docker Compose Commands

**Start all services:**
```bash
docker-compose up
```

**Rebuild images and start:**
```bash
docker-compose up --build
```

**Stop services (data persists):**
```bash
docker-compose stop
```

**Stop and remove containers (data persists in volume):**
```bash
docker-compose down
```

**Stop and remove everything including volumes (fresh database next run):**
```bash
docker-compose down -v
```

**View logs:**
```bash
docker-compose logs -f
```

**View logs for specific service:**
```bash
docker-compose logs -f api
```

---

## Image Sizes (Expected)

- **Postgres:** ~80-100 MB (alpine-based)
- **API:** ~250-300 MB (multi-stage, runtime only)
- **Frontend:** ~100-150 MB (node:18-alpine + React bundle)
- **Total Downloaded:** ~500 MB first run

*Sizes will be documented in README.md after build.*

---

## Health Checks

**Postgres Health Check:**
- Command: `pg_isready -U postgres`
- Interval: 10s
- Timeout: 5s
- Retries: 5 (fails after 50s of failures)

**API Health Check:**
- Endpoint: GET /api/v1/health
- Response: `{ "status": "ok", "database": "connected" }`
- Used by frontend to know when API is ready

---

## Local Development Workflow

1. Clone repo: `git clone https://github.com/CHRIS8530/testify-app.git`
2. Copy env: `cp .env.example .env` (fill in values)
3. Start services: `docker-compose up --build`
4. Wait for logs: "Application started" (API), "ready" (frontend)
5. Open browser: http://localhost:3000
6. Make code changes (hot reload in frontend, may need API restart)
7. Stop: Ctrl+C
8. Clean up: `docker-compose down`

---

## Troubleshooting

**Port already in use:**
- Change port mapping in docker-compose.yml
- Or: `lsof -i :5000` (find process), `kill -9 <pid>`

**Database connection refused:**
- Check postgres container logs: `docker-compose logs postgres`
- Verify DATABASE_URL in .env matches service name and password

**API can't reach database:**
- Inside container, database is at `postgres:5432` (not localhost)
- Check docker-compose depends_on and healthcheck

**Frontend can't reach API:**
- Browser calls `http://localhost:5000` (external)
- Frontend container calls `http://api:5000` (internal network)
- Both work if network configured correctly

**Volume permission issues:**
- Ensure API and Frontend run as non-root users
- Database volume auto-managed by Docker