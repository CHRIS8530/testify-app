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

Last updated: September 24, 2026