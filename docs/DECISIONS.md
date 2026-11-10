# Testify Project — Decision Log

These documents record every significant decision made during the AI-Assisted Engineering Challenge. For each decision: context, what was asked of the AI, what it produced, what was changed and why, and what wasn't understood at first.

---

## Decision Template

Use this format for every significant decision (not every prompt, but every architectural choice, technology pick, or thing that took thought):

**Context:** What problem was I solving? What was the trigger?

**What I asked the AI:** The substance of the prompt (paraphrased—not a copy-paste, just what I was asking for)

**What it gave me:** Summarised—what did the AI produce? How long was it? What was the shape of the response?

**What I changed and why:** This is the critical part. What did I alter? Why didn't the AI's suggestion work as-is? What did I remove, add, or rewrite?

**What I did not understand at first:** Be honest here. Did you have to read docs to understand it? Did you get an error and have to debug? Did you misread the code? This field is where learning lives.

**Source verification:** Any docs, RFCs, blog posts, or source code I consulted to verify the decision. Especially important for security and infrastructure.

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

### Decision 2A: Fly.io Free Tier Removed

**Context:** Decision 2 chose Fly.io based on permanent free tier. When preparing for deployment, discovered Fly.io no longer offers permanent free tier to new accounts—they changed their pricing model mid-challenge.

**What I asked the AI:** "Fly.io removed their free tier. What hosting platforms still have genuine free deployment?"

**What it gave me:** Analysis of AWS free tier and Render as alternatives to Fly.io.

**What I changed and why:** Decided to try AWS first, based on prior familiarity with the platform.

**What I did not understand at first:** That payment verification issues could block deployment, even with proper documentation.

**Result:** AWS attempt blocked by payment verification. Pivoted to Render (see Decision 2B).

---

### Decision 2B: AWS Payment Verification Blocker — Pivoted to Render

**Context:** After Fly.io's free tier removal, attempted AWS free tier as the next platform. Hit payment verification issues that couldn't be resolved quickly.

**Blocker:** AWS kept failing payment card verification during signup. After back-and-forth with support, the issue remained unresolved and was eating time.

**What I asked the AI:** "AWS payment verification is stuck. What's my fallback hosting option that actually has free tier?"

**What it gave me:** Render—genuine free tier for one year, 750 hours/month compute, free PostgreSQL database included, simpler signup process.

**What I changed and why:** Abandoned AWS and switched to Render immediately. It met all technical requirements without the payment friction, and the signup was straightforward.

**What I did not understand at first:** That payment verification can be a real blocker for new accounts, even with correct documentation. Some platforms just have friction that costs time. Render's approach was deliberately simpler.

**Result:** Render PostgreSQL database provisioned successfully on first try. All EF Core migrations applied without issues (after fixing tool version mismatch in Decision 26).

**Lesson:** When free platforms become friction, switch faster. The sunk cost of debugging payment verification isn't worth it if alternatives exist.

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

**Last updated:** 2026-09-10
**Status:** Milestone 1 complete. 6 foundational decisions locked in. Ready for Milestone 2.
**Next:** Start Milestone 2 (CRUD API, integration tests)

# Testify: Decision Log - Milestone 2

**Milestone:** Days 3-5
**Status:** All entities have working CRUD, validation, pagination, OpenAPI spec served, integration tests green (5/5 passing).

---

### Decision 7: Integration Tests with InMemory Database (M2) - RESOLVED

**Context:**
M2 requires CRUD API with integration tests. Need to test endpoints without spinning up real Postgres. xUnit + WebApplicationFactory is the pattern for this.

**What I asked the AI:**
How do I set up xUnit integration tests using an InMemory database instead of PostgreSQL? I want tests that run fast and don't need Docker.

**What it gave me:**
Approach to modify Program.cs with environment-based conditional DbContext registration (InMemory for "Test" environment, Npgsql for production). WebApplicationFactory setup code with `.WithWebHostBuilder()` to configure the test environment. 4 sample test cases.

**What I changed and why:**
Implemented exactly as suggested:
1. Added `Microsoft.EntityFrameworkCore.InMemory` NuGet package
2. Modified Program.cs to check environment and conditionally register InMemory vs Npgsql
3. Created test factory that sets "Test" environment
4. Wrote 4 tests: GetProjects, CreateProject (valid), CreateProject (invalid), GetProject (not found)

Tests compiled. All 4 failed at runtime. Issue diagnosed and fixed by Eric using diagnostic logging.

**What I did not understand at first:**

- **How WebApplicationFactory runs my app.** It cannot reach into a top-level-statements `Program.cs`, so it re-runs the entry point and passes its configuration in as command-line arguments. That is what the `args=[--environment=... --contentRoot=... --applicationName=...]` line is. This also means `WebApplication.CreateBuilder(args)` must be handed `args`. If I ever write `CreateBuilder()` with no argument, every one of these test overrides silently stops working.
- **`ASPNETCORE_ENVIRONMENT` vs `environment`.** The prefix belongs to the environment variable, not to the configuration key.
- **Validate-on-build is environment-dependent.** Development validates the whole service graph at `Build()`; other environments do not. A DI mistake can therefore be loud locally and silent in production.
- **That a "diagnosis" is a claim requiring evidence.** Measurement (print statements) replaces theory faster than speculation.

**Status:**
RESOLVED - fix applied by Eric, M2 integration tests green (5/5 passing).

**Source verification:**
- ASP.NET Core hosting: `WebHostDefaults.EnvironmentKey` and the `ASPNETCORE_` prefix-stripping behaviour of the environment-variable configuration provider
- `Microsoft.AspNetCore.Mvc.Testing` source (`WebApplicationFactory`, `DeferredHostBuilder`)
- .NET generic host docs on `ValidateOnBuild` / `ValidateScopes`

---

### Decision 8: Fix 4 Production Bugs Found in M2 Code (Beyond Scope)

**Context:**
After M2 integration tests passed, Eric's tool M identified 4 bugs in the controller code. Eric noted them but explicitly said "we'll leave it. Glaringly obvious, but changes nothing for you." They don't block M3. I asked AI to find and fix them anyway.

**What I asked the AI:**
Find and fix any glaringly obvious bugs. I want to understand what they are and fix them now.

**What it gave me:**
Analysis of 4 specific bugs:
1. DashboardController & DefectsController use nested `_db.TestCases.Any()` queries (don't translate to efficient SQL)
2. ProjectsController has redundant validation logic that calls `.Length` on potentially null string
3. Program.cs health endpoint calls `ExecuteSqlRawAsync()` which fails on InMemory database

**What I changed and why:**
Replaced all nested `Any()` queries with pre-fetched ID lists and `Contains()`, which translates to proper SQL joins. Split the validation logic in ProjectsController into separate checks with individual error messages. Added database type detection in health endpoint to use `EnsureCreatedAsync()` for InMemory and `ExecuteSqlRawAsync()` for Npgsql.

**What I did not understand at first:**
- Why nested `Any()` is inefficient (I thought EF Core would optimize it)
- That InMemory database doesn't support raw SQL operations
- That deferring tech debt is sometimes the right call, and pushing back to understand why matters more than fixing everything immediately

**Status:**
RESOLVED - All 4 bugs fixed and tested. All 5 integration tests passing. But note: Eric had good reason to defer these. Worth reflecting on whether fixing immediately was the right call.

**Source verification:**
- Entity Framework Core performance best practices
- .NET database API documentation

---

### Decision 9: Package Vulnerability - Microsoft.OpenApi

**Context:**
Build warnings showed Microsoft.OpenApi 2.0.0 has a known high-severity vulnerability (GHSA-v5pm-xwqc-g5wc). Needed to decide whether to fix or defer.

**What I asked the AI:**
Will this vulnerability leave us open to attack in a showcase app?

**What it gave me:**
Analysis that it's low-risk for a showcase (it's a documentation endpoint, not auth), but should still be fixed as a matter of process and awareness.

**What I changed and why:**
Upgraded to Microsoft.OpenApi 2.1.0 explicitly in the project. Vulnerability still exists in 2.1.0 (it's a broader issue with the package), so we'll note this in the final SECURITY.md as a known limitation.

**What I did not understand at first:**
That vulnerabilities can exist across multiple versions and may require waiting for upstream fixes, not just upgrading.

**Status:**
PARTIAL - Upgraded but vulnerability persists. Will document as known limitation in M3.

---

### Decision 10: JWT Token Implementation

**Context:**
M3 requires JWT tokens for authentication. Needed to implement token generation and validation with specific constraints: 15-minute access tokens, HS256 signing, token validation with expiry.

**What I asked the AI:**
Build a JwtService that generates and validates JWT tokens with these specs: 15-min access, HS256, claims for userId/email/username.

**What it gave me:**
Complete service with GenerateAccessToken and ValidateToken methods, proper signing with symmetric key, claims extraction, exception handling.

**What I changed and why:**
Implemented exactly as provided. Added System.IdentityModel.Tokens.Jwt NuGet package. Service is clean and testable (interface-based).

**What I did not understand at first:**
- How ClockSkew works (it's time tolerance for expiry validation)
- Why we need both issuer and audience validation (prevents token reuse in different contexts)
- That returning null on validation failure (instead of throwing) makes the caller's job easier

**Status:**
RESOLVED - JwtService complete, builds successfully, ready for AuthController integration.

**Source verification:**
- System.IdentityModel.Tokens.Jwt documentation
- JWT best practices (HS256, token expiry, claims structure)

---

### Decision 11: Password Hashing with Bcrypt Cost 10

**Context:**
Authentication requires secure password hashing. Chose bcrypt with cost factor 10 per decomposition.

**What I asked the AI:**
Implement PasswordService with bcrypt, cost factor 10. Simple interface: HashPassword and VerifyPassword.

**What it gave me:**
Service using BCrypt.Net-Next package, HashPassword method, VerifyPassword with try-catch for invalid hashes.

**What I changed and why:**
Used exact implementation. Had to add NuGet package and correct the BCrypt API calls (BCrypt.Net.BCrypt namespace).

**What I did not understand at first:**
- The correct API for BCrypt.Net-Next (it's BCrypt.Net.BCrypt, not just BCrypt)
- That catch-all exception handling in VerifyPassword is appropriate (invalid hash formats throw)

**Status:**
RESOLVED - PasswordService complete, cost factor 10 confirmed, integrated into AuthController.

---

### Decision 12: Mock Email Service for MVP

**Context:**
M3 password reset and project invitations require email sending. For MVP, mock service logs to console instead of real SMTP.

**What I asked the AI:**
Create a mock email service that logs password reset and invitation emails to the console for development.

**What it gave me:**
MockEmailService implementing IEmailService with two methods, logging details to ILogger.

**What I changed and why:**
Implemented as provided. Discovered and fixed CA2017 logging parameter mismatch (duplicate placeholders).

**What I did not understand at first:**
- How to format logging messages correctly with correct parameter counts
- That mock logging is valuable during development (no SMTP setup needed)

**Status:**
RESOLVED - MockEmailService logs to console, can be swapped for real SMTP implementation later.

---

### Decision 13: Audit Logging Service for Full Transparency

**Context:**
M3 requires audit logging of all user actions for security and observability. Every action (login, register, invite, etc) gets logged with user, action type, resource, details, timestamp, IP address.

**What I asked the AI:**
Create AuditLogService that logs actions to the AuditLogs table with safe error handling.

**What it gave me:**
Service that takes userId, action, resourceType, resourceId, projectId, details (dict), ipAddress and stores in database as JSON.

**What I changed and why:**
Implemented as provided. Uses JsonSerializer to store details dictionary as JSON string. Wraps in try-catch to prevent logging errors from crashing the app.

**What I did not understand at first:**
- That Dictionary<string, object> needs to be JSON-serialized for database storage
- Why we catch exceptions silently in logging (you never want logging to break the app)

**Status:**
RESOLVED - AuditLogService integrated into all auth endpoints, logs all actions with full context.

---

### Decision 14: AuthController - 6 Authentication Endpoints

**Context:**
Core authentication layer requires 6 endpoints: register, login, refresh, logout, forgot-password, reset-password. Each needs validation, error handling, audit logging, email sending.

**What I asked the AI:**
Build AuthController with all 6 endpoints, full validation, JWT token handling, refresh token httpOnly cookies, password reset flow, email sending, audit logging.

**What it gave me:**
Complete controller with all 6 endpoints, detailed validation for each, httpOnly cookie handling, password hashing, refresh token revocation on reset, comprehensive error messages.

**What I changed and why:**
Implemented exactly as provided. Added missing `using Microsoft.EntityFrameworkCore;` directive.

**What I did not understand at first:**
- How httpOnly cookies work (secure flag prevents JavaScript access, sameSite prevents CSRF)
- Why forgot-password always returns 200 even if email doesn't exist (security: don't leak email addresses)
- Why reset-password revokes all active refresh tokens (forcing re-login after password change is secure)

**Status:**
RESOLVED - AuthController complete with 6 endpoints, all integrated with JwtService, PasswordService, EmailService, AuditLogService.

---

### Decision 15: ProjectMembersController - Invitations and Removal

**Context:**
Project access management requires endpoints for owner to invite editors/viewers and remove members. Only owner can perform these actions.

**What I asked the AI:**
Build ProjectMembersController with invite and remove endpoints. Owner-only. Send invitation emails. Full audit logging.

**What it gave me:**
Controller with two endpoints: POST /invite (validate email, check ownership, add member, send email) and DELETE /{userId} (owner check, remove member).

**What I changed and why:**
Implemented as provided. Had to add AddedAt property to ProjectMember model (was missing in initial definition).

**What I did not understand at first:**
- That we need to extract userId from JWT claims in controllers (User.FindFirst(ClaimTypes.NameIdentifier))
- Why invitation flow stores member first, then sends email (better to have the record exist even if email fails)

**Status:**
RESOLVED - ProjectMembersController complete, owner-only authorization enforced, audit logging integrated.

---

### Decision 16: Integration Tests for Auth Endpoints (M3) - RESOLVED

**Context:**
M3 requires integration tests for auth-protected endpoints (ProjectsController). Same pattern as M2: xUnit + WebApplicationFactory + InMemory database.

**What I asked the AI:**
Build ProjectsControllerTests with WebApplicationFactory, replace PostgreSQL DbContext with InMemory for isolation.

**What it gave me:**
Test class with factory setup, service registration, test methods for GetProjects, CreateProject (unauthorized), GetProject (not found).

**What I changed and why:**
Went through multiple failed attempts before landing on the fix:
1. Tried removing DbContextOptions/DbContext descriptors manually before adding InMemory — provider conflict persisted regardless
2. Tried `builder.UseEnvironment("Test")` — compiled error, method not found on IWebHostBuilder
3. Tried `services.RemoveAll()` — same provider conflict as attempt 1

Root cause: Program.cs already had correct conditional DbContext registration (Test → InMemory, else → Npgsql) from the M2 pattern. The actual fix needed:
1. `using Microsoft.AspNetCore.Hosting;` — missing using directive, without it `UseEnvironment()` doesn't resolve even though the method exists
2. `builder.UseEnvironment("Test")` actually called (earlier attempts never activated the InMemory branch at all)
3. In-memory configuration injection for `JWT_SECRET` — test host doesn't load appsettings.json, and JwtService's constructor requires this value

Final working setup:
```csharp
builder.UseEnvironment("Test");
builder.ConfigureAppConfiguration((context, config) =>
{
    config.AddInMemoryCollection(new Dictionary<string, string?>
    {
        { "JWT_SECRET", "test-secret-key-for-integration-tests-only-not-for-production-use" }
    });
});
```

**What I did not understand at first:**

- **That `UseEnvironment` needs its own using directive.** It's an extension method on `IWebHostBuilder` living in `Microsoft.AspNetCore.Hosting` — without the using statement, the compiler can't find it even though IntelliSense sometimes suggests it.
- **That removing a service registration doesn't undo which code path already ran.** Program.cs's `if/else` on environment had already executed by the time test code tries to remove services — the problem wasn't "which DbContext is registered" but "which branch of Program.cs's conditional fired in the first place."
- **That the test host is isolated from appsettings.json by default.** Services requiring configuration values (like JwtService needing JWT_SECRET) need those values injected explicitly via `ConfigureAppConfiguration`, they don't fall through from the main app's config files.

**Status:**
RESOLVED - 4/4 integration tests passing (GetProjects, CreateProject unauthorized, GetProject not found, plus one more).

**Source verification:**
- ASP.NET Core hosting: `IWebHostBuilder.UseEnvironment` extension method location
- `Microsoft.AspNetCore.Mvc.Testing` docs on `WithWebHostBuilder` and `ConfigureAppConfiguration`
- Decision 7 (M2) — related but distinct issue in the same WebApplicationFactory + environment-switching pattern; M2's blocker was environment/DI validation, M3's was a missing using directive and test-host configuration injection

**Reflection:**
Related to Decision 7 (M2), which used the same WebApplicationFactory + environment-switching pattern. In hindsight, checking whether the environment switch was actually firing (via a quick log line) before assuming a database provider conflict would have narrowed this down faster. Noted for next time: verify environment activation early when WebApplicationFactory tests misbehave, since it's a common failure point in this pattern.

---

### Decision 17: TestRunResults CRUD and Dashboard Security Gap — Found and Fixed Retroactively (Post-M3)

**Context:**
While preparing for M4 (frontend), I reviewed REACT_COMPONENTS.md (written during decomposition, before any backend code existed) against the actual API built in M2/M3. Two gaps surfaced that belonged to M2's original scope ("All entities have CRUD") but were missed at the time:

1. **No controller existed for TestRunResult** — the model (`TestRunResult.cs`) was created in M2 alongside the other entities, but no endpoints were ever built to create or update per-test-case results within a run. This meant the core "execute a test run" feature (mark each case Pass/Fail/Blocked/Not Run) had no backend support at all.

2. **DashboardController had no authorization check** — it was built (date: Sept 10) but never had a `GetUserIdFromToken()` or `ProjectMembers` membership check added, even after M3 added authorization to every other controller. Any authenticated user could view any project's dashboard regardless of membership. It also used a different response shape (`{ data: {...}, status: 200 }`) than every other endpoint in the API, which would have caused inconsistent frontend parsing logic in M4.

**What I asked the AI:**
Reviewed the discrepancy between REACT_COMPONENTS.md and the actual backend controllers, and asked for a controller to manage TestRunResult CRUD (list results, add test cases to a run, update individual result status), plus a fix for DashboardController's missing auth check and inconsistent response shape.

**What it gave me:**
New `TestRunResultsController` with three endpoints: `GET /results` (list), `POST /results` (add test cases to a run, creating "Not Run" placeholder results), `PATCH /results/{id}` (update result status and notes). Also a corrected `DashboardController` with the same `GetUserIdFromToken()` + `ProjectMembers` pattern used elsewhere, and the `data`/`status` wrapper removed to match the rest of the API.

**What I changed and why:**
Implemented as provided. Chose "Option B" — matching Dashboard's shape to the rest of the API (plain objects, no wrapper) rather than "Option A" (wrapping every other endpoint to match Dashboard). Option B required editing one file instead of five, and plain objects without a wrapper is what the rest of the API already used everywhere else, so consistency favored the smaller change.

**What I did not understand at first:**
- That a model existing in the DbContext (TestRunResult) does not mean a controller was ever built for it — the model was created for relationships/EF Core mapping purposes in M2, but nobody had gone back to check whether every model actually had a corresponding controller.
- That "it builds successfully" is not the same as "it's complete" — DashboardController compiled and ran fine for weeks with a security hole because C# doesn't complain about a missing authorization check the way it complains about a missing semicolon.
- That documentation written during decomposition (REACT_COMPONENTS.md) is a genuinely useful audit tool later — comparing planned API contracts against what was actually built surfaced two real gaps I would not have found by just re-reading the M2/M3 code in isolation.

**Status:**
RESOLVED — TestRunResultsController built and tested (build succeeds), DashboardController secured and shape-corrected. Discovered during M4 preparation, retroactively filed under M2 since that's where entity CRUD completeness belonged.

**Note:** This decision is filed after M3 was already marked complete. Numbering reflects when the gap was found, not the original M2 timeline. Left as an honest record of the gap rather than silently folding it into M2's original decisions.

---

### Decision 18: First PR Workflow — CI Was Broken - Found and Fixed

**Context:**
After adopting the PR-based workflow per the brief's requirement (section 3.6), the very first PR (CORS fix) exposed that CI had actually been failing. Direct-to-main commits meant nobody was watching CI status, so this went unnoticed.

**What I asked the AI:**
Investigate why the "Lint TypeScript frontend" CI step was failing, using an analysis that had already been generated (whitespace formatting errors + an oxlint native binding error) as a starting point.

**What it gave me:**
A methodical verification process rather than blindly applying the pasted fix: checked the actual CI logs directly on GitHub, ran `dotnet format --verify-no-changes` locally to confirm the whitespace claims were real (they were), then traced the oxlint failure to its actual root cause step by step.

**What I changed and why:**
1. Ran `dotnet format` to fix real whitespace issues in `EmailService.cs` and `TestifyDbContext.cs` — confirmed via `--verify-no-changes` before and after.
2. Applied the suggested fix of clearing `node_modules`/`package-lock.json` before `npm install` in CI — this did NOT fix the oxlint error, contrary to the original analysis's confidence.
3. Diagnosed the real cause myself: CI was pinned to Node 18, but local dev machine runs Node 22. Oxlint's native binding resolution for a recent version (`^1.79.0`) failed specifically on the older Node version in CI. Bumped CI's `node-version` from `'18'` to `'22'` to match local — this fixed it. CI went green for the first time.
4. Found and removed a stray duplicate `Controllers/TestRunResultsController.cs` sitting at the repo root (outside `api/Controllers/`) — leftover from an earlier `code` command run from the wrong directory. Verified it was byte-identical to the real file via `Compare-Object` before deleting.

**What I did not understand at first:**
- That a step in a CI workflow (`dotnet format --verify-no-changes --verbosity diagnostic || true`) with `|| true` appended means the step always "succeeds" regardless of the actual command's exit code — so the whitespace errors were never actually what was blocking CI, even though fixing them was still good practice.
- That clearing `node_modules`/`package-lock.json` is a real, documented fix for npm's optional-dependency bug (npm/cli#4828) — but only when the underlying package version actually has bindings for the target platform/Node version. It's not a universal fix; you still have to verify the environment matches.
- That accepting a plausible-sounding analysis at face value (even when parts of it are correct) is risky — the whitespace diagnosis was accurate, but the "clear and reinstall" fix for oxlint was incomplete on its own. Verifying each claim against actual CI logs, before and after each change, caught this.
- That stray duplicate files can silently accumulate from directory-navigation mistakes during rapid terminal work, and are worth checking for with `git status` periodically, not just when something breaks.

**Status:**
RESOLVED — CI passes (green) for the first time since it was set up. First PR merged successfully via GitHub's UI with branch protection enforced (require PR before merge; approvals not required, given solo/limited-availability collaborator setup with Eric).

**Source verification:**
- npm/cli issue #4828 (referenced directly in the oxlint error message) — confirms this is a known, documented npm bug, not something unique to this project
- Direct comparison of local `node --version` (v22.15.0) against CI runner's reported `Node.js v18.20.8` in the failure logs — confirmed via primary evidence (actual log output), not assumption

---

**Last updated:** 2026-09-13
**Status:** Milestone 2 complete. 5 integration tests passing (5/5). 4 production bugs fixed. Ready for Milestone 3.
**Next:** Start Milestone 3 (Authentication, Authorization, Audit Logging)

---

# Milestone 4: Web Application

## Decision 19: Phase 1 Auth — Setup and Blockers Encountered

**Context:**
Started M4 (Frontend) with Phase 1: Auth setup. Goal was LoginPage, RegisterPage, AuthContext, routing — all wired and functional. Quickly hit multiple configuration and dependency issues that consumed ~2 hours of troubleshooting.

**What I asked the AI:**
Build Phase 1 auth from scratch — types, AuthContext (login/register/logout/refresh), LoginPage, RegisterPage, React Router setup, basic CSS styling. Copy-paste code, move fast.

**What it gave me:**
1. TypeScript types (User, AuthResponse, Project)
2. AuthContext with full auth flow (login, register, logout, silent refresh on app load)
3. LoginPage and RegisterPage forms with validation
4. App.tsx with Router and protected routes
5. Global CSS (forms, buttons, cards, utilities)

**What I changed and why:**
1. Attempted Tailwind CSS setup — hit PostCSS configuration issues immediately (Tailwind v4 doesn't ship CLI by default, PostCSS config errors). Abandoned Tailwind, reverted to plain CSS instead. Lesson: Tailwind adds complexity we don't need for MVP; plain CSS is faster.
2. Fixed import paths: `import { User, AuthResponse } from '../types'` → `import type { User, AuthResponse } from '../types/index'` (TypeScript verbatimModuleSyntax requirement + explicit index path).
3. Downgraded react-router-dom from v7.18.4 to v6 because v7 had incompatibility with React 19 hooks (BrowserRouter was throwing "Invalid hook call" errors).
4. Fixed Router/AuthProvider nesting: Initially wrapped Router inside AuthProvider (wrong), causing hook dispatcher to be null. Corrected to AuthProvider wrapping Router (hooks must be called in valid React component context).
5. Removed useAuth.ts hook initially, then recreated it when imports failed.
6. Deleted postcss.config.js and tailwind.config.js after abandoning Tailwind.

**What I did not understand at first:**
- That Tailwind v4 changed its distribution model away from CLI — wasted 30min trying to configure something that doesn't work the way it used to. Should have checked Tailwind v4 docs before attempting setup.
- That TypeScript's `verbatimModuleSyntax` flag requires explicit `import type` for interfaces, not just `import`. This is a strict mode that prevents runtime-only imports.
- That react-router-dom v7 has compatibility issues with React 19 in some configurations — v6 is the stable fallback.
- That component composition order matters with React hooks: hooks in child components can't execute before the parent's hook dispatcher is initialized. AuthProvider must wrap Router, not vice versa.

**Status:**
RESOLVED — Phase 1 auth fully functional. LoginPage renders, RegisterPage renders, routing works, protected routes redirect to /login. Frontend is ready for Phase 2 (Projects list/detail).

**Testing:**
Manual testing in browser: both login and register pages load and render correctly. Forms are interactive (not yet wired to API, but structure is correct).

**Commit:** 955d955 (feat/phase-1-auth PR merged to main)

---

### Decision 20: Phase 2 Projects — ProjectListPage and useApi Hook

**Context:**
Built ProjectListPage to list all projects from API. Created useApi hook as centralized fetch wrapper handling Bearer token auth, credentials, and error handling.

**What I asked:**
Build useApi hook (token auth wrapper) and ProjectListPage (fetches projects, grid layout, click to detail page).

**What it gave me:**
useApi hook with apiCall function, ProjectListPage with loading/error/empty states, grid layout, navigation.

**What I changed:**
Implemented as provided. Wired directly to API at http://localhost:5000.

**What I did not understand at first:**
None — this phase was straightforward.

**Status:**
RESOLVED — ProjectListPage fully functional, fetches from API, routes to detail page.

**Commit:** f161657

---

### Decision 21: Phase 3 Project Detail — ProjectDetailPage with Tab Navigation

**Context:**
Built ProjectDetailPage with tab navigation (TestCases, TestRuns, Defects, Dashboard). Created TabNav component and 4 tab skeleton components.

**What I asked:**
Build ProjectDetailPage, TabNav component, and 4 placeholder tab components. Wire detail page to API.

**What it gave me:**
ProjectDetailPage fetching single project from API, TabNav with active tab styling, 4 tab skeletons ready for content, back button to projects list.

**What I changed:**
Implemented as provided.

**What I did not understand at first:**
None — tab pattern is standard.

**Status:**
RESOLVED — ProjectDetailPage fully functional, fetches from API, tabs switch on click. Ready for Phase 4 (fill in tab content).

**Commit:** 0bdb39c

---

### Decision 22: Phase 4 Forms — TestCaseEditor Component Template

**Context:**
Started Phase 4 (Forms). Built TestCaseEditor as template component showing form pattern for all remaining forms (TestRunExecutor, CreateTestRunModal).

**What I asked:**
Build TestCaseEditor form component (create/edit test cases, fetch if caseId exists, POST/PATCH).

**What it gave me:**
Complete form with fields (title, preconditions, steps, expectedResult, priority), error/loading states, save/cancel buttons.

**What I changed:**
None — implemented as provided.

**Status:**
IN PROGRESS — TestCaseEditor built. Remaining: fill tab content (TestCasesTab with editor + list), TestRunExecutor, CreateTestRunModal, E2E tests.

**Commit:** d1cbcf8

---

### Decision 23: Phase 4 Forms Complete — TestCasesTab and TestCaseEditor

**Context:**
Completed Phase 4 forms. Built full TestCasesTab with list, add/edit/delete buttons. TestCaseEditor form component handles both create and edit flows.

**What I asked:**
Implement TestCasesTab (list + CRUD buttons), fill remaining placeholder tabs, fix TypeScript errors.

**What it gave me:**
TestCasesTab with useEffect fetch, TestCaseEditor integration, table display with actions, error/loading states.

**What I changed:**
Fixed TypeScript errors: type-only imports for Project interface, removed unused React imports, prefixed unused params with underscore.

**Status:**
RESOLVED — Phase 4 forms complete. TestCasesTab fully functional (pending API connection). Remaining tabs (TestRunsTab, DefectsTab, DashboardTab) filled with placeholders ready for content.

**Commit:** be6bc5e

---

### Decision 24: Phase 5 E2E Tests — Playwright Test Suite

**Context:**
Completed Phase 5: Built 4+ Playwright E2E test suites covering core user flows (auth, projects, test cases, defects).

**What I asked:**
Write 6+ E2E tests (Playwright) for: register, login, create project, create test case, execute run, create defect.

**What it gave me:**
4 test suites with complete flows:
- auth.spec.ts: Register and login
- projects.spec.ts: Create and list projects
- testcases.spec.ts: Create test cases
- defects.spec.ts: Create defects
- playwright.config.ts: Full Playwright configuration

**What I changed:**
Fixed linting errors (unused params), adjusted test selectors for actual UI.

**Status:**
RESOLVED — E2E tests written and ready. Tests require API running (Docker/PostgreSQL setup needed).

**Commit:** 262964e

**Next steps (post-deadline):**
1. Fix Docker/PostgreSQL infrastructure
2. Run E2E tests: `npm run test`
3. Fix remaining linting warnings (exhaustive-deps)
4. Deploy to cloud (M5)

---

**Last updated:** 2026-09-19
**Status:** M4 Phase 3 complete. Phase 1 (Auth), Phase 2 (Projects list), Phase 3 (Project detail with tabs) built and merged. Phases 4-5 in progress.
**Next:** Phase 4 (Forms: TestCaseEditor, TestRunExecutor, CreateTestRunModal).

---

# Milestone 5: Deployment & Documentation

## Overview
M5 covers deployment infrastructure, README documentation, OpenAPI specs, and CI/CD pipeline tuning.

---

### Decision 25: M5 Deployment — Render PostgreSQL + EF Core Migration Blockers

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

### Decision 26: EF Core Migration Blocker — RESOLVED

**Date:** September 26, 2026

**Context:** Decision 26 identified EF Core 10.0.12 vs 10.0.1 tools mismatch as the blocker preventing Render deployment.

**What I asked the AI:** "How do we fix the EF Core version mismatch and apply migrations to Render?"

**What it gave me:** Update the global dotnet-ef tool, create a new migration, apply it with the Render connection string.

**What I changed and why:** Followed exactly. The fix was simple once we understood the root cause.

**What I did not understand at first:** That updating one tool (dotnet-ef) would resolve the entire validation chain. I initially thought the problem was in the code or the database, but it was just the tooling version.

**Result:** RESOLVED. All three migrations (InitialCreate, AddAuthenticationTables, RenderDeployment) successfully applied to Render PostgreSQL. Cloud deployment path is now clear.

**Commit:** deb1b29

---

### Decision 27: Health Check Endpoint — Deployment Infrastructure

**Date:** September 26, 2026

**Context:** Deployment platforms need a way to monitor if the application is alive and the database is accessible. Without a health check, deployment platforms can't properly manage the app or trigger restarts when something fails.

**What I asked the AI:** "Build a health check endpoint that tests if the API and database are working. It should return HTTP 200 if healthy, 503 if not."

**What it gave me:** Simple GET endpoint at `/api/v1/health` that executes `SELECT 1` on the database and returns JSON with status, timestamp, and database connection state.

**What I changed and why:** Kept it exactly as suggested. The simplicity is the strength — it does one thing well and doesn't need embellishment.

**What I did not understand at first:** That this endpoint becomes the heartbeat of the deployed system. Every deployment platform (Render, Fly.io, etc.) uses health checks to know if your app is alive. Without it, monitoring and auto-restart capabilities don't work.

**Result:** Health check endpoint live and working. Returns 200 with `{"status":"healthy","timestamp":"...","database":"connected"}` when both API and database are reachable. Returns 503 with database disconnected if there's a problem.

**Commit:** 26347b5

---

### Decision 28: Implement Rate Limiting for API Protection

**Date:** September 27, 2026

**Context:** API needed protection against brute force and DoS attacks.

**What I asked the AI:** "How should I rate limit API requests?"

**What it gave me:** Fixed-window rate limiting configuration using .NET 10 built-in middleware. 100 requests per minute per IP.

**What I changed and why:** Used .NET 10's built-in rate limiting middleware. Applied globally to all endpoints (can be disabled selectively with [DisableRateLimiting] on specific endpoints if needed). Chose fixed-window over sliding-window for simplicity.

**What I did not understand at first:** Rate limiting should ideally be by user ID for authenticated endpoints vs by IP for public endpoints. Simple approach: global by IP. This is sufficient for MVP and protects against brute force login attempts.

**Result:** Rate limiting active. 100 requests per minute, 2 queued requests. Protects against basic DoS and brute force attacks.

**Commit:** b3d5fb6

---

### Decision 29: Committed Database Credential, Rotated, Removed and Scrubbed from History

**Date:** October 14, 2026 (credential revoked October 7)

**Context:** CODE_REVIEW.md section 0 found the live Render PostgreSQL connection string, password included, in api/appsettings.json. I committed it on 24 September 2026 (commit 25a7143) and it was still on main in a public repository. The review also found .env tracked in git even though it is in .gitignore, and that pressing F5 started the API in Development and connected it to the live database.

**What I asked the AI:** What to do about the leaked credential, in what order, and how to keep connection strings out of the repo while still being able to run locally. Later I asked whether a revoked password left in git history actually matters, and how to remove it if it does.

**What it gave me:** An ordered plan: rotate the credential before touching any code, remove the string from appsettings.json, supply it through dotnet user-secrets locally and environment variables elsewhere, make the app fail fast when it is missing, fix the compose file, and untrack .env. For the history, it first called the scrub required, then agreed when I pushed back that a revoked password in history is harmless, and that the scrub is mostly for the brief's "no secrets in the git history" line and for secret scanners. Eric said to do it, so I did.

**What I changed and why:**
1. Deleted the old Render database on 7 October, which killed the leaked password. On 14 October I created a new database (testify-db-2), saved its connection string in user secrets, and ran the API against it so the migrations applied.
2. Removed the ConnectionStrings block from appsettings.json.
3. Program.cs now throws a clear error at startup if ConnectionStrings:DefaultConnection is missing. I tested it by removing the secret and confirming the crash.
4. Local development reads the connection string and JWT secret from dotnet user-secrets.
5. docker-compose.yml now passes ConnectionStrings__DefaultConnection. The old DATABASE_URL was never read by any code.
6. Ran git rm --cached .env. .env.example stays as the template.
7. Scrubbed the git history, starting on 15 October and finishing on 16 October. I made a mirror backup of the repo first, then ran git filter-repo --replace-text on a fresh clone to swap the password for a placeholder. I checked the result, force-pushed the rewritten main, and deleted the 32 other remote branches (all already merged), because many of them still pointed at the old commits. Then I cloned the repo again into a clean folder and repeated the checks there.
8. How I checked it: searching the history for the old password (git log -S) found two commits before the scrub and none after. I saved the log of main before and after (author date, commit date, author name and subject). It was 126 commits both times with no differences, so only the commit hashes changed. The 4 backend tests pass on the fresh clone.

**How long it was exposed:** 13 days total, from 24 Sep 2026 until 7 Oct 2026.
The database was deleted and the password rotated immediately on 7 Oct when I became aware of the issue.
The configuration cleanup was merged on 14 October and the git history scrub was finished on 16 October.
In a production setting, rotation and remediation would have been handled immediately.
The password stopped working on 7 October, so the leftover string in git history was inert from then on. I removed it anyway because the brief says no secrets anywhere in the git history.

**How it got there:** Decision 25 records the connection string being configured in appsettings.json during the Render setup, and lists it under what the AI gave me. I accepted that and committed it forgetting my repository is public.

**What I did not understand at first:**
- Deleting the line from the file does not remove it from git history. Anyone can still read it from the old commit, so the real fix is making the password stop working. That is why rotation came before any code change.
- Why pressing F5 hit production. launchSettings.json sets the environment to Development, and Development fell through to the real connection string in appsettings.json.
- How configuration layers work. appsettings.json loads first, then user secrets (Development only), then environment variables, and later sources win. A double underscore in an environment variable name stands for the colon in a config key, so ConnectionStrings__DefaultConnection becomes ConnectionStrings:DefaultConnection.
- I had never used dotnet user-secrets. The value lives in my Windows user profile, outside the repo, so git never sees it.
- How serious a leaked password is once it has been revoked. After the database was deleted, the password in history opened nothing. The real mistake was leaving a live password in a public repo for 13 days. The scrub afterwards was tidying and checklist work, and I only understood that difference after asking.
- What a history rewrite changes. Every commit from the password commit onward gets a new hash, but author dates, commit dates, authors and messages stay the same. I did not take that on trust. I compared the log before and after.
- Why the scrub runs on a fresh clone and not in my working folder. git filter-repo even removes the origin remote by itself, so a rewrite cannot be pushed by accident. I also took a mirror backup first.
- Why rewriting main was not enough. The old remote branches still pointed at the old commits, so I had to delete them too.
- That branch protection did not stop me. GitHub printed "Bypassed rule violations", so as the repo owner I can override my own rule. It also showed that the rule was switched on.
- That GitHub keeps hidden references to old pull requests, and a force-push cannot remove them. Only GitHub Support can purge them. This is why revoking the credential was the real fix, and the scrub is cleanup.
- Why renaming my project folder failed. Something, most likely VS Code, had it open, so I cloned into a fresh folder (testify-app-clean) instead.

**Where the AI cost me time:**
- It assumed I deleted the old database on 14 October and gave me an exposure figure of 20 days. The real date was 7 October, so the correct figure is 13 days. I only caught it because I knew when I had done it.
- It called the history scrub required, then backed down when I asked whether it actually mattered. That cost a round of back and forth.

**Result:** RESOLVED. The credential has been dead since 7 October, the connection string now comes from configuration only, and the password is gone from the history of main on GitHub (checked with git log -S on a fresh clone). One limit: GitHub may still serve the old commits by their exact hash through pull request references until GitHub Support purges them. The old database hostname and username are still visible in old commits, but they point to a database that no longer exists.

**Source verification:** Microsoft Learn, "Safe storage of app secrets in development in ASP.NET Core": secrets are stored outside the project tree and only apply in Development. Microsoft Learn, "Configuration in ASP.NET Core": a double underscore is the nested key separator for environment variables. GitHub Docs, "Removing sensitive data from a repository": a pushed credential should be treated as compromised and revoked first, and old pull request references need GitHub Support to purge. git-filter-repo manual: it preserves commit dates and authors, and removes the origin remote on purpose.

---

### Decision 30: CI That Can Fail, and the Frontend That Did Not Compile

**Date:** October 17, 2026

**Context:** CODE_REVIEW.md 1.1 and 4.1. The frontend had 9 TypeScript errors, so `npm run build` failed and the frontend image could not be built. Pull requests had still been merged because CI only ran lint. CI also had a format check ending in `|| true`, and it deleted package-lock.json on every run.

**What I asked the AI:** How to reproduce the compile errors, fix them one at a time, and then change CI so this cannot happen again.

**What it gave me:** Small fixes for the three stub tabs and an unused import, and a new CI workflow with two jobs, `api` and `frontend`.

**What I changed and why:**
1. Ran `npx tsc -b` first and got 9 errors. One of them was an unused React import in TabNav, which the review did not list.
2. Fixed the three stub tabs (they all exported `DashboardTab`) and the unused import (PR #35). `npx tsc -b`, `npm run build` and `npm run lint` pass.
3. Rewrote ci.yml (PR #36): `dotnet format --verify-no-changes` without `|| true`, `dotnet test api.tests`, `npm ci` instead of deleting the lockfile, and `npm run build` (tsc, then vite build). I checked locally first that format, tests and build pass, so CI would not go red on something I did not cause.
4. Turned on "Require status checks to pass" and "Require branches to be up to date" for main, with `api` and `frontend` as the required checks. Before this the rule required no checks, so the pipeline was not a gate at all.
5. Proved the gate can fail (PR #37, closed without merging). A deliberate type error made `frontend` fail with TS2322, `api` stayed green, and the merge button was blocked.

**What I did not understand at first:**
- Lint does not type-check. oxlint passed on a frontend that did not compile, which is how the broken code reached main.
- `|| true` makes a step always succeed, so a check ending in it cannot fail.
- `npm ci` installs exactly what the lockfile pins and fails if the lockfile and package.json disagree. `npm install` quietly re-resolves versions, so CI was not testing what I committed.
- A destructured parameter like `{ _projectId }` is not the same as a parameter named `_props`. TypeScript ignores unused parameters that start with an underscore, but my code asked for a property called `_projectId` that the type did not have.
- Branch protection matches checks by job name, and GitHub only offers a check name after it has run once. That is why I turned the rule on after the first run on the PR.
- .NET file methods in PowerShell resolve relative paths against the folder the terminal started in, not the folder I had moved to, so my first script failed until I built the path from `$PWD`.

**Where the AI cost me time:** The first script it gave me for the TabNav fix assumed I was in the repo root, so it failed with a path error after I had moved into the frontend folder.

**Result:** RESOLVED. The frontend compiles, CI runs tests, type-check and build, and a red run blocks the merge. Not covered yet: building the Docker images in CI, end-to-end tests in CI, deploy on merge and dependency scanning.

**Source verification:** GitHub Docs on protected branches and required status checks (checks are matched by name, and only checks that ran recently can be selected). npm docs for `npm ci`. PR #37 as the evidence that the gate fails.

---

### Decision 31: Frontend Reads the API Address From the Environment

**Date:** October 17, 2026

**Context:** CODE_REVIEW.md 1.5. Six hardcoded `http://localhost:5000` URLs in AuthContext.tsx and useApi.ts meant a deployed frontend would call the visitor's own machine. `VITE_API_URL` was set in docker-compose.yml and the frontend Dockerfile but nothing read it, which broke the brief's rule of configuration through environment variables only.

**What I asked the AI:** How to make the frontend read the API address from the environment, and how Vite handles environment variables.

**What it gave me:** A small `config.ts` that reads `VITE_API_URL`, replacements for the six URLs, a `.env.development` for local use and a one-line compose fix.

**What I changed and why:**
1. Reproduced first: a search found the six URLs and no use of `import.meta.env`.
2. New `frontend/src/config.ts` exports `API_BASE_URL` and throws a clear error at startup if `VITE_API_URL` is missing. It is the same fail-fast idea as the backend connection string: a missing setting should be loud, not a silent call to localhost.
3. The six calls now use `API_BASE_URL`.
4. `frontend/.env.development` sets `http://localhost:5137`, the port `dotnet run` uses. Before, the code pointed at 5000, which nothing listens on outside Docker. It holds no secret, so it is committed.
5. docker-compose.yml passes `http://localhost:5000` without `/api/v1`, because the code already adds that path.
6. Checked in the browser: the first request goes to `http://localhost:5137/api/v1/auth/refresh`. Removing `.env.development` gives the "VITE_API_URL is not set" error.

**What I did not understand at first:**
- Vite bakes `VITE_` variables into the bundle when it builds, so a production build needs the variable at build time. Changing it afterwards does nothing.
- Variables already in the environment, like the one compose sets, take priority over `.env` files.
- Two different ports are both correct: 5137 when I run `dotnet run`, and 5000 when the API runs in Docker Compose, because the container's port is published on my PC.
- `.env.development` is only loaded in dev mode, so CI's `npm run build` still works without the variable. The check happens when the app runs in a browser.

**Result:** RESOLVED. Local development now reaches the API. Compose still cannot run end to end (review 1.4): the API container listens on 8080 but compose maps 5000, and CORS only allows port 5173.

**Source verification:** Vite docs, "Env Variables and Modes": only `VITE_` variables are exposed, they are replaced at build time, and existing environment variables take priority over `.env` files.

---

### Decision 32: CORS Origins Come From Configuration, Tests First

**Date:** October 18, 2026

**Context:** CODE_REVIEW.md 1.4 (point 3). Program.cs allowed only http://localhost:5173, but Compose serves the frontend on port 3000 and a deployed frontend has its own address. The brief asks for CORS locked to a known origin, never `*`, and for configuration through environment variables.

**What I asked the AI:** How to read the allowed origins from configuration, keep `*` out, and test that the right origin is allowed and any other is not.

**What it gave me:** A comma separated `CORS_ALLOWED_ORIGINS` setting read in Program.cs, a startup error when it is missing or contains `*`, a development default in appsettings.Development.json, and two tests.

**What I changed and why:**
1. Wrote the tests first and ran them: the configured-origin test failed and the other-origin test passed, as predicted.
2. Moved the test setup into one shared `TestifyApiFactory`.
3. Made the change, then proved the startup failures by running with `ASPNETCORE_ENVIRONMENT=Production` and `--no-launch-profile`: a missing value, `*`, and a valid origin that then stopped at the next missing setting.
4. Compose passes `http://localhost:3000`.

**What I did not understand at first:**
- Program.cs reads configuration while it is still registering services, which happens before the test factory's `ConfigureAppConfiguration` values arrive. Settings that Program.cs needs that early must go in with `UseSetting`. `JWT_SECRET` works with `ConfigureAppConfiguration` because JwtService reads it later. It is the same family of problem as Decisions 7 and 16.
- A browser sends a preflight OPTIONS request before a cross-origin call, and the `Access-Control-Allow-Origin` response header decides whether the call goes ahead.
- Browsers refuse `*` together with credentials, so the cookie based sign in needs explicit origins anyway.

**Result:** RESOLVED. `AllowAnyMethod` and `AllowAnyHeader` are still open (review 2.7).

**Source verification:** dotnet/aspnetcore issue 37680 and the ASP.NET Core integration testing documentation on how factory configuration is applied.

---

### Decision 33: Docker Desktop Would Not Start, So Compose Is Parked

**Date:** October 19, 2026

**Context:** CODE_REVIEW.md 1.4 needs `docker compose up` to be shown working. Docker Desktop on my laptop, which had worked before, stopped starting with "Error: backend exited before becoming ready".

**What I asked the AI:** How to diagnose it, and whether to keep going.

**What it gave me:** A series of checks (WSL version and distributions, the Windows features it needs, leftover processes, the backend log), a reinstall over the old version, and a time box. The backend log showed a normal start and then simply stopped, with no error line.

**What I changed and why:** I stopped after spending too long on it. The planned compose edits (API build context, port mapping, Node 22 in the frontend image) are parked with nothing lost. I told Eric what I had tried and that I would prove compose in CI instead: a job that builds both images, starts the stack and checks the health endpoint and the CORS preflight. He had not replied when I moved on.

**What I did not understand at first:** `docker --version` working only means the command line client is installed. The engine lives inside Docker Desktop, so `docker info` fails when Docker Desktop is not running. On Windows it also depends on WSL 2 and its own `docker-desktop` distribution.

**Where the AI cost me time:** The plan to verify compose assumed Docker would run on my machine. When it did not, the diagnosis took far longer than it should have. I should have time-boxed it harder and switched to CI sooner.

**Result:** PARKED. Compose and the production frontend image are still to do (review 1.4 and 4.4).

---

### Decision 34: One Helper Decides Who Can Do What

**Date:** October 22, 2026

**Context:** CODE_REVIEW.md 2.1, 2.3, 2.4 and 2.6. Anyone, signed in or not, could read every project. Every "not allowed" answer crashed with a 500 because `Forbid()` needs an authentication scheme the app never registers. A project owner could not write to their own project. The only test asserted the leak as correct behaviour.

**What I asked the AI:** How to make every project endpoint check who is asking, what to do about `Forbid()`, and how to prove it with tests.

**What it gave me:** A `ProjectAccess.GetRoleAsync` helper, two controller helpers (`RequireProjectAccessAsync` and `ForbiddenResponse`), scripted edits for every controller, and 22 authorization tests.

**What I changed and why:**
1. Wrote the tests first: 16 of 27 failed before the fix (including the owner who could not write, and the crashes) and all 27 passed after.
2. Decided that the owner is whoever `Project.OwnerId` says, and the ProjectMembers table only holds invited people (editor or viewer). One source of truth, no second row to keep in sync and no backfill of old projects. DATABASE_SCHEMA.md says the owner is also a member, which is why the planning documents now carry a banner.
3. Status codes: 401 without a token, 404 for an unknown project, 403 for a known user without enough access.
4. Reads need viewer, changes to test cases, runs, results and defects need editor, project update and delete and invitations need owner. Viewers became read only on defects too.
5. Replaced `Forbid()` with `ForbiddenResponse`, which returns the standard error shape. I kept the custom authentication middleware, because switching to the JwtBearer package would change how claims are mapped.
6. Project existence checks use `AnyAsync` instead of loading the project, which also stopped the object cycle 500 on creating test cases, runs and defects.

**What I did not understand at first:**
- Why write a test that fails first. It proves the test can detect the bug, so a pass afterwards means something. The old test `GetProjects_ReturnsOk` could never fail, so it proved nothing.
- Why `Forbid()` crashed: it asks the authentication system to write the response, and the app registers no scheme because the custom middleware parses the token itself.
- In the test client an unhandled server exception is thrown as an exception instead of showing up as a 500.

**Result:** RESOLVED for project data. The audit log, tokens and input validation follow in Decisions 35 to 37.

**Source verification:** OWASP Top 10, A01 Broken Access Control.

---

### Decision 35: Audit Log Scoped To The User, Three Leaks Closed

**Date:** October 25, 2026

**Context:** CODE_REVIEW.md 2.2 and 2.7. Any signed in user could read every user's audit entries, including emails and IP addresses. `User.PasswordHash` had no `[JsonIgnore]`. The health endpoint returned raw exception text. A rate limit attribute pointed at a policy that does not exist.

**What I asked the AI:** How to restrict the audit log without adding an admin role, and how to close the smaller leaks.

**What it gave me:** A rule that a user reads entries for their own actions and for projects they own, `[JsonIgnore]` on the hash, a health endpoint that logs the exception and returns only the status, and removal of the dead attribute.

**What I changed and why:**
1. Tests first: 7 new, 4 failed before the fix (34 in total), all passed after. Three of the new tests passed before and after on purpose, to prove the fix does not lock out legitimate access (a user's own entries, an owner seeing an editor's actions).
2. In the test environment the health check always fails, because the in-memory database cannot run raw SQL, which made it possible to test the error path.

**What I did not understand at first:**
- The code comment in AdminController was honest that this was unfinished, while SECURITY.md claimed "owner only". The document was the problem, not the code.
- Exception text can reveal host and database names, so it goes in the server log and never in the response.
- `[JsonIgnore]` protects the hash even if some endpoint returns a `User` by mistake.

**Result:** RESOLVED. Rate limiting on the auth endpoints is still to do (2.7).

**Source verification:** OWASP Top 10, A01 and A09.

---

### Decision 36: Tokens Stored As Hashes, Refresh Tokens Rotate

**Date:** October 26, 2026

**Context:** CODE_REVIEW.md 2.5 and 3.4. Refresh and password reset tokens were stored in plain text, refresh tokens never changed, and `/auth/refresh` expected the token in the request body, which JavaScript cannot supply from an httpOnly cookie. So the silent refresh could never work and every reload signed you out.

**What I asked the AI:** How to store tokens safely, rotate refresh tokens, and make the endpoints read the cookie.

**What it gave me:** A `TokenHasher` (256 random bits, SHA-256 for storage), one helper that issues refresh tokens, a cookie limited to `/api/v1/auth`, a refresh endpoint that retires the used token and returns the user, a logout that revokes the token and clears the cookie, and 10 tests.

**What I changed and why:**
1. Tests first: 8 of the 10 new tests failed before the fix (44 in total), all passed after.
2. Kept the column name `Token` although it now holds the hash, to avoid a migration. Sessions and reset links from before the change stop working, so everyone signs in once more.
3. Removed the unused `RefreshRequest` and `LogoutRequest` classes.

**What I did not understand at first:**
- A fast hash is fine for these tokens because they are random and cannot be guessed. Passwords need bcrypt because people choose guessable ones.
- The browser sends an httpOnly cookie by itself when the request includes credentials. That is why the endpoint must read the cookie, not the body.
- The .NET test client does not send Secure cookies over http, so the tests switch cookie handling off and send the header by hand.
- Rotation can race with React StrictMode, which runs the page-load refresh twice in development. The auth flow PR deals with it.

**Result:** RESOLVED, with limits: a stolen token that is reused is rejected but does not end all sessions, and SameSite Strict needs the frontend and API on the same site once deployed.

**Source verification:** OWASP Top 10, A07, and the OWASP session management guidance on rotating tokens.

---

### Decision 37: Validate Status Values And Defect References

**Date:** October 27, 2026

**Context:** CODE_REVIEW.md 3.1, 3.2, 3.3 and 3.9. Any string was accepted for priority, severity and statuses. Runs started as "pending" and never set `completedAt`. Defects were created as "open" while the dashboard counted "Open", so open defects by severity was always empty. A defect could point at another project's test case. Adding cases to a run saved and then crashed with a 500.

**What I asked the AI:** How to validate the allowed values in one place, fix the status bugs, and make defect creation check what it references.

**What it gave me:** An `Allowed` class with the value lists, 400 responses with codes, the status fixes, a check that a defect comes from a failed result of a run in the same project for that result's test case, and plain-object responses for two endpoints.

**What I changed and why:**
1. Tests first: 12 new, all 12 failed before the fix (56 in total), all passed after. The defect tests seed their data straight into the database so they do not depend on the endpoint that crashed.
2. The defect check is also a tenant isolation fix, because without it a member of project B could attach a defect to project A.
3. Left a full DTO layer, status transition rules and `resolvedAt` for later.

**What I did not understand at first:**
- `"open"` and `"Open"` are two different values for the database and for C#, so the dashboard and the writer disagreed without any error.
- Returning an EF entity can serialise its related objects too, and two objects that point at each other make the serialiser loop.
- A test that seeds data directly is more precise than one that goes through three endpoints, because a failure points at the thing being tested.

**Result:** RESOLVED for the write path. Pagination bounds, one error shape and PUT versus PATCH are still to do (3.5, 3.7, 3.8).

**Source verification:** OWASP Top 10, A01 and A04.

---

### Decision 38: The Sign In Flow Works End To End In The Browser

**Date:** October 29, 2026

**Context:** CODE_REVIEW.md 3.4, 3.5 and 3.10. After the API fixes, the screens still did not work with them. Registering bounced you to the login page, an expired token threw "please log in again", editing a test case returned 405, the register form accepted passwords the server rejects, the links reloaded the page and wiped the in-memory token, and the page-load refresh ran twice.

**What I asked the AI:** For six small fixes, one at a time, each with a check I could run and a browser test I could see.

**What it gave me:**
1. Register signs in straight after the account is created.
2. `refreshAccessToken` in AuthContext, shared between callers, and a `useApi` that refreshes and retries once on a 401.
3. The test case editor sends PUT, and the README lists the real verbs.
4. The register form uses the same rules and messages as the server.
5. `<Link>` instead of `<a href>` on the login and register pages.
6. The page-load refresh goes through `refreshAccessToken`, so React StrictMode's second run shares the first request.

**What I changed and why:**
1. Kept the API as it was. Making register return a token would have created a second place that issues tokens, so the frontend calls login after a successful register instead.
2. `useApi` returns nothing for a 204 response. A successful delete has no body, and parsing it as JSON threw an error.
3. The password check on the form uses Unicode character classes (`\p{Lu}`, `\p{Nd}`), because the server uses `char.IsUpper` and `char.IsDigit`, which also accept non-ASCII characters. A plain `[A-Z]` would have disagreed with the server on some input.
4. Tested the 401 retry for real instead of trusting the code: shortened the token to 1 minute, waited, then went Back and Forward in the browser. Back and Forward move around the app without reloading the page, so the page keeps its old token. The Network tab showed `projects` 401, `refresh` 200, `projects` 200. Then I reverted the change with `git checkout` and confirmed the working tree was clean.
5. Tested the StrictMode fix by counting `refresh` requests in the Network tab after a reload: one, not two.
6. Fix 5 went in with leftover lines from the old link and failed the type check. I repaired it before pushing with `git commit --amend`, and re-ran the type check, lint, build and the browser test on the repaired code.

**What I did not understand at first:**
- The refresh cookie is single use. The server retires it the moment it is used, so two refresh calls at the same moment cannot both succeed. That is why concurrent requests must share one refresh instead of each starting their own.
- React StrictMode runs effects twice in development on purpose, to expose side effects that are not safe to repeat. It does not happen in a production build, but a rotating token makes the double run visible.
- A plain `<a href>` is a full page navigation. Anything held in memory is lost, including the access token, which is the reason the token lives in memory and not in localStorage. A router `Link` changes the view without a reload.
- PUT replaces the whole resource and PATCH changes part of it. The editor always sends every field, so PUT matches what it does, and the API already used PUT. The mismatch was between the frontend, the README and the API, and the API was the one that was right.
- A commit can succeed while the checks fail. Git does not run the type check or the build, so a clean `git commit` says nothing about whether the code compiles. Reading the output of each check before committing is part of the workflow, and `--amend` is only safe because nothing had been pushed yet.
- A 204 response has no body, so calling `response.json()` on it throws even though the request worked.

**Where the AI cost me time:**
- The instructions for fix 5 replaced one block but did not make clear that the old lines had to be removed, so two leftover lines broke both pages.
- The instructions for fix 6 said to delete a block and paste a new one, but not where to paste it. I now ask for whole files to paste over, so there is nothing to place.

**Result:** RESOLVED for the frontend sign in flow. Limits: the retry happens once per call, the browser checks were done by hand, and the Playwright tests have not been updated for the new register rules (`auth.spec.ts` still uses a password with no special character). That belongs to the testing PR.

**Source verification:** React documentation on StrictMode (effects run twice in development). React Router documentation for `Link`. OWASP session management guidance on rotating tokens. MDN on HTTP 204 (no content) responses.

---

### Decision 39: Create Project, A Shared Look, And The Test Runs Flow

**Date:** October 30, 2026

**Context:** CODE_REVIEW.md 1.2 and 1.3. The Create Project button went to `/projects/new`, which did not exist, so the app treated "new" as a project id and showed an error. The Test Runs tab was a stub that said "coming soon", which left the core feature of the brief (run a set of test cases and see a live pass rate) with no screen at all.

**What I asked the AI:** To agree the look of the new screens from a mockup before any code, then build Create Project, then the Test Runs list, then the page where a run is executed. The pass rate on the run list needed counts the API did not send yet, so I asked for that to be built tests first.

**What it gave me:**
1. A mockup of the Projects page and the Create Project form that I approved before any code was written.
2. A shared header, restyled project cards and a `CreateProjectPage` with the same validation and messages as the server.
3. `RunStats.PassRate` and run summaries (result counts and pass rate) on the run list and run detail endpoints, with 10 new tests.
4. A Test Runs tab: list of runs with a pass rate bar, and a panel to pick test cases for a new run.
5. A run page: mark each result Pass, Fail, Blocked or Not run, add notes, see the pass rate move, complete or reopen the run.

**What I changed and why:**
1. Kept plain CSS and added no libraries, because Decision 19 showed Tailwind cost time for no gain.
2. The counts are worked out by the database in one query for the whole list, not by one request per run. The pass rate is calculated in one place (`RunStats`) so every screen shows the same number.
3. Wrote the tests first and watched them fail twice: first the build failed because `RunStats` did not exist, then the 4 run summary tests failed because the API did not send counts. After the change all 66 tests passed (56 before, 10 new).
4. Creating a run is two calls: create the run, then add the cases. If adding the cases fails, the screen deletes the empty run so nothing half-made is left behind.
5. A completed run is locked. To change a result you reopen it first.
6. The run page links back to `?tab=runs`, so Back lands on the Test Runs tab and not the Test Cases tab.

**What I did not understand at first:**
- .NET `Math.Round` rounds halves to the nearest even number by default, so 12.5 would become 12. JavaScript rounds 12.5 up to 13. To make the server and the screen agree, the server uses `MidpointRounding.AwayFromZero`, which rounds halves up like JavaScript does.
- Counting results per run does not need one request per run. EF Core turned the counts into one SELECT with a subquery for each count, which I could see in the API log.
- Windows keeps `Testify.Api.exe` locked after a run that was not stopped properly, so `dotnet run` could not rebuild and failed with MSB3026. "Access is denied" meant I could not kill it either. `dotnet run --no-build` ran the build that already existed, and a restart cleared the lock. The habit is to press Ctrl+C in the API terminal before closing it.
- Values set in a terminal override user secrets, because configuration layers load in order and later sources win. That is why pasting database settings by hand pointed the API at a database that did not exist.
- EF Core warns when paging (`Skip` and `Take`) is used without an `OrderBy`, because the database does not promise any order then. The test cases list still has this problem and needs a sort.

**Where the AI cost me time:**
- A second AI told me to set a database password and JWT secret by hand and start Postgres in Docker. That was wrong for this setup: Docker Desktop does not run on my laptop and my real values were already in user secrets. It cost a round of confusion before I went back to the original setup.
- The run page and tab use the same data loading pattern as the older tabs, which adds lint warnings (6 before this PR, 9 now). They are warnings, not errors, but they need a proper fix.

**Result:** RESOLVED for Create Project and Test Runs. Limits: executing a run is only tested by hand in the browser so far, there is no Playwright test for it yet (testing PR), the test cases list still has no sort order, and Defects and the Dashboard are still stubs.

**Source verification:** Microsoft Learn on `Math.Round` and `MidpointRounding` (default is to even). MDN on `Math.round` (halves round up). Microsoft Learn on configuration providers in ASP.NET Core (later sources override earlier ones). EF Core documentation warning about `Skip`/`Take` without `OrderBy`.

---

### Decision 40: Defects Are Raised From A Failed Result And Managed In Their Own Tab

**Date:** October 30, 2026

**Context:** CODE_REVIEW.md 1.2. The Defects tab was a stub that said "coming soon". The brief says a defect is raised from a failed test case inside a run, links back to that case and run, and has a severity and a status (Open, In Progress, Resolved, Closed). The API for all of that already existed and was tested (Decisions 34 and 37), but nothing in the app could use it.

**What I asked the AI:** For a Defects feature in the same look as the rest of the app: a way to raise a defect from a failed result on the run page, a Defects tab to list defects and change their status, and any API change needed first, built tests first.

**What it gave me:**
1. A one line API change: the defects list is now ordered newest first, with a test written before the change.
2. A Defects tab with a severity badge (Critical, High, Medium, Low), the test case each defect came from, the date, a status dropdown that saves straight away, and a filter by status.
3. A Raise defect button on every failed row of the run page. It opens a form with the title filled from the test case, the description filled from the note I wrote on the result, and a severity choice.
4. Once raised, the failed row shows a "Defect raised" link to the Defects tab, so the same result cannot be raised twice by accident.

**What I changed and why:**
1. Wrote the list order test first and ran it against the unchanged controller, so I saw it fail before the one line change made it pass (67 tests in total).
2. The form checks the title is not blank with the same message as the server ("Title is required"). Severity is a dropdown of the four allowed values, so it cannot be wrong.
3. The Defects tab looks up test case titles from the test cases list, because the defects endpoint only returns the id of the case. That is one extra request, and it keeps the API unchanged.
4. Kept the colours from the existing palette and added no libraries.

**What I did not understand at first:**
- A defect points at two things: the test case, and the specific result of that case in one run. The result is what proves the case failed, so the API checks that it belongs to this project and really is Fail. The test case id alone would not be enough.
- Without an `OrderBy`, `Skip` and `Take` give no promised order, so "newest first" is not something the database does for free. The API log had been printing a warning about this on the test cases list too.
- The run page cannot ask the API "does this result already have a defect?", because there is no such filter. It loads the project's defects and matches them by result id in the browser. That works for a normal project, but it only looks at the first 200 defects, so a very large project would miss some links. A filter on the API is the proper fix.
- `PUT` on a defect only changes the status. The title, description and severity cannot be edited after raising, which is a gap rather than a decision.
- The defect table has a `ResolvedAt` column, but nothing sets it when a defect moves to Resolved. The brief does not ask for it, but a real tracker would want it.

**Where the AI cost me time:** Nothing significant on this change. The browser checks all passed.

**Result:** RESOLVED for raising and managing defects. Limits: defects cannot be edited beyond their status, the "already raised" lookup only covers the first 200 defects, a defect link only shows while the result is still Fail, `ResolvedAt` is never set, and there is no Playwright test yet (testing PR).

**Source verification:** EF Core documentation on ordering with `Skip` and `Take` (the paging warning). MDN on the HTML `select` element, and WAI-ARIA guidance on labelling form controls.

---

### Decision 41: The Dashboard Uses One Pass Rate Rule And The Latest Run

**Date:** October 31, 2026

**Context:** CODE_REVIEW.md 1.2. The Dashboard tab was the last stub in the app. The brief asks for four numbers per project: total cases, runs completed, current pass rate and open defects by severity. The existing dashboard endpoint had no "runs completed" number, calculated its own pass rate, and did its own access check.

**What I asked the AI:** To read the existing dashboard controller before designing the screen, say what was missing or wrong, fix the API tests first, and then build the tab in the same look as the rest of the app.

**What it gave me:**
1. Three gaps found by reading the controller: no completed runs count, a pass rate that rounds differently from the run list, and "current pass rate" not being defined anywhere.
2. 5 new API tests (72 in total), written before the change.
3. A rewritten `DashboardController`: it counts completed runs, returns the latest run with its own counts and pass rate, and uses `RunStats.PassRate` for every percentage.
4. A Dashboard tab with four stat cards, a Latest run panel with the pass rate bar and result chips, open defects by severity as coloured bars, and the five most recent defects.

**What I changed and why:**
1. Defined "current pass rate" as the pass rate of the latest run, because that is the first thing a tester asks. The pass rate across all runs is still returned and shown as a smaller line under the panel.
2. Replaced the controller's own membership check with the shared `RequireProjectAccessAsync` helper, so 401, 404 and 403 behave the same as every other endpoint. This also removes the inconsistency from review 2.4, where the dashboard was the only place that checked ownership differently.
3. The open defects query now joins through the test case in the database. Before, it loaded every test case id into a list and filtered with `Contains`.
4. Wrote the tests first and ran them against the old controller: 4 of the 5 failed. The fifth, open defects by severity, passed before and after on purpose, to prove the rewrite did not break something that already worked.
5. Left the chart as plain CSS bars. No chart library, so there is nothing new to keep up to date or scan for vulnerabilities.

**What I did not understand at first:**
- "Current pass rate" can mean at least two different things: the latest run, or every result across all runs. The brief does not say which. Naming the choice and showing the other number as a secondary line kept both visible and avoided silently picking one.
- The dashboard rounded with .NET's default, which sends exact halves to the nearest even number, so 12.5 shows as 12 here while the run list shows 13. It is the same bug as Decision 39 in a second place. The fix is not to copy the rounding line into a third place, but to route every percentage through one function.
- A filter on a related entity such as `d.TestCase.ProjectId` becomes a SQL join, so the database does the filtering instead of the application loading ids first.
- The response contains a `latest_run` that is null for a project with no runs. The screen has to handle that case on purpose, or it would crash on a brand new project.

**Where the AI cost me time:** Nothing significant on this change. The browser checks all passed.

**Result:** RESOLVED. Every screen in the brief now exists. Limits: the dashboard has no trend over time (a stretch goal in the brief), `defects_by_status` is returned but not shown, recent defects are not links, and there is no Playwright test yet (testing PR).

**Source verification:** Microsoft Learn on `Math.Round` and `MidpointRounding` (the default sends halves to even). EF Core documentation on how navigation properties in a filter translate to joins. The challenge brief, section 2, for the four dashboard metrics.

---

### Decision 42: The README And SECURITY.md Are Rewritten From The Running App

**Date:** October 31, 2026

**Context:** CODE_REVIEW.md section 5 and the brief's definition of done, which asks for a README with what it is, an architecture diagram, how to run it, how to deploy it, and known limitations. The README written on October 27 said the web app was incomplete and that the Test Runs, Defects and Dashboard tabs did not work. Three pull requests later every one of those screens works, so the Known Limitations list had become false. SECURITY.md also still said the register form did not check the server's password rules, which stopped being true in Decision 38.

**What I asked the AI:** To rewrite the README from the current code and the results I had actually seen, and to find any line in SECURITY.md that the last few pull requests had made untrue.

**What it gave me:**
1. A whole new README: a short description, a status line, what you can do, an architecture diagram, the tech stack, how to run it locally, a configuration table, how to run the tests and what CI does, the real endpoint list with the right verbs, a deployment section, a container section and an honest Known Limitations list.
2. A one line correction in SECURITY.md.

**What I changed and why:**
1. Kept every claim to something I had run or read. The test count (72) comes from the last `dotnet test`, the endpoint verbs come from the controllers, and the configuration names come from Program.cs and config.ts.
2. The deployment section says the app is not deployed. Only the database is on Render. I would rather the README say so than describe a deployment that does not exist.
3. Image sizes are marked as not recorded, not estimated. The brief asks for them, but Docker Desktop does not start on my laptop, so I cannot build the images yet.
4. The architecture diagram is a Mermaid block, so GitHub draws it from text and it cannot drift into a stale picture file.

**What I did not understand at first:**
- A Known Limitations list is only true on the day it is written. The October 27 version was honest when I wrote it and wrong within days, because features landed afterwards. The habit that fixes this is to update the README in the same pull request as the feature, not in a catch-up pull request like this one.
- GitHub draws a fenced block marked `mermaid` as a diagram, so a diagram can live in the README as plain text and be reviewed like code.
- The CI badge shows the latest run of the workflow on the default branch, not the status of an open pull request.

**Where the AI cost me time:** Nothing significant on this change. The earlier READMEs described the plan or a past state of the app, which is the failure the review pointed out in section 5, and this rewrite exists to fix that.

**Result:** RESOLVED for now. Limits: the README still has no recorded image sizes and no real deployment instructions, because there is no deployment. Both need real values once the images are built and the app is deployed.

**Source verification:** GitHub Docs on creating diagrams with Mermaid in Markdown. GitHub Docs on adding a workflow status badge. The challenge brief, section 7, for the README requirements.

---

### Decision 43: A Test Strategy And Browser Tests For The Critical Paths

**Date:** October 31, 2026

**Context:** CODE_REVIEW.md 4.2 and brief 3.7. Testing was the biggest gap and it was meant to be my strongest area. There was no written test strategy, the four old Playwright files were broken (a password with no special character, an old create project form, a stub Defects tab, a user and project that were never seeded), the config started only the web app with no API behind it, and there was no browser test for executing a run.

**What I asked the AI:** For a written test strategy, and a set of browser tests for the six paths the brief names (register, log in, create a project, create a case, execute a run, raise a defect) that do not depend on seeded data and do not touch the real database.

**What it gave me:**
1. `docs/TEST_STRATEGY.md`: the levels (static checks, unit, API integration, end to end), what each covers and why, how to run them, and an honest list of what is not covered.
2. A `RATE_LIMIT_PER_MINUTE` setting for the API, 100 when it is not set.
3. `helpers.ts` and `critical-paths.spec.ts`: 8 Playwright tests (the six paths, a wrong password, and the dashboard).
4. A rewritten `playwright.config.ts` (Chromium, no report opening in the browser) and the Playwright output folders added to `.gitignore`.

**What I changed and why:**
1. Deleted the four old spec files instead of patching them, because every one of them depended on something that no longer existed or never existed.
2. Every test makes its own user with a unique email. Setup (the project, the cases, the run, the results) is done through the API, and only the behaviour under test is done in the browser. That keeps tests independent, lets them run in parallel, and means a failure points at the thing under test.
3. Made the rate limit configurable instead of removing it. Eight tests make far more than 100 requests a minute from one address and would get 429 responses. The default stays 100, so normal behaviour is unchanged, and all 72 API tests still pass.
4. The browser tests run against a throwaway API in the Test environment with an in-memory database, using made-up secrets in a terminal I closed afterwards, so the Render database is never touched.
5. The 8 tests passed on the first run (10.3 seconds). Lint shows 0 errors and the build is clean.

**What I did not understand at first:**
- A browser test needs two things running, the web app and an API. The old config started only the web app, so the old suite had no backend to talk to even if its steps had been right.
- Setting `ASPNETCORE_ENVIRONMENT=Test` switches the API to the in-memory database, the same switch the xUnit tests use. `--no-launch-profile` is needed as well, because `launchSettings.json` sets Development and would override it, which is the same trap as review 0.3.
- A file opened with `code` does not exist on disk until it is saved. My first `git add` for the strategy failed with "pathspec did not match", and I committed it after the browser tests.
- Playwright finds elements best by role, label and text, and not by CSS class. The outcome buttons expose `aria-pressed`, which makes the page easier to test and easier to use with a screen reader at the same time.

**Where the AI cost me time:** Nothing significant. The tests passed on the first run.

**Result:** RESOLVED for the critical paths. Limits: the browser tests run locally and not in CI yet, only Chromium is used, there are no tests against a real PostgreSQL, rate limiting has no test of its own, editor and viewer limits are tested at the API level only, and the test files are not type checked by `tsc -b`, because it only covers `src` and `vite.config.ts`.

**Source verification:** Playwright documentation on locators, test isolation and parallelism. Microsoft Learn on ASP.NET Core environments and `launchSettings.json`. The challenge brief, section 3.7, for the testing requirements.

---

### Decision 44: The Browser Tests Run In CI And Block The Merge

**Date:** November 1, 2026

**Context:** CODE_REVIEW.md 4.2 and brief 3.7: "Tests run in CI and block the merge when they fail." Decision 43 gave me 8 Playwright tests, but they ran only on my laptop. A broken screen could still have merged, because CI covered the API tests, lint, the type check and the build, and nothing drove a real browser. The browser test files were also outside `tsc -b`, which only covered `src` and `vite.config.ts`.

**What I asked the AI:** For a CI job that runs the Playwright tests against a throwaway API with an in-memory database and made-up secrets, and for a way to type check the test files too. I asked for small steps, with the checks run before each commit.

**What it gave me:**
1. A third job, `e2e`, in `ci.yml`. It installs Node and .NET, installs Chromium, builds the API, and starts it in the Test environment in the background. It polls the health endpoint for up to 2 minutes, then runs `npx playwright test`. On failure it prints the API log and keeps the Playwright report for 7 days.
2. `frontend/tsconfig.e2e.json` and a third reference in `tsconfig.json`, so `tsc -b` (and so `npm run build` in CI) also checks `frontend/tests` and `playwright.config.ts`.

**What I changed and why:**
1. Kept the job as provided, and checked it against the Test environment setup I already used locally (Decision 43): the same environment name, the same CORS origin and the same raised rate limit.
2. The secrets in the job are made up for that job only. The API uses an in-memory database there, so there is nothing real to protect, and the real Render connection string is never given to CI.
3. Ran `npx tsc -b`, `npm run lint` and `npm run build` locally before committing the type-check config. `tsc -b` printed nothing, lint showed 0 errors and 11 warnings (the known `apiCall` pattern), and the build passed.
4. After the merge, added `e2e` to the required checks on `main`, next to `api` and `frontend`. All three must now pass before a pull request can merge.
5. Updated `docs/TEST_STRATEGY.md` and the README so they say the browser tests run in CI. The old wording said they did not, which was no longer true.

**What I did not understand at first:**
- GitHub only offers a check name in the branch protection settings after the job has run once. That is why the job went in first and the rule change came after the merge.
- The branch protection form needs a branch name pattern. Opening the "add rule" form instead of the existing rule for `main` leaves that field empty and blocks saving.
- A command ending in `&` in a CI step runs in the background, so the API keeps running while the next commands run. The health check loop is what stops the tests starting before the API can answer.
- `--no-launch-profile` matters in CI for the same reason as locally (Decision 43): `launchSettings.json` sets Development and would override the Test environment.
- `tsc -b` follows the project references in `tsconfig.json`, so adding a referenced config is enough to bring a new set of files under the type check.

**Where the AI cost me time:** Nothing significant on the code. The job passed on its first run. The steps I was given said to edit the existing rule, but I landed on the new rule form, which cost a short detour.

**Result:** RESOLVED. The first run of the `e2e` job (CI #146 on PR #53) was green: `api` 56s, `frontend` 14s, `e2e` 56s, 58s in total, with no failed run before it. Limits:
- I have not yet seen the `e2e` gate fail. Decision 30 proved it for `api` and `frontend` with PR #37, but not for `e2e`. A gate that has never been seen to fail is the same trap as the `|| true` in Decision 30, so a throwaway branch with one deliberately broken test is still to do.
- CI annotations warn that Node.js 20 is deprecated for `actions/checkout@v4` and `actions/setup-node@v4`. It is a warning, not a failure. The actions need to be upgraded before the runner default changes.
- Only Chromium is used, the API tests still use the in-memory database, and there is no CI job that builds the Docker images yet.

**Source verification:** GitHub Docs on protected branches and required status checks (only checks that ran recently can be selected). GitHub Actions workflow syntax (`timeout-minutes`, `if: failure()`) and the `upload-artifact` action. Playwright documentation on running tests in CI. TypeScript documentation on project references and `tsc -b`. Keep only the sources you actually opened.

---

### Decision 45: The e2e Gate Was Seen To Fail

**Date:** November 1, 2026

**Context:** Decision 44 added the `e2e` job and made it a required check, but listed as a limit that I had never seen it fail. A gate that has never failed might not be a gate, which is the lesson of the `|| true` in Decision 30. PR #37 had proved this for `api` and `frontend`, not for `e2e`.

**What I asked the AI:** For exact steps to break one browser test on a throwaway branch, push it, and watch what CI does, without merging it.

**What it gave me:** A script that changes one expected text in the wrong password test from `Invalid email or password` to `DELIBERATELY WRONG TEXT`, with a check that the text appears exactly once. The change still type checks and lints, so only the browser test can fail.

**What I changed and why:**
1. Ran `npx tsc -b` and `npm run lint` before committing. `tsc` printed nothing and lint showed 0 errors and 11 warnings, the known ones. That confirmed the only thing broken was the test, so a red `e2e` could only mean the gate works.
2. Pushed the branch `test/e2e-gate-fails` and opened a PR marked do not merge. After checking the result I deleted the branch and did not merge it.

**What I did not understand at first:**
- Playwright is configured to retry failed tests twice in CI. One broken test therefore appears three times in the log (the first try, retry #1 and retry #2), and the run takes longer. That is a retry policy, not three separate failures.
- A failing job still runs the steps marked `if: failure()`, so the API log is printed and the Playwright report is uploaded. Those are the first places to look when a real failure happens.
- The red result came from the step `Run the browser tests`. Setup, install and build steps were all green, so a failure there points at the tests and not at the environment.

**Where the AI cost me time:** Nothing significant. I deleted the branch before looking at the PR page, so I did not capture whether the merge button was blocked.

**Result:** RESOLVED for the main question. The `e2e` job failed in 1m 6s (the run on the `test/e2e-gate-fails` branch): 6 tests passed, and the wrong password test failed on the first try and on both retries. The `e2e` gate can fail. Limits: I did not capture the PR page, so the merge block is the behaviour GitHub documents for required checks and not something I recorded myself, and I did not record that `api` and `frontend` stayed green on that run.

**Source verification:** The CI log of the failing run. Playwright documentation on retries. GitHub Docs on protected branches and required status checks.

---

### Decision 46: Integration Tests Against A Real PostgreSQL, And Bounded Paging

**Date:** November 1, 2026

**Context:** CODE_REVIEW.md 3.7 and 4.2, and brief 3.7: integration tests against a real database in a container. All API tests used the in-memory provider, which accepts things PostgreSQL rejects. A negative `offset` or `limit` on the test cases list is one of them: the in-memory database returns 200, PostgreSQL throws and the API answered 500. The list also had no sort order and no maximum page size.

**What I asked the AI:** How to run the real API against a real PostgreSQL in a Testcontainers container, skip locally when Docker is missing, and prove the paging bug on a real database before fixing it.

**What it gave me:**
1. `PostgresApiFactory`: starts a `postgres:15-alpine` container, uses the real migrations through a non-Test environment, and skips when Docker is unavailable (but fails in CI, where `CI=true`).
2. A smoke test (register, sign in, create and list a project) and a theory test that a negative `offset` or `limit` returns 400.
3. The fix: `Paging.Validate` (limit 1 to 200, offset 0 or more, answers 400 `INVALID_PAGING`) and an `OrderBy` on the test cases list, plus in-memory tests so the rule can be seen red and green on my laptop.

**What I changed and why:**
1. Moved building the container from a field initializer into `InitializeAsync`. `Build()` throws when Docker is missing, and in the field initializer that happened before my `try/catch`, so every local `dotnet test` was red instead of skipped.
2. Passed the image name to the builder constructor, because the parameterless one is obsolete (a build warning said so).
3. Wrote the test first and watched it fail on GitHub: `Failed: 2, Passed: 73, Skipped: 0, Total: 75`, with PostgreSQL's own errors `LIMIT must not be negative` and `OFFSET must not be negative`. Then added the fix and the local tests: red `Failed: 4` locally, then `Failed: 0, Passed: 78, Skipped: 3, Total: 81` after the fix.
4. After the merge the CI run on `main` showed `Failed: 0, Passed: 81, Skipped: 0, Total: 81`, which proves the three PostgreSQL tests really ran in CI and were not skipped.
5. Filled in the missing run reference in Decision 45, which had been left as a placeholder.

**What I did not understand at first:**
- A branch push alone runs nothing. The workflow only triggers on pull requests and pushes to `main`, so the red run only appeared once the pull request was open.
- `new PostgreSqlBuilder().Build()` checks for Docker immediately. Where it runs decides whether a missing Docker is a skip or a crash.
- PostgreSQL rejects a negative `LIMIT` or `OFFSET`, but the in-memory provider does not, which is the kind of difference the strategy doc predicted.
- `Skip.IfNot` in a test only helps once the fixture has been built without throwing.
- Paging with `Skip` and `Take` and no `OrderBy` gives no promised order, so the sort is part of the fix and not decoration.

**Where the AI cost me time:**
- The first version of the factory could not skip, and I committed it after a local run that showed one failure. It reached `main` and made `dotnet test` red locally until the fix.
- The expected test count I was given was wrong (82 given, 81 real).
- A leftover placeholder shipped in Decision 45 because the script that wrote it did not check for it.

**Result:** RESOLVED for the test cases list. The CI run on `main` after the merge was green for `api`, `frontend` and `e2e`. Limits: defects, test runs, audit logs and the projects list still have no bounds check or total count, only the test cases list was fixed, and only negative paging has a PostgreSQL test so far.

**Source verification:** PostgreSQL's error text in the failing CI log. Testcontainers for .NET build warning about the obsolete constructor. Microsoft Learn on EF Core paging and ordering. GitHub Docs on workflow triggers for `pull_request` and `push`.

---

### Decision 47: Every List Rejects Bad Paging Values And Has A Stable Order

**Date:** November 7, 2026

**Context:** CODE_REVIEW.md 3.7. Decision 46 fixed the test cases list after a real PostgreSQL showed that a negative `offset` or `limit` answers 500, and that no list had a maximum page size. The same hole was in the defects, projects, test runs and audit log lists.

**What I asked the AI:** To apply the shared `Paging.Validate` rule to each remaining list, one list per pull request, written test first, and to keep the response bodies unchanged so the frontend does not break.

**What it gave me:**
1. For each list, a test file with the four invalid values (`offset=-5`, `limit=-1`, `limit=0`, `limit=201`) and one test for the largest allowed page (`limit=200`).
2. A change in each controller that calls `Paging.Validate` and answers 400 `INVALID_PAGING`.
3. A tiebreaker on `Id` after the main sort, so rows with the same timestamp never swap places between pages.
4. A line in the README known limitations for each list, updated in the same pull request.

**What I changed and why:**
1. One list per pull request, with the red run seen before the fix. Defects: `Failed: 4`, then `Passed: 83` (86 in total). Test runs: `Failed: 4, Passed: 90`, then `Passed: 94` (97 in total). Audit log: `Failed: 4, Passed: 95`, then `Passed: 99` (102 in total). The projects list was done in another session with 6 new tests (86 to 92 in total), and I did not record its red run here.
2. After each controller change I ran `git diff --stat` and compared it with what I expected. The test runs diff was 5 insertions and no deletions, where I had been told 6, so I trusted the output and not the estimate.
3. The paging check sits after the access check, so a caller with no access still gets 401 or 403 and never learns anything from a 400.
4. In the audit log, the paging check sits after the sign in check, so an anonymous caller still gets 401. The existing five audit log tests still pass because the default limit of 100 is inside the allowed range.
5. I kept every response body as it was. Wrapping a list as `{ items, total }` would break the frontend, which reads a plain array with `Array.isArray`.

**What I did not understand at first:**
- Sorting by one column is not enough for paging. Two rows with the same timestamp can come back in a different order on the next query, so a row can appear on two pages or on none. A second sort key that is unique (`Id`) makes the order stable.
- A default page size is also a limit. The Projects page does not pass a limit, so it shows the newest 50 projects. That is fine today, but it needs a total count and a "next page" before a user can have more.
- Only the audit log returns a total count, because it already had one before this work.
- A `git pull` made between two commits on a feature branch can open a merge message editor (nano). `Ctrl+X` closes it, and the merge is recorded.

**Where the AI cost me time:**
- A guessed diff size (6 insertions) did not match the real one (5), which is why I now compare against the output and not the estimate.
- Switching AI in the middle of the defects pull request repeated a step and opened the merge editor. Switching between pull requests has been fine.

**Result:** RESOLVED for the test cases, defects, projects, test runs and audit log lists. Limits: the test run results list is still unbounded, only the audit log returns a total count, the Projects page shows at most the newest 50 projects, and none of these lists has a test on a real PostgreSQL except the test cases list.

**Source verification:** The PostgreSQL error text in the CI log from Decision 46 (`LIMIT must not be negative`, `OFFSET must not be negative`). Microsoft Learn on EF Core paging and ordering. The CI runs on `main` after each merge.

---

### Decision 48: The OpenAPI Document Is Served, And Microsoft.OpenApi Is Upgraded Instead Of Removed

**Date:** November 8, 2026

**Context:** CODE_REVIEW.md 3.6 and 2.7, and brief 3.2, which asks for an OpenAPI spec served by the app. `Microsoft.AspNetCore.OpenApi` was referenced in the project, but `Program.cs` never called `AddOpenApi` or `MapOpenApi`, so `/openapi/v1.json` answered 404. Every `dotnet build` also printed warning NU1903 for `Microsoft.OpenApi` 2.0.0 (GHSA-v5pm-xwqc-g5wc). The review suggested removing that package because the spec was not served, but serving the spec is the reason to keep it.

**What I asked the AI:** How to serve the spec with the built in generator, and how to clear the advisory without removing the package. I also asked for a written brief at the start of each pull request, so that another AI could pick the work up safely.

**What it gave me:**
1. A written PR brief: goal, branch, the expected red and green counts, the expected diff size, and a stop rule if any number differed.
2. Six tests in `OpenApiTests`: the document is served as JSON without signing in, it declares an OpenAPI 3 version, and four main endpoints are listed.
3. Four lines in `Program.cs` (`AddOpenApi` and `MapOpenApi`, with comments) and one line in the project file, a direct reference to `Microsoft.OpenApi` 2.7.5.
4. Guarded edit scripts that stop if the text they look for is not there exactly once.

**What I changed and why:**
1. Wrote the tests first and saw them fail: `Failed: 6, Passed: 99, Skipped: 3, Total: 108`. After the change: `Failed: 0, Passed: 105, Skipped: 3, Total: 108`.
2. Upgraded instead of removing. The advisory text (GitHub advisory GHSA-v5pm-xwqc-g5wc, CVE-2026-49451, read on osv.dev) says the fix is 2.7.5 for the 2.x line and 3.5.4 for the 3.x line. I stayed on 2.x, and the build succeeded with 0 errors and no NU1903 line, which answered the one open question in the brief: the two packages work together.
3. Checked the diff size before committing: 5 insertions and no deletions (4 in `Program.cs`, 1 in the project file), as the brief said.
4. Opened the real running API in a browser, not only the test host: `/openapi/v1.json` returned an OpenAPI 3.1.1 document titled `Testify.Api | v1` with every controller route listed.
5. Updated the README and SECURITY.md in the same pull request, including the limits the document showed, and merged `main` into the branch before writing this entry, because the branch had been created before the last pull and would have clashed with Decision 47 in this file.

**What I did not understand at first:**
- `Microsoft.OpenApi` was not in my project file at all. It arrived through `Microsoft.AspNetCore.OpenApi`, which is why the warning named version 2.0.0. A direct reference to a higher version takes priority over the one that arrives indirectly.
- The advisory is about parsing a crafted document with circular references, which can crash the process. The API only generates a document and never parses one from outside, so the real exposure was small. Upgrading still removes the warning and the scanner flag, and it is cheaper than explaining an open advisory.
- Referencing a package is not the same as using it. Until `AddOpenApi` and `MapOpenApi` were called, the package sat in the project and the endpoint did not exist.
- The generated document is correct about routes and request bodies but shallow. It lists one 200 response for every endpoint, so it does not say that creating returns 201 or that errors are 400, 401, 403 or 404, and it does not mention the Bearer token.
- A branch made before pulling `main` starts from the old `main`. It still merges, but a docs edit at the end of a file that both sides changed would clash.

**Where the AI cost me time:** Nothing significant. The tests, the build and the browser check all matched what the brief predicted.

**Result:** RESOLVED. The brief's requirement is met: the app serves its OpenAPI document and the build no longer warns. Limits: the document has the default title and no description, lists only a 200 response per endpoint, does not describe the Bearer token, and is public in every environment. Adding response types and a security scheme would make it more useful to a reader of the spec.

**Source verification:** GitHub advisory GHSA-v5pm-xwqc-g5wc (CVE-2026-49451) as shown on osv.dev: the affected ranges and the fixed versions. The `dotnet build` output (0 errors, no NU1903) and the generated document opened in the browser.

---

### Decision 49: The API Image Builds And The Compose Stack Starts, Proven In CI

**Date:** November 10, 2026

**Context:** CODE_REVIEW.md 1.4 (the build and the port) and brief 3.3: `docker compose up` must give a working environment. The API Dockerfile ran `dotnet publish api/Testify.Api.csproj`, but compose built it with the `./api` folder as the context, so that path did not exist inside the build. Compose also mapped port 5000 to 5000 while the container listens on 8080. Docker Desktop does not start on my laptop (Decision 33), so the proof had to come from CI.

**What I asked the AI:** For a CI job that proves the API image builds and the stack starts, written so that I could see it fail for the right reason before the fix, and then the smallest fix. I also asked it to start the pull request with a written brief that names the expected red result and the expected size of each diff.

**What it gave me:**
1. The brief: goal, branch, the expected red (`MSB1009`, project file does not exist), the files the fix may touch, and a stop rule.
2. A `containers` job in `ci.yml`: check the compose file, build the API image, start postgres and the API, wait for `/api/v1/health`, check a CORS preflight from `http://localhost:3000`, print the image sizes, print the API log on failure, and always stop the stack.
3. A rewritten `api/Dockerfile` and a three line change in `docker-compose.yml`.

**What I changed and why:**
1. Committed the CI job first and let it fail. The log showed the predicted error at the publish step: `MSBUILD : error MSB1009: Project file does not exist. Switch: api/Testify.Api.csproj`.
2. Dockerfile: the build context is now the repo root, so the paths start with `api/`. The project file is copied and restored before the rest of the code, so the package restore is cached until the project file changes. The published files are copied with `--chown`, because the old `chown` ran before the copy and did nothing (review 4.4). The final image is still runtime only and still runs as a non-root user.
3. Compose: `context: .`, `dockerfile: api/Dockerfile`, and `"5000:8080"`. The frontend lines and `VITE_API_URL` on port 5000 are unchanged, because the browser reaches the API through the published host port.
4. Checked each diff against the brief: compose 3 insertions and 3 deletions, the Dockerfile 12 insertions and 7 deletions. The `ci.yml` diff showed 46 insertions and 1 deletion, not the "50 to 55 insertions and no deletions" I had been told. I looked at the removed line: it was the old last line, `retention-days: 7`, marked "No newline at end of file". The file had no final newline, and adding one makes Git show that line as removed and added again.
5. After the fix all four checks were green. In the `containers` log: one `curl: (56) Recv failure: Connection reset by peer` and then `The API is healthy`; the preflight answered `Access-Control-Allow-Origin: http://localhost:3000`; the API image `testify-app-api` was 240MB and `postgres:15-alpine` was 292MB.

**What I did not understand at first:**
- The path in a `COPY` or in `dotnet publish` is relative to the build context, not to where the Dockerfile sits. Compose chose `./api` as the context, so anything outside that folder did not exist during the build. Moving the context to the repo root and naming the Dockerfile with a path fixes both.
- A port mapping is host port, then container port. `"5000:5000"` published a port nothing was listening on, because the app listens on the 8080 set by `ASPNETCORE_URLS`. `EXPOSE` documents a port but does not publish one.
- One failed health request is expected while the container starts. The published port can accept a connection before the app inside is ready, so the first request can be reset. The loop retries for up to a minute, so a single early failure is not a failure of the job.
- The CORS preflight check proves more than a status code would: it shows that the `CORS_ALLOWED_ORIGINS` value from compose reaches the app inside the container.
- The image list in the log also shows many images that are not mine (the runner's own tooling). Only `testify-app-api` and the postgres image belong to this project.

**Where the AI cost me time:**
- The expected size of the `ci.yml` diff was wrong (50 to 55 insertions and no deletions, where the real one was 46 and 1). The stop rule triggered, and checking it cost a round trip, but it found a harmless cause.
- I pasted a second block while the first push was still printing, which mixed the output. The results were intact, but the terminal log was hard to read.

**Result:** RESOLVED for the API image and the compose stack of postgres and the API, proven in CI. Limits: the web app container is not started (its Dockerfile still runs the Vite dev server), the compose file still uses local development defaults for the JWT secret and the database password, the 240MB image size has not been broken down by layer, and the `containers` check covers the build, the startup, the health endpoint and the CORS header, not the business rules. I made it a required check on `main` after its first green run. The job did fail when it should (the MSB1009 run), which is the evidence that it can fail.

**Source verification:** The CI log of the failing run (the MSB1009 error at the publish step) and of the green run (health, CORS header, image sizes), and the diffs of `docker-compose.yml`, `api/Dockerfile` and `ci.yml`. Docker's documentation on the build context and on publishing ports is still to be read, and this line should say so once it has been.

---

**Note:** This log is living. New decisions will be added as work continues.

Last updated: November 10, 2026