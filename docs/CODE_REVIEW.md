# Testify Code Review

**Date:** 7 October 2026
**Reviewed commit:** `18ffcbc` (main)
**Reviewer:** Eric Truter (with Claude Code)

---

## How to use this document

Every finding below was **verified by running the code**, not by reading it. Each one includes
the evidence and a way to reproduce it yourself.

I have deliberately **not fixed anything**. Rule 1 of the challenge is that you own every line,
so the diagnosis and the reproduction are here, and the fix is yours. If you disagree with a
finding, reproduce it first, then tell me why I am wrong. That is a perfectly good outcome.

Work top to bottom. The ordering is deliberate: the top items block everything underneath them.

---

## 0. Do this before you write any code

### 0.1 Rotate the database password. Today.

`api/appsettings.json:10` contains a working Render PostgreSQL password, committed in
`25a7143` ("feat: configure Render PostgreSQL connection string") and still present at HEAD
in a public repository.

```
Host=dpg-<redacted>.oregon-postgres.render.com;Port=5432;Database=<redacted>;
Username=<redacted>;Password=<REDACTED - 32 chars, see appsettings.json>;SSL Mode=Require
```

The live value is deliberately not reproduced in this document, because this document is itself
committed to the same public repository. Read it from `api/appsettings.json:10`, or from
`git show 25a7143 -- api/appsettings.json`, and verify the exposure with:

```bash
# substitute the password value from appsettings.json; do not paste it into a tracked file
git log --all --oneline -S '<password-from-appsettings>'
```

Assume this credential is compromised. Public repositories are scraped continuously by
automated credential harvesters, usually within minutes of a push.

What needs to happen, in this order:

1. Rotate the password in the Render dashboard.
2. Move the connection string out of `appsettings.json` and into an environment variable.
3. Scrub it from git history (`git filter-repo` or BFG), then force-push.
4. Write up the incident in `docs/DECISIONS.md`: how it got there, how long it was exposed,
   what you did about it, and what you changed so it cannot recur.

`docs/SECURITY.md:146` currently lists "Database connection string hardcoded" under
*Environment-Specific Considerations*, as something to tidy up in production. That framing is
the real problem. A live production credential in a public repo is an incident, not a backlog
item. The brief is explicit: "If a secret ever lands in a commit, rotate it and write up what
happened."

### 0.2 `.env` is tracked despite being gitignored

```bash
git log --oneline -- .env    # committed in 0dd2af5
```

The values are local-only, so this is low severity, but it shows `.gitignore` was added after
the file was already tracked. `git rm --cached .env` and confirm `.env.example` is the only one
in the repo.

### 0.3 Pressing F5 writes to production

This one caught me out during the review, and it is worth understanding properly because it is
the most dangerous thing in the repository.

I started the API intending to use the in-memory test database, setting
`ASPNETCORE_ENVIRONMENT=Test`. The app came up as **Development** and connected to the live
Render database anyway.

Why: `api/Properties/launchSettings.json:10` hardcodes
`"ASPNETCORE_ENVIRONMENT": "Development"`, and for `dotnet run` the launch profile takes
precedence over the ambient environment. Development then falls through to the hardcoded
production connection string in `appsettings.json`.

Net effect: **anyone who clones this repo and presses F5 in their IDE is pointed at
production.** It ran `Database.Migrate()` against it. In my case no migrations were pending, so
nothing was written (`No migrations were applied`), and I verified zero
INSERT/UPDATE/DELETE/ALTER/DROP statements were issued. Next time there is a pending migration,
it will silently apply it to the live database.

Fix the connection string handling (0.1) and this hazard goes away with it.

---

## 1. Blockers

These stop the application from working at all. Nothing else matters until they are cleared.

### 1.1 The frontend does not compile

```bash
cd frontend && npx tsc -b
```

Nine errors. The two that matter:

```
src/pages/ProjectDetailPage.tsx(7,10): error TS2305:
  Module './tabs/TestRunsTab' has no exported member 'TestRunsTab'.
src/pages/ProjectDetailPage.tsx(8,10): error TS2305:
  Module './tabs/DefectsTab' has no exported member 'DefectsTab'.
```

All three stub tab files export a function literally named `DashboardTab`. So
`frontend/src/pages/tabs/DefectsTab.tsx:1` exports `DashboardTab`, returning
`"Defects coming soon"`. `ProjectDetailPage` imports `DefectsTab`, gets `undefined`, and React
throws at render time when you click that tab.

The remaining seven errors are the same copy-paste in all three stubs: the props are destructured
as `_projectId` while the type declares `projectId`.

Consequence: `npm run build` fails, so the frontend Docker image cannot be built, so the app
cannot be deployed.

**This is the most important lesson in the review.** Your CI runs `oxlint` and nothing else. It
never runs `tsc`, so a frontend that does not compile reached `main` through a pull request and
sat there. Adding a type-check step to CI would have caught this on the PR that introduced it.
See 4.1.

### 1.2 Three of the four tabs are stubs

| File | Renders |
|---|---|
| `frontend/src/pages/tabs/TestRunsTab.tsx` | `"Test Runs coming soon"` |
| `frontend/src/pages/tabs/DefectsTab.tsx` | `"Defects coming soon"` |
| `frontend/src/pages/tabs/DashboardTab.tsx` | `"Dashboard coming soon"` |

Test run execution, the defect list, and the dashboard are all mandatory section 2 features and
none of them exist in the UI. `TestCasesTab` is the only real one, and it is good work, which
makes the contrast stark.

### 1.3 There is no way to create a project in the UI

`frontend/src/pages/ProjectListPage.tsx:35` navigates to `/projects/new`. That route does not
exist in `App.tsx`. React Router v6 ranks routes rather than matching in order, so
`/projects/:id` matches with `id="new"`, loading the detail page, which calls
`GET /api/v1/projects/new`, which fails Guid model binding and returns 400.

Click "Create Project" and you get an error page. "Create a project" is the first mandatory
feature in the brief.

### 1.4 `docker compose up` cannot work

Three independent reasons, all of which need fixing:

1. **Port mismatch.** `api/Dockerfile:13-14` exposes 8080 and sets
   `ASPNETCORE_URLS=http://+:8080`. `docker-compose.yml:31` maps `"5000:5000"`. Nothing listens
   on 5000.
2. **`DATABASE_URL` is never read.** `docker-compose.yml:27` sets it; no code anywhere reads it.
   ```bash
   grep -rn "DATABASE_URL" --include=*.cs .    # no results
   ```
   `api/Program.cs:51` reads `ConnectionStrings:DefaultConnection` instead, so compose falls back
   to the hardcoded production string. Your "local" environment talks to production.
3. **CORS blocks the frontend.** `api/Program.cs:33` allows only `http://localhost:5173`. Compose
   serves the frontend on host port 3000.

The brief's definition of done is "`git clone` then `docker compose up` gives a working local
environment with no manual steps beyond copying `.env.example`". Test this from a fresh clone in
a temp directory, not from your working copy.

### 1.5 The deployed frontend cannot reach the API

```bash
grep -rn "localhost:5000" frontend/src
```

Six hardcoded `http://localhost:5000` URLs across `context/AuthContext.tsx` and `hooks/useApi.ts`.
`VITE_API_URL` is set in both `docker-compose.yml:43` and `frontend/Dockerfile:7`, and read
nowhere:

```bash
grep -rn "import.meta.env" frontend/src    # no results
```

A deployed user's browser would call their own machine on port 5000. Brief 3.4 requires
configuration through environment variables only.

---

## 2. Security

### 2.1 Broken access control: anonymous read of everything

Reproduce with no token at all:

```bash
curl -i http://localhost:5137/api/v1/projects
# HTTP 200 - every project belonging to every user

curl -i http://localhost:5137/api/v1/projects/{any-id}
# HTTP 200 {"name":"Alice Project","description":"secret","ownerId":"..."}
```

`ProjectsController.cs:30-45` has no authentication or authorization check at all. `GetProjects`
is `_db.Projects.ToListAsync()` with no filter. The same applies to every nested collection:

| Endpoint | File |
|---|---|
| `GET /projects` and `GET /projects/{id}` | `ProjectsController.cs:30,37` |
| `GET /projects/{id}/test-cases` (+ by id) | `TestCasesController.cs:30,42` |
| `GET /projects/{id}/test-runs` (+ by id) | `TestRunsController.cs:30,42` |
| `GET /projects/{id}/defects` (+ by id) | `DefectsController.cs:30,47` |
| `GET /projects/{id}/test-runs/{runId}/results` | `TestRunResultsController.cs:30` |

`docs/SECURITY.md:7` says "Every endpoint checks that you own the project before letting you see
or change anything." `docs/SECURITY.md:96` says "All endpoints check authorization before
responding." Both are false.

Brief 3.5: "a user can only see and modify data in projects they belong to. Prove this with a
test." Right now the opposite is true, and see 2.6 for what the test suite claims.

### 2.2 The audit log is readable by any authenticated user

```bash
# as an ordinary user who owns nothing and is a member of nothing
curl -H "Authorization: Bearer $TOKEN" http://localhost:5137/api/v1/admin/logs
# HTTP 200, full audit history for every project and every user
```

I confirmed a second user retrieving 18 entries including the first user's email address. The
response includes user emails, usernames, IP addresses, and action details across all tenants.

`AdminController.cs:41-45` documents the gap in its own comments:

```csharp
// Only owner (Chris) can view audit logs
// For now, we'll allow anyone authenticated to view logs (this should be restricted later)
```

`docs/SECURITY.md:108` claims "Accessible via GET /api/v1/admin/logs (Owner only)".

Worth sitting with: the code was honest and the documentation was not. If you had shipped only
the comment, this would read as known, accepted debt. The claim in `SECURITY.md` is what turns it
into a problem.

### 2.3 Every `Forbid()` returns 500 instead of 403

19 call sites, all broken. Reproduce by having user B attempt to update user A's project:

```
HTTP 500

System.InvalidOperationException: No authenticationScheme was specified, and there was no
DefaultForbidScheme found. The default schemes can be set using either
AddAuthentication(string defaultScheme) or AddAuthentication(Action<AuthenticationOptions> ...)
   at Microsoft.AspNetCore.Mvc.ForbidResult.ExecuteResultAsync(ActionContext context)
```

`api/Program.cs` never calls `AddAuthentication()`. It calls `app.UseAuthorization()` at line 67
without it, and `AuthenticationMiddleware` hand-rolls the token parsing instead.

This is worth understanding properly rather than patching. Your custom middleware sets
`context.User` from a validated JWT, which is enough for `[Authorize]` to pass, but
`ControllerBase.Forbid()` delegates to `IAuthenticationService.ForbidAsync`, which does not exist
because `AddAuthentication()` was never called. Decide whether you want the real
`AddAuthentication().AddJwtBearer(...)` pipeline, or to keep the custom middleware and stop using
`Forbid()`. Either is defensible. Pick one and be able to explain the trade-off.

### 2.4 The project owner cannot write to their own project

`ProjectsController.CreateProject` (`ProjectsController.cs:60-71`) inserts the `Project` and never
inserts a `ProjectMember` row for the owner. Every write on test cases, runs, results, and defects
checks membership:

```csharp
var member = await _db.ProjectMembers.FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == userId);
if (member == null || member.Role == "viewer")
    return Forbid();
```

So create a project, then try to add a test case to it, and you get a 500 (via 2.3). The core
workflow is dead end to end unless you invite a second user and act as them. I had to do exactly
that to test the rest of the API.

Note the inconsistency to resolve while you are in here: `DashboardController.cs:36` is the only
place that treats the owner as authorized (`if (member == null && project.OwnerId != userId)`),
while `ProjectsController` checks `OwnerId` only and ignores membership entirely. You have three
different authorization models in one codebase. Settle on one.

### 2.5 Tokens are stored in plaintext, and refresh tokens never rotate

`api/Models/RefreshToken.cs:9` stores `Token` as a raw string, and `AuthController.cs:104-117`
saves the unhashed GUID. Same for `PasswordReset.Token` (`AuthController.cs:209-219`). A database
read gives an attacker working refresh tokens and working password-reset tokens.

`docs/SECURITY.md:133` claims "Refresh tokens stored hashed (not plaintext)".
`docs/SECURITY.md:13` claims "Refresh tokens rotate". `AuthController.Refresh` issues a new access
token and leaves the refresh token untouched, so neither is true.

### 2.6 There is no authorization test, and `SECURITY.md` presents one that does not exist

`docs/SECURITY.md:59-73` shows this as proof that authorization works:

```csharp
[Fact]
public async Task GetProject_WithoutAuth_Returns401()
{
    var response = await _client.GetAsync("/api/v1/projects");
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
}
```

That test does not exist in the repository. `_authenticatedClient` does not exist either. Try to
find them:

```bash
grep -rn "GetProject_WithoutAuth_Returns401\|_authenticatedClient" api.tests/
```

The test that does exist asserts the opposite. `api.tests/ProjectsControllerTests.cs:30-35`:

```csharp
[Fact]
public async Task GetProjects_ReturnsOk()
{
    var response = await _client.GetAsync("/api/v1/projects");
    Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
}
```

That is an unauthenticated request to a list-all endpoint, asserting 200. The test suite encodes
the vulnerability in 2.1 as intended behaviour, and it passes, so the suite stays green while the
API leaks every tenant's data.

Of everything in this review, this is the finding I would most want you to think about. A green
test suite that asserts the bug is worse than no test suite, because it actively tells you the
problem is not there.

### 2.7 Other security items

- **Rate limiting is not on the auth endpoints.** `Program.cs:14-23` configures one global rule,
  `Endpoint = "*"`, 100 requests per minute. Brief 3.5 asks specifically for rate limiting on
  authentication endpoints; 100 login attempts per minute per IP is weak brute-force protection.
- **Per-IP keying is wrong behind a proxy.** There is no `UseForwardedHeaders`, so on Render every
  request appears to come from the proxy and shares one bucket. This also makes every IP address
  in your audit log the proxy's.
- **Dead rate-limit attribute.** `HealthController.cs:19` has `[EnableRateLimiting("fixed")]`,
  which belongs to the built-in `AddRateLimiter` API. You use `AspNetCoreRateLimit` instead, and
  no policy named `"fixed"` is registered anywhere. The attribute does nothing.
- **CORS is wider than documented.** `Program.cs:34-36` uses `AllowAnyMethod()` and
  `AllowAnyHeader()`. `docs/SECURITY.md:40-41` claims specific method and header allowlists.
  There is no production origin configured at all.
- **The health endpoint leaks exception detail.** `HealthController.cs:38-44` returns
  `error = ex.Message` to unauthenticated callers. Npgsql exceptions can include host and database
  names.
- **No dependency scanning.** `docs/SECURITY.md:17,51` claim "Dependabot scans automatically" and
  "GitHub Dependabot enabled on repository". There is no `.github/dependabot.yml`:
  ```bash
  ls .github/    # workflows/ only
  ```
  Meanwhile `dotnet build` emits NU1903 on every single build: `Microsoft.OpenApi` 2.0.0 has a
  known **high** severity advisory (GHSA-v5pm-xwqc-g5wc). `SECURITY.md:53` calls it low risk and
  documentation-only; `SECURITY.md:138` correctly calls it high severity. Both are in the same
  file.
  The actual triage answer: OpenAPI generation is not even switched on (see 3.6), so the package
  is unused attack surface and can simply be removed. Brief 3.5 asks for findings "triaged, not
  ignored", and removing a dependency you do not use is the strongest possible triage.
- **`User.PasswordHash` has no `[JsonIgnore]`.** It did not leak in any response I probed, because
  the `Project.Owner` navigation was never loaded. But controllers return raw EF entities (see
  3.1), so this is one eager-load away from serialising password hashes to clients. Add the
  attribute regardless of whether you adopt DTOs.

---

## 3. Correctness

### 3.1 Writes succeed and then return 500

`POST /test-cases`, `POST /test-runs`, and `POST /test-runs/{id}/results` all returned 500 during
my testing, while the entity was saved:

```
Saved 1 entities to in-memory store.
System.Text.Json.JsonException: A possible object cycle was detected.
Path: $.Project.Members.Project.Members.Project.Members.Project...
```

Root cause: controllers return raw EF entities with bidirectional navigation properties
(`TestCase.Project` to `Project.TestCases` and back). Once the change tracker has populated those
navigations, which it does after a create in the same `DbContext`, serialization recurses until
it hits the depth limit.

This is nastier than a plain 500 because the write already committed. A client that retries on
500 duplicates data. It is also intermittent by endpoint, depending on what happens to be
tracked, which makes it miserable to debug from symptoms alone.

The fix is DTOs on the way out, not `ReferenceHandler.Preserve`. Returning entities is also what
creates the `PasswordHash` exposure in 2.7 and leaks internal columns like `ownerId` to anonymous
callers.

### 3.2 The dashboard's open-defect metric is always empty

`DefectsController.cs:88` writes `Status = "open"`. `DashboardController.cs:57` filters
`d.Status == "Open"`. `api/Models/Defect.cs:11` defaults to `"Open"`. PostgreSQL string comparison
is case-sensitive, so the filter never matches anything created through the API.

With exactly one open Critical defect in the project:

```json
{"open_defects":0,
 "defects_by_severity":[],
 "defects_by_status":[{"status":"open","count":1}],
 "recent_defects":[{"title":"Bug 1","severity":"Critical","status":"open",...}]}
```

"Open defects by severity" is a named, mandatory dashboard metric in brief section 2, and it
returns an empty array every time. Note that `defects_by_status` and `recent_defects` right next
to it both see the defect, which is what makes the single zero so easy to miss.

Fix the casing in one place, then decide where the canonical set of status values lives so this
cannot drift again (see 3.3).

### 3.3 No validation on any enum except test results

```bash
curl -X PUT .../test-runs/{id} -d '{"status":"totally-made-up-status"}'
# HTTP 200, persisted verbatim
```

| Field | Brief requires | Actually accepted |
|---|---|---|
| `TestCase.Priority` | Low / Medium / High / Critical | any string |
| `TestRun.Status` | In Progress / Complete | any string |
| `Defect.Severity` | a defined set | any string |
| `Defect.Status` | Open / In Progress / Resolved / Closed | any string |
| `TestRunResult.Result` | Not Run / Pass / Fail / Blocked | **correctly validated** |

`TestRunResultsController.cs:128-130` does it right. Copy that pattern, or better, promote these
to real types so the compiler enforces them.

Two related inconsistencies: `TestRunsController.cs:71` creates runs with `Status = "pending"`,
which is not one of the brief's allowed values and does not match the model default of
`"In Progress"` in `api/Models/TestRun.cs:10`. And brief 3.7 asks for unit tests on "status
transitions", which cannot exist while any string is legal.

`docs/SECURITY.md:31` claims server-side validation of "enum values".

### 3.4 Authentication flow is broken in three places

- **Registration bounces you to the login page.** `AuthController.Register` returns
  `{ id, email, username, createdAt }` with no token. `AuthContext.tsx:74-76` reads
  `data.accessToken`, gets `undefined`, leaves `isAuthenticated` false, and `RegisterPage.tsx:40`
  navigates to `/projects`, where `ProtectedRoute` redirects to `/login`. Registration looks like
  a silent failure to the user.
- **Every page reload logs you out.** Login sets the refresh token as an httpOnly cookie
  (`AuthController.cs:120-130`), but `/auth/refresh` reads it from `[FromBody]`
  (`AuthController.cs:151`). JavaScript cannot read an httpOnly cookie to put it in a body, by
  design. So `AuthContext.tsx:29` silent refresh can never succeed. This is the one finding where
  your instinct was right and the wiring was wrong: httpOnly cookies are the correct choice, the
  endpoint just needs to read `Request.Cookies["refreshToken"]`.
- **The refresh response has no user.** `AuthContext.tsx:36` reads `data.user`;
  `AuthController.Refresh` returns only `{ accessToken, expiresIn }`.

Also worth fixing while you are here: `RegisterPage.tsx:122` uses `<a href="/login">` instead of
react-router's `<Link>`. That triggers a full page reload, which discards the in-memory access
token. In an SPA, every internal navigation should be a `<Link>`.

### 3.5 Test case editing returns 405

`frontend/src/components/TestCaseEditor.tsx:47` sends `PATCH`. `TestCasesController.cs:99` maps
`[HttpPut("{id}")]` only. Editing a test case cannot work.

`README.md:21` also documents `PATCH`, as do the project and defect update endpoints. So your
README and your frontend agree with each other and both disagree with the API. Pick PUT or PATCH
deliberately (PATCH is the better fit for partial updates, which is what the editor sends) and
make all three consistent.

### 3.6 No OpenAPI spec is served

```bash
curl -o /dev/null -w "%{http_code}\n" http://localhost:5137/openapi/v1.json         # 404
curl -o /dev/null -w "%{http_code}\n" http://localhost:5137/swagger/v1/swagger.json # 404
curl -o /dev/null -w "%{http_code}\n" http://localhost:5137/swagger                 # 404
```

`Microsoft.AspNetCore.OpenApi` is referenced in `api/Testify.Api.csproj:12`, but `Program.cs`
never calls `AddOpenApi()` or `MapOpenApi()`. It calls `AddEndpointsApiExplorer()` at line 27,
which only populates endpoint metadata and serves nothing.

Brief 3.2 requires an OpenAPI spec "served by the app". `docs/API_ENDPOINTS.md` is hand-written
documentation, not a served spec, and it has already drifted from the implementation (3.5).

### 3.7 Pagination is unbounded and inconsistent

```bash
curl ".../test-cases?offset=-5"        # HTTP 200
curl ".../test-cases?limit=9999999"    # HTTP 200
```

- No bounds checking on `limit` or `offset`. A negative offset becomes `OFFSET -5` in PostgreSQL,
  which is a SQL error and a 500 (it passes silently on the in-memory provider your tests use,
  which is why you have not seen it).
- No maximum page size.
- Responses are bare JSON arrays with no total count or page metadata, so a client cannot know
  whether more pages exist. `AdminController.cs:91-97` does return `{ logs, total, limit, offset }`,
  which is the right shape. Make the others match it.
- `GET /api/v1/projects` has no pagination at all (`ProjectsController.cs:33`), and it is the one
  endpoint guaranteed to grow unbounded.

### 3.8 Error response shapes are inconsistent

Brief 3.2 requires a consistent error shape across every endpoint. There are two:

```jsonc
// hand-rolled, in every controller
{"error":{"message":"Name is required","code":"INVALID_NAME"},"status":400}

// ASP.NET Core ProblemDetails, on malformed JSON or a bad Guid in the path
{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1",
 "title":"One or more validation errors occurred.","status":400,
 "errors":{"id":["The value 'not-a-guid' is not valid."]},"traceId":"..."}
```

Anything the framework rejects before your code runs (model binding, `[ApiController]` automatic
400s, unhandled exceptions) uses the second shape. Either adopt `ProblemDetails` everywhere, which
is less code and is the platform default, or add an exception filter plus
`ConfigureApiBehaviorOptions` to force everything into your custom shape. There is also no
`UseExceptionHandler`, so unhandled 500s have no consistent body at all.

### 3.9 Defect creation does not validate its foreign keys

`DefectsController.cs:80-91` takes `req.TestCaseId` and `req.TestRunResultId` at face value:

- Neither is checked against `projectId`. A member of project A can create a defect pointing at a
  test case in project B, which then appears in project B's defect list.
- `CreateDefectRequest.TestRunResultId` is a non-nullable `Guid`, so omitting it gives
  `Guid.Empty`. With `DeleteBehavior.Restrict` on that FK (`TestifyDbContext.cs:138-142`) this is a
  constraint violation, surfacing as an unhandled 500 rather than a 400.

The brief says a defect is "raised from a failed test case in a run" and "links back to the test
case and run that produced it". So both IDs should be required, validated as belonging to this
project, and ideally validated as belonging to a result whose status is `Fail`.

### 3.10 Client-side validation does not mirror the server

Brief 3.1: "Form validation on the client that matches the validation on the server."

| Rule | Server (`AuthController.cs:47-58`) | Client (`RegisterPage.cs:15-29`) |
|---|---|---|
| Password >= 8 chars | yes | yes |
| Password needs uppercase | yes | **no** |
| Password needs digit | yes | **no** |
| Password needs special char | yes | **no** |
| Username 3 to 50 chars | yes | **no** |

Type `password` into the form and the client accepts it, then the server returns 400. This is also
why two of your E2E tests fail (4.2).

`docs/SECURITY.md:32` claims "Client-side validation mirrors server rules".

### 3.11 Dead code and dangling links

- `ProjectMembersController.cs:81` generates `inviteToken`, emails it at line 86, and never stores
  it. There is nothing to validate an acceptance against, so the invite flow is decorative.
- `Services/EmailService.cs:20,38` link to `/reset-password` and `/accept-invite`. Neither route
  exists in `App.tsx`. Both are hardcoded to `http://localhost:5173`, which is another env-var
  violation.
- There is no endpoint to list project members, so a UI cannot show who is on a project.
- `api.tests/UnitTest1.cs` is an empty test that always passes. Delete it.

---

## 4. CI, testing, and infrastructure

### 4.1 CI does almost nothing

`.github/workflows/ci.yml` is 39 lines and runs two lint steps. Brief 3.6 requires that every PR
"lints, type-checks, runs tests, builds both images".

| Required | Present |
|---|---|
| Lint | Yes, but see below |
| Type-check (`tsc --noEmit`) | **No.** This is why 1.1 reached main |
| Run tests | **No.** `dotnet test` is never invoked |
| Build both images | **No** |
| Deploy on merge to main | **No** |

Two specific problems in what is there:

```yaml
run: dotnet format --verify-no-changes --verbosity diagnostic || true
```

`|| true` swallows the exit code, so the lint gate can never fail. A gate that cannot fail is not
a gate.

```yaml
run: rm -rf node_modules package-lock.json && npm install
```

This deletes the committed lockfile and resolves fresh versions on every run, so CI is not testing
the dependency tree you committed. Use `npm ci`, which exists precisely for this.

Brief 3.7 also requires "Tests run in CI and block the merge when they fail", and 3.6 requires a
branch protection rule on `main` requiring the pipeline to pass. Worth checking whether branch
protection is actually enabled, since with the current pipeline it would only be enforcing lint.

### 4.2 Testing is the biggest gap, and it was meant to be your strongest area

The brief set a higher bar here explicitly: "You are a QA. This is your strength, so I am setting
the bar higher here than elsewhere."

Current state: `dotnet test` passes 4 of 4. One is empty (`UnitTest1.cs`), so there are **three
real backend assertions**, and one of those asserts a security bug is correct behaviour (2.6).

Against brief 3.7:

- **Unit tests on business logic:** none. The brief names three targets specifically. Pass rate
  calculation (`DashboardController.cs:49`) is pure arithmetic and trivially testable, and a test
  would have caught 3.2 immediately. Status transitions cannot be tested while any string is legal
  (3.3). Authorization rules are untested, which is how 2.1 through 2.4 all survived.
- **Integration tests against a real database in a container:** none. The only tests use the
  in-memory provider, which is explicitly not what the brief asked for, and which hides real bugs:
  3.7's negative-offset failure and 3.2's case-sensitive comparison both behave differently on
  PostgreSQL. Testcontainers is the usual answer here.
- **At least six E2E tests covering the critical paths:** there are exactly six, but the paths do
  not line up.

  | Brief's required path | Covered |
  |---|---|
  | register | `auth.spec.ts` |
  | log in | `auth.spec.ts` |
  | create a project | `projects.spec.ts` |
  | create a case | `testcases.spec.ts` |
  | **execute a run** | **no test exists** |
  | raise a defect | `defects.spec.ts` |

  Plus a bonus "list projects". So you hit the count and missed one of the six named paths.

  More importantly, several of these must currently fail:
  - `auth.spec.ts:5` uses password `TestPassword123`, which has no special character, so the
    server rejects it with 400 (3.10).
  - `auth.spec.ts:16` expects the URL to contain `/projects` after registering, which cannot
    happen (3.4).
  - `defects.spec.ts` drives the Defects tab, which is a `"coming soon"` stub (1.2).
  - `projects.spec.ts` creates a project through a UI path that does not exist (1.3).
  - `defects.spec.ts:7` depends on a pre-existing user `test@example.com` and a project named
    `Test Project`, with no seeding or fixtures.

  Because Playwright never runs in CI, none of this was visible. `playwright.config.ts:27` also
  starts only `npm run dev`, with no API and no database, so the suite has no backend to talk to.
- **A written test strategy:** not present. This is the cheapest item on the list and the one most
  aligned with your background. Write it.

A suggestion on sequencing, since this is the area you will be judged hardest on: write the
authorization test from 2.6 *first*, watch it fail against the current code, then fix 2.1. That
gives you a genuine red-green cycle on a real vulnerability, which is a much stronger thing to
walk me through than a passing suite.

### 4.3 No infrastructure as code

```bash
find . -name "*.tf" -o -name "render.yaml"    # nothing
```

Brief 3.4 requires infrastructure defined as code, Terraform preferred. The Render setup appears
to have been done by hand in the dashboard. A `render.yaml` blueprint is the lowest-effort way to
satisfy this and is a documented equivalent.

Also missing from 3.4: documented database backup or persistence strategy.

### 4.4 The frontend Dockerfile is not production-shaped

`frontend/Dockerfile` is 8 lines and violates most of brief 3.3:

```dockerfile
FROM node:18-alpine        # Vite 8 requires Node 20.19+; this image cannot run it
RUN npm install            # ignores the committed lockfile; should be npm ci
COPY . .                   # ships all source into the final image
ENV VITE_API_URL=...       # environment-specific config baked into the image
CMD ["npm", "run", "dev"]  # ships the Vite DEV SERVER as the production command
```

- **Not multi-stage.** Brief: "Your final images must not contain build tooling or source." This
  image is build tooling and source.
- **Runs as root.** No `USER` directive. Brief: "Containers run as a non-root user." Your
  `api/Dockerfile` gets this right, so compare the two.
- **Node 18 cannot run this toolchain.** `package.json` pins Vite 8, TypeScript 6, and
  `@types/node` 24, and CI uses Node 22.

The API Dockerfile is much better: genuinely multi-stage, non-root, runtime-only base image. Two
small things there: `chown -R appuser:appuser /app` on line 10 runs *before* `COPY --from=builder`
on line 11, so the copied files are still root-owned (harmless for read-only access, but the chown
is doing nothing); and `COPY . .` with a build context of `./api` means
`dotnet publish api/Testify.Api.csproj` on line 5 refers to a path that does not exist inside the
image. Verify the build actually succeeds from compose.

Brief 3.3 also requires image sizes recorded in the README, which is not done.

---

## 5. Documentation that needs correcting

`docs/SECURITY.md` needs a careful pass. These claims are contradicted by the code:

| Line | Claim | Reality |
|---|---|---|
| 7 | "Every endpoint checks that you own the project" | 2.1 |
| 96 | "All endpoints check authorization before responding" | 2.1 |
| 108 | "admin/logs (Owner only)" | 2.2 |
| 13 | "Refresh tokens rotate" | 2.5 |
| 133 | "Refresh tokens stored hashed (not plaintext)" | 2.5 |
| 59-73 | Authorization tests shown as proof | 2.6, they do not exist |
| 17, 51 | "Dependabot scans automatically" / "enabled" | 2.7, no config file |
| 31 | Server-side validation of "enum values" | 3.3 |
| 32 | "Client-side validation mirrors server rules" | 3.10 |
| 38 | CORS includes "deployed domain (prod)" | localhost only |
| 40-41 | Specific CORS method and header allowlists | `AllowAnyMethod` / `AllowAnyHeader` |
| 45-47 | "Rate Limiting - Status: Not implemented" | It is implemented, since Decision 28 |
| 53 vs 138 | "low risk" vs "high-severity" for the same CVE | Contradicts itself |
| 92 | "You are the only Project Owner" | Leftover from an earlier design |

`README.md` gaps against the definition of done ("what it is, architecture diagram, how to run it,
how to deploy it, known limitations"):

- No title or description. The file opens on `## API Endpoints`.
- No architecture diagram, no "how to run it", no "how to deploy it".
- No CI status badge (brief 3.6).
- No image sizes (brief 3.3).
- `README.md:123-128` points at `docs/decisions/Milestone1.md` through `Milestone5.md`. That
  directory does not exist; everything is in `docs/DECISIONS.md`. `README.md:91` has the same
  broken reference.
- `README.md:113` says "6 ESLint warnings remain" and "All production code compiles and runs
  without errors". The project uses oxlint, not ESLint, and the frontend does not compile (1.1).
- `README.md:118` says "2 npm audit moderate vulnerabilities (Microsoft.OpenApi)".
  `Microsoft.OpenApi` is a NuGet package, not npm, and the advisory is high severity.
- `README.md:96-101` describes a silent-refresh flow that cannot work (3.4).
- `README.md:69-91` says deployment is blocked and lists deploying the API as a post-deadline next
  step. If that is still true, M5 is not delivered. If it is now deployed, this section is stale
  and needs the live URL.

A general note on all of this. Across both documents the pattern is the same: they describe the
application you intended rather than the one in the repository. That is an easy habit to fall into
when you write docs from your plan instead of from the running system, and it is the single
highest-value habit to change. Documentation that overstates is worse than no documentation,
because it stops you and everyone else from looking.

Brief Rule 5 is also unaddressed: there is no section in `DECISIONS.md` recording where AI sent
you down a wrong path. Several findings here are good candidates. The `Forbid()` 500 (2.3), the
refresh-token-in-body mistake (3.4), and the `EnableRateLimiting` attribute from the wrong library
(2.7) all look like plausible-but-wrong AI output that was accepted without being run. That is
exactly what the brief wanted logged, and writing it up honestly will score better than leaving it
out.

---

## 6. What is good, and should not change

Worth stating plainly, because the list above is long.

**The decision log is the strongest thing in this repository.** 28 numbered entries, inside the
brief's 15 to 30 range, with "what I did not understand" filled in 28 times and not sanitised.
Decisions 2A and 2B (Fly.io removing its free tier, the AWS payment blocker, the pivot to Render)
and Decision 18 (first PR workflow, CI was broken) read like real engineering, not a writing
exercise. Keep doing this.

Also genuinely good:

- **Access token in React state, not `localStorage`** (`AuthContext.tsx:21`). This is the right
  call for XSS resistance, it is the less obvious option, and the README explains the reasoning.
  Same for choosing httpOnly cookies for the refresh token; the wiring is broken but the design
  instinct was correct.
- **The API Dockerfile** is properly multi-stage and non-root.
- **Schema managed by EF migrations**, not hand-edited. Three migrations, with a model snapshot.
- **Real PR discipline.** 117 commits across 29 merge commits, meaningful branch names
  (`fix/cors-known-origin`, `docs/decision-28-rate-limiting`), readable messages. No giant initial
  commit.
- **`TestCasesTab` is solid work.** Loading, empty, and error states all handled, which is exactly
  what brief 3.1 asked for. `ProjectListPage` and `ProjectDetailPage` too. Build the other three
  tabs to this standard.
- **`TestRunResult.Result` validation** (`TestRunResultsController.cs:128-130`) is the pattern the
  rest of the codebase should follow.
- **Unique index on `(TestRunId, TestCaseId)`** (`TestifyDbContext.cs:127-129`). Good instinct,
  stops duplicate cases in a run at the database level rather than in application code.
- **`forgot-password` returns 200 unconditionally** (`AuthController.cs:203-207`) so it does not
  leak whether an email is registered, with a comment explaining why.
- **Audit logging** is wired through every write path, with a sensible schema and indexes.
- **bcrypt at cost 10** with a documented justification.
- **The EF relationship configuration** in `TestifyDbContext.cs` is careful and deliberate, with
  thought given to cascade versus restrict per relationship.

---

## 7. Suggested order of work

1. Rotate the credential and write up the incident (0.1).
2. Make the frontend compile (1.1). Nothing can be verified until this is done.
3. Add `tsc --noEmit` and `dotnet test` to CI, and remove the `|| true` (4.1). Do this before
   fixing more code, so the pipeline catches regressions from here on.
4. Fix configuration: connection string, `VITE_API_URL`, CORS origin, compose ports (0.3, 1.4,
   1.5). This also removes the production-database hazard.
5. Write the authorization test (2.6), watch it fail, then fix access control (2.1 through 2.4).
6. Fix the write path: DTOs (3.1), enum validation (3.3), the dashboard casing bug (3.2).
7. Fix the auth flow (3.4) and the remaining frontend wiring (1.3, 3.5, 3.10).
8. Build the three missing tabs (1.2).
9. Serve OpenAPI (3.6), tidy pagination (3.7) and error shapes (3.8).
10. Correct `SECURITY.md` and `README.md` against the actual code (5).
11. Add the test strategy, business-logic unit tests, and container-backed integration tests (4.2).
12. Infrastructure as code and the deploy pipeline (4.3, 4.1).

If time is short, items 1 to 6 are the ones I would not skip. They cover the live credential, the
data-leaking authorization hole, and the CI gap that let all of it through.

---

## 8. One thing to take away

The individual bugs here are ordinary. Case-sensitive string comparison, a missing
`AddAuthentication()`, a copy-paste export name. Every working engineer has shipped all three.

What is worth sitting with is the gap between the repository and its own documentation. The code
comments in `AdminController` are honest about the authorization hole. The decision log is honest
about what you did not understand. But `SECURITY.md` asserts protections that are not there and
quotes tests that do not exist, and the one real test asserts that a data leak is correct
behaviour.

That pattern comes from writing documentation and tests from your intent rather than from the
running system, which is exactly the failure mode AI assistance makes easy. The model will happily
produce a confident `SECURITY.md` describing the application you described to it, and it reads
well enough that checking it feels unnecessary.

The habit that fixes it is cheap: before you write down that something works, run it. Before you
assert a test proves something, read the assertion. You already have the instinct for this in your
decision log, where you wrote down what you did not understand instead of papering over it. Apply
the same honesty to the artefacts that make claims about the code, and most of section 5
disappears.

The brief says you cannot pass on a working application alone. The inverse is also true: you
cannot pass on the log alone either. You have the harder half of that pair already.
