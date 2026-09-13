# Testify: Decision Log - Milestone 2

**Milestone:** Days 3-5
**Status:** All entities have working CRUD, validation, pagination, OpenAPI spec served, integration tests green (5/5 passing).

---

## Decision Template

**Context:** What problem was I solving? What was the trigger?

**What I asked the AI:** The substance of the prompt (paraphrased—not a copy-paste, just what I was asking for)

**What it gave me:** Summarised—what did the AI produce? How long was it? What was the shape of the response?

**What I changed and why:** This is the critical part. What did I alter? Why didn't the AI's suggestion work as-is? What did I remove, add, or rewrite?

**What I did not understand at first:** Be honest here. Did you have to read docs to understand it? Did you get an error and have to debug? Did you misread the code? This field is where learning lives.

**Source verification:** Any docs, RFCs, blog posts, or source code I consulted to verify the decision.

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

### Decision 18: TestRunResults CRUD and Dashboard Security Gap — Found and Fixed Retroactively (Post-M3)

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

**Last updated:** 2026-09-13
**Status:** Milestone 2 complete. 5 integration tests passing (5/5). 4 production bugs fixed. Ready for Milestone 3.
**Next:** Start Milestone 3 (Authentication, Authorization, Audit Logging)