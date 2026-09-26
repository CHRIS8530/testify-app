# Milestone 5: Deployment & Documentation

## Overview
M5 covers deployment infrastructure, README documentation, OpenAPI specs, and CI/CD pipeline tuning.

---

### Decision 26: M5 Deployment — Render PostgreSQL + EF Core Migration Blockers

**Context:**
Attempted M5 cloud deployment on Render free tier. Set up PostgreSQL database successfully but hit EF Core migration conflicts preventing application startup.

**What I asked:**
Deploy API to cloud: set up Render PostgreSQL, configure connection string, migrate database, deploy frontend.

**What it gave me:**
Render PostgreSQL database created successfully, connection string configured in appsettings.json.

**What I changed:**
Connected to Render External Database URL. Attempted `dotnet ef database update` but hit EF Core pending changes warning.

**What I did not understand at first:**
EF Core 10.0.12 runtime vs 10.0.1 tools version mismatch created migration validation conflicts with Render's empty database. This requires either updating tools or creating a fresh migration in the new environment.

**Status:**
PARTIAL — Render PostgreSQL working, connection configured. EF Core migrations need resolution before full deployment.

**Blockers:**
- EF Core tool version mismatch
- Migration validation against empty cloud database
- Time constraint (deadline approaching)

**Workaround:**
Deploy with local docker-compose for now. Cloud deployment deferred to post-deadline infrastructure work.

**Decision:**
Focus remaining time on finalizing M1-M4 (complete) and documenting deployment process. M5 cloud setup partially attempted but requires additional EF Core troubleshooting.

---

### Decision 27: EF Core Migration Blocker — RESOLVED

**Date:** September 26, 2026

**Context:** Decision 26 identified EF Core 10.0.12 vs 10.0.1 tools mismatch as the blocker preventing Render deployment.

**What I asked the AI:** "How do we fix the EF Core version mismatch and apply migrations to Render?"

**What it gave me:** Update the global dotnet-ef tool, create a new migration, apply it with the Render connection string.

**What I changed and why:** Followed exactly. The fix was simple once we understood the root cause.

**What I did not understand at first:** That updating one tool (dotnet-ef) would resolve the entire validation chain. I initially thought the problem was in the code or the database, but it was just the tooling version.

**Result:** RESOLVED. All three migrations (InitialCreate, AddAuthenticationTables, RenderDeployment) successfully applied to Render PostgreSQL. Cloud deployment path is now clear.

**Commit:** deb1b29

---

### Decision 28: Health Check Endpoint — Deployment Infrastructure

**Date:** September 26, 2026

**Context:** Deployment platforms need a way to monitor if the application is alive and the database is accessible. Without a health check, deployment platforms can't properly manage the app or trigger restarts when something fails.

**What I asked the AI:** "Build a health check endpoint that tests if the API and database are working. It should return HTTP 200 if healthy, 503 if not."

**What it gave me:** Simple GET endpoint at `/api/v1/health` that executes `SELECT 1` on the database and returns JSON with status, timestamp, and database connection state.

**What I changed and why:** Kept it exactly as suggested. The simplicity is the strength — it does one thing well and doesn't need embellishment.

**What I did not understand at first:** That this endpoint becomes the heartbeat of the deployed system. Every deployment platform (Render, Fly.io, etc.) uses health checks to know if your app is alive. Without it, monitoring and auto-restart capabilities don't work.

**Result:** Health check endpoint live and working. Returns 200 with `{"status":"healthy","timestamp":"...","database":"connected"}` when both API and database are reachable. Returns 503 with database disconnected if there's a problem.

**Commit:** 26347b5

---

**Note:** This log is living. New decisions will be added as work continues.

Last updated: September 24, 2026