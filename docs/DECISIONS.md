# Testify: Decision Log

A record of every significant decision made during the Testify challenge, including what was asked, what was received, what was changed, and what was learned. This is a working document, updated as decisions are made—not a retrospective.

---

## Decision Template

Use this format for every significant decision (not every prompt, but every architectural choice, technology pick, or thing that took thought):

[Date] - [Decision Title]

Context:
What problem was I solving? What was the trigger?

What I asked the AI:
The substance of the prompt (paraphrased—not a copy-paste, just what I was asking for)

What it gave me:
Summarised—what did the AI produce? How long was it? What was the shape of the response?

What I changed and why:
This is the critical part. What did I alter? Why didn't the AI's suggestion work as-is? What did I remove, add, or rewrite?

What I did not understand at first:
Be honest here. Did you have to read docs to understand it? Did you get an error and have to debug? Did you misread the code? This field is where learning lives.

Source verification:
Any docs, RFCs, blog posts, or source code I consulted to verify the decision. Especially important for security and infrastructure.


Expected entries: 15-30 by the end of the challenge.

---

## Technology Decisions (Pre-Build)

### Decision 1: API Language - C#/.NET

**Context:** 
Starting the Testify challenge. Need to pick an API language from TypeScript (Node), C#/.NET, Go, or Ruby on Rails. Goal is to learn infrastructure and architecture under pressure while building a real app.

**What I asked the AI:**
I already know Node.js really well. For this challenge, should I pick something I'm comfortable with or force myself to learn a new stack? Which would actually teach me more about architecture?

**What it gave me:**
Comparison of learning value vs. speed tradeoff for each language. Analysis of which stacks have stronger built-in patterns for DI, security, and configuration. Recommendation to pick something unfamiliar to maximize learning.

**What I changed and why:**
Chose C#/.NET over Node because the reasoning made sense: I already know how to build fast with Node, but I don't know why patterns work. .NET's opinionated approach would force me to understand DI, configuration, and error handling at a deeper level.

**What I did not understand at first:**
Haven't built anything yet, but I know I'll struggle with:
- How dependency injection actually works in ASP.NET Core
- Entity Framework migrations and how they differ from raw SQL migrations
- CORS configuration in .NET vs Node

**Source verification:**
Microsoft documentation (will consult as we build)

---

### Decision 2: Deployment Platform - Fly.io

**Context:**
Challenge requires completely free deployment that works in real life. Can't use my own server or an old AWS account. Options: Fly.io, Render, AWS free tier, Oracle Cloud.

**What I asked the AI:**
What's the best free hosting option for a full-stack app? I need something that's genuinely free—no surprises when I deploy—and easy enough to learn quickly.

**What it gave me:**
Breakdown of each platform's free tier limits, cold start behavior, ease of learning, and transparency about what's actually happening. Fly.io came out ahead on all fronts.

**What I changed and why:**
Went with Fly.io as recommended. The analysis was specific enough that I felt confident: generous free tier (3 VMs, 3GB Postgres), no cold starts like Render, and the platform teaches you about containers without being overwhelming.

**What I did not understand at first:**
- How Fly.io's regions actually work and whether I need to care
- Whether 3GB Postgres storage is realistic for this app size
- What happens to my app if I exceed free tier limits

**Source verification:**
Fly.io pricing page and docs (will review during deployment)

---

### Decision 3: Database ORM - Entity Framework Core

**Context:**
Building with .NET. Need an ORM for PostgreSQL. Main options: Entity Framework Core (industry standard), Dapper (lightweight), or raw SQL.

**What I asked the AI:**
Should I use Entity Framework Core or Dapper? I want to learn patterns the industry uses, but I also want to understand when to drop down to raw SQL.

**What it gave me:**
Pros and cons of each: EF Core's strengths (LINQ, migrations, tight ASP.NET integration), Dapper's strengths (control, performance, simplicity), when to use each. Recommended EF Core for a challenge focused on understanding architecture.

**What I changed and why:**
Chose Entity Framework Core because:
- It's what .NET shops actually use
- Migrations force you to think about database versioning
- LINQ makes you write queries deliberately (can't just paste SQL)
- Parameterized by default (harder to make security mistakes)
- I'll definitely hit N+1 problems and learn how to fix them

**What I did not understand at first:**
- How LINQ actually compiles to SQL
- The difference between lazy loading, eager loading, and explicit loading
- How to debug what SQL EF Core is actually generating
- When it makes sense to write raw SQL instead

**Source verification:**
Entity Framework Core documentation (consulting as we build)

---

### Decision 4: End-to-End Testing - Playwright

**Context:**
Challenge requires 6+ E2E tests covering critical user flows. Need to pick a tool that works with React frontend + .NET backend.

**What I asked the AI:**
What's the best E2E testing tool for a React frontend with a .NET backend? I want something with good debugging and reliable waits, not flaky tests that fail randomly.

**What it gave me:**
Comparison of Playwright, Cypress, and Selenium focused on reliability, debugging experience, cross-framework support, and performance. Playwright won on all metrics.

**What I changed and why:**
Chose Playwright because the analysis showed it handles async waits better than Cypress, works with any frontend (not just JavaScript), and has better debugging tools. The reliability angle sold me—I don't want to debug flaky tests under time pressure.

**What I did not understand at first:**
- How to write E2E tests that don't break when selectors change
- How to handle React's async state in tests
- Whether to clean up database state between tests or reset the app
- When to mock the backend vs. test with real API calls

**Source verification:**
Playwright documentation (will consult during M4 testing phase)

---

### Decision 5: Unit/Integration Testing - xUnit

**Context:**
Challenge requires unit tests for business logic and integration tests for API endpoints. Need to pick the .NET testing framework that's actually used in industry.

**What I asked the AI:**
What testing framework does the .NET industry actually use? Should I learn xUnit, NUnit, or MSTest? Which one will look good on my resume?

**What it gave me:**
Breakdown of each framework: syntax style, community size, async support, employer familiarity. xUnit came out as the clear industry standard. Also mentioned TestContainers for spinning up real Postgres in tests.

**What I changed and why:**
Chose xUnit because:
- Industry standard (.NET employers actually expect this)
- Clean, readable syntax
- Great async/await support
- TestContainers integration lets us test with real Postgres
- Larger community than MSTest

**What I did not understand at first:**
- How TestContainers actually works—does it slow down the build?
- How to write integration tests that don't pollute each other's state
- Whether I should mock everything or test with real dependencies
- How async test methods actually work in xUnit

**Source verification:**
xUnit and TestContainers documentation (will consult during testing phases)

---

## Build Phase Decisions

### Decision 6: Docker Compose for Local Development (M1)

**Context:** 
M1 (Foundations) needs a working local environment where API, frontend, and database all run together. Can't rely on everyone having Postgres installed locally.

**What I asked the AI:**
How do I get my API, frontend, and database all running together locally without Docker Desktop being a pain? Should I use docker-compose?

**What it gave me:**
Complete docker-compose.yml template with 3 services (Postgres, API, Frontend), plus Dockerfiles for both apps. Included health checks, networking setup, and volume configuration.

**What I changed and why:**
Implemented exactly as suggested. Added one thing myself: `service_healthy` condition on the API to ensure Postgres starts before the API tries to connect.

Used Docker Compose because:
- One command (`docker-compose up`) starts everything
- Mirrors how the app actually runs in production
- No cloud costs during development
- Teaches container networking and orchestration

**What I did not understand at first:**
- How to pass environment variables from docker-compose to the containers
- Why Postgres needs a health check (race condition with startup)
- Whether volumes persist data correctly between runs
- How the services actually communicate with each other by name

**What happened:**
Built multi-stage Dockerfiles for API (.NET) and frontend (Node/Vite). Created docker-compose.yml with proper networking and health checks. Structure is correct and committed. Docker Desktop had startup issues so couldn't test live, but file validation shows correctness.

**Source verification:**
Docker documentation, Docker Compose networking guide

---

### Decision 7: Integration Tests with InMemory Database (M2) - BLOCKED

**Context:**
M2 requires CRUD API with integration tests. Need to test endpoints without spinning up real Postgres. xUnit + WebApplicationFactory is the pattern for this.

**What I asked the AI:**
How do I set up xUnit integration tests using an InMemory database instead of PostgreSQL? I want tests that run fast and don't need Docker.

**What it gave me:**
Approach to modify Program.cs with environment-based conditional DbContext registration (InMemory for "Test" environment, Npgsql for production). WebApplicationFactory setup code with .WithWebHostBuilder() to configure the test environment. 4 sample test cases.

**What I changed and why:**
Implemented exactly as suggested:
1. Added `Microsoft.EntityFrameworkCore.InMemory` NuGet package
2. Modified Program.cs to check environment and conditionally register InMemory vs Npgsql
3. Created test factory that sets "Test" environment
4. Wrote 4 tests: GetProjects, CreateProject (valid), CreateProject (invalid), GetProject (not found)

Tests compile successfully but fail at runtime with: "Unable to resolve service for type 'Microsoft.EntityFrameworkCore.DbContextOptions`1[TestifyDbContext]'"

**What I did not understand at first:**
- Why the environment setting doesn't propagate to Program.cs in time
- How WebApplicationFactory instantiates a new host and re-runs Program.cs
- DI timing and ordering issues specific to .NET 10.0 preview
- Why removing the old DbContextOptions descriptor doesn't prevent Npgsql registration

**Diagnosis:**
Both Npgsql and InMemory providers are being registered simultaneously. EF Core rejects a DbContext with two database providers. The environment check in Program.cs either happens too late or doesn't reach the host builder properly through WebApplicationFactory.

**Status:** 
BLOCKED - Needs review from someone experienced with .NET 10 preview DI patterns. All code is committed for investigation.

**Source verification:**
- Microsoft xUnit and WebApplicationFactory documentation
- Entity Framework Core InMemory database testing guide
- .NET 10 preview release notes (to be consulted)

---

---

**Last updated:** 2026-09-09
**Status:** 5 decisions complete (M1), 1 decision blocked (M2 integration tests - awaiting review)
**Next review:** After integration test blocking issue is resolved (currently unresolved)