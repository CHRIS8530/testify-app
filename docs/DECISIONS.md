# Testify: Decision Log

A record of every significant decision made during the Testify challenge, including what was asked, what was received, what was changed, and what was learned. This is a working document, updated as decisions are made—not a retrospective.

---

## Decision Template

Use this format for every significant decision (not every prompt, but every architectural choice, technology pick, or thing that took thought):

```
## [Date] - [Decision Title]

**Context:** 
What problem was I solving? What was the trigger?

**What I asked the AI:** 
The substance of the prompt (paraphrased—not a copy-paste, just what I was asking for)

**What it gave me:** 
Summarised—what did the AI produce? How long was it? What was the shape of the response?

**What I changed and why:** 
This is the critical part. What did I alter? Why didn't the AI's suggestion work as-is? What did I remove, add, or rewrite?

**What I did not understand at first:** 
Be honest here. Did you have to read docs to understand it? Did you get an error and have to debug? Did you misread the code? This field is where learning lives.

**Source verification:**
Any docs, RFCs, blog posts, or source code I consulted to verify the decision. Especially important for security and infrastructure.
```

Expected entries: 15-30 by the end of the challenge.

---

## Technology Decisions (Pre-Build)

### Decision 1: API Language - C#/.NET

**Context:** 
Starting the Testify challenge. Need to pick an API language from TypeScript (Node), C#/.NET, Go, or Ruby on Rails. Goal is to learn infrastructure and architecture under pressure while building a real app.

**What I asked the AI:**
Not applicable—this was a human decision made with Claude's input before coding started.

**What it gave me:**
N/A

**What I changed and why:**
Chose C#/.NET over Node (which I already know) to force deeper learning and signal ability to handle unfamiliar stacks. .NET's built-in patterns for security, DI, and configuration will teach me *why* things matter, not just how to implement them.

**What I did not understand at first:**
Haven't built anything yet, but I expect to be confused by:
- How dependency injection wires up in ASP.NET Core
- How Entity Framework migrations differ from raw SQL migrations
- CORS configuration in .NET vs Node

**Source verification:**
Microsoft docs (will consult as we build)

---

### Decision 2: Deployment Platform - Fly.io

**Context:**
Challenge requires completely free deployment that works in real life. Options: Fly.io, Render, AWS free tier, Oracle Cloud.

**What I asked the AI:**
Not applicable—recommendation from Claude before build started.

**What it gave me:**
N/A

**What I changed and why:**
Chose Fly.io because: free tier is generous (3 shared-cpu-1x 256MB VMs, 3GB Postgres storage), no cold starts, learning curve is gentle, and the platform makes container orchestration visible without being overwhelming.

**What I did not understand at first:**
- How Fly.io's regions and scaling work
- Whether 3GB Postgres storage is enough for this project (probably yes, it's a small app)
- Whether free tier networking is sufficient

**Source verification:**
Fly.io pricing page (to be consulted during deployment phase)

---

### Decision 3: Database ORM - Entity Framework Core

**Context:**
Building with C#/.NET. Need an ORM for PostgreSQL. Options: EF Core (industry standard), Dapper (lighter), raw queries.

**What I asked the AI:**
Not applicable—recommendation from Claude before build started.

**What it gave me:**
N/A

**What I changed and why:**
Chose Entity Framework Core because:
- Industry standard for .NET + PostgreSQL
- Migrations teach database versioning (you see generated SQL)
- LINQ forces deliberate query thinking
- Parameterized queries by default (security)
- Will hit performance issues (N+1) and learn to fix them deliberately

**What I did not understand at first:**
- How LINQ generates SQL under the hood
- Difference between lazy loading, eager loading, and explicit loading
- How to debug generated SQL queries
- When it's better to use raw SQL vs LINQ

**Source verification:**
EF Core docs (will consult during API build)

---

### Decision 4: End-to-End Testing - Playwright

**Context:**
Challenge requires 6+ end-to-end tests covering critical paths. Options: Playwright, Cypress, Selenium.

**What I asked the AI:**
Not applicable—recommendation from Claude before build started.

**What it gave me:**
N/A

**What I changed and why:**
Chose Playwright because:
- Works with any frontend (not JavaScript-specific)
- Better debugging than Cypress
- Faster and more reliable waits
- Can record and replay tests
- Cross-browser support if needed later

**What I did not understand at first:**
- How to structure E2E tests to avoid brittle selectors
- How to handle async state in React during testing
- Database state cleanup between tests
- How to mock vs. test with real backend

**Source verification:**
Playwright docs (will consult during testing phase)

---

### Decision 5: Unit/Integration Testing - xUnit

**Context:**
Challenge requires unit and integration tests for business logic, status transitions, and authorization rules. Need .NET testing framework.

**What I asked the AI:**
Not applicable—recommendation from Claude before build started.

**What it gave me:**
N/A

**What I changed and why:**
Chose xUnit because:
- Industry standard for .NET (what employers expect)
- Clean syntax, good async/await support
- TestContainers integration (spin up real Postgres in Docker for integration tests)
- Better than MSTest for this use case

**What I did not understand at first:**
- How TestContainers work and whether it'll slow down CI/CD
- How to structure integration tests to use real database without pollution
- Mocking vs. integration test trade-offs
- How async tests work in xUnit

**Source verification:**
xUnit docs, TestContainers docs (will consult during testing phase)

---

## Build Phase Decisions

(To be filled in as we build, starting with M1 foundations)

---

## Learning Log: Where AI Went Wrong

(Times when I was confidently given wrong advice, hallucinations, outdated patterns, or code that looked right but broke)

---

**Last updated:** [Today's date: 09/03/2026]
**Next review:** Before we start M1 (Foundations phase)
## 6. Docker Compose for Local Development (M1)
- **Decision:** Use Docker Compose with 3 services (API, Frontend, Postgres) for local development
- **Why:** Consistency across dev/demo, no cloud costs, full control, mirrors production setup
- **What was asked:** M1 requires working local environment
- **What we built:** Multi-stage API Dockerfile, Node/Vite frontend Dockerfile, docker-compose.yml with health checks, service dependencies, networking, volume persistence
- **Status:** Configured and committed; docker testing deferred due to Docker Desktop startup issues but structure validated
