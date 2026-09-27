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

**Note:** This log is living. New decisions will be added as work continues.

Last updated: September 27, 2026