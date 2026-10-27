# Testify Security Documentation

This describes the code as it is today: what it protects, how, and what it does not. It is updated when the code changes. Where a claim depends on tests, the test class is named. The reasoning behind each change is in `docs/DECISIONS.md` (Decisions 29 and 32 to 37).

## OWASP Top 10: where we stand

| Risk | Status | What we do, and what is missing |
|---|---|---|
| A01 Broken Access Control | Addressed | Every project endpoint checks the signed-in user's role on the project (owner, editor or viewer). No token gives 401, an unknown project 404, not enough access 403. Covered by `AuthorizationTests` and `AuditLogAccessTests`. Missing: an admin role. |
| A02 Cryptographic Failures | Partly | Passwords use bcrypt (cost 10). Refresh and password reset tokens are random 256 bit values stored as SHA-256 hashes. HTTPS redirection and HSTS are on. Missing: key rotation and a secrets manager. |
| A03 Injection | Addressed | Entity Framework Core parameterises every query and no user input is concatenated into SQL. React escapes rendered values. |
| A04 Insecure Design | Partly | Access tokens last 15 minutes, refresh tokens rotate, roles follow least privilege. Missing: a formal threat model, detection of a stolen refresh token being reused, account lockout. |
| A05 Security Misconfiguration | Partly | Security headers are set, CORS only allows the configured origins (never `*`), and the API refuses to start without its secrets. Missing: CORS still allows any method and header, and `AllowedHosts` is `*`. |
| A06 Vulnerable Components | Not yet | A high severity advisory in `Microsoft.OpenApi` 2.0.0 shows as build warning NU1903, `npm audit` reports 3 frontend vulnerabilities, and Dependabot is not configured. None of it is triaged yet. |
| A07 Authentication Failures | Partly | Password rules, short lived JWT access tokens, an httpOnly, Secure, SameSite Strict refresh cookie limited to `/api/v1/auth`, and single use reset tokens that expire after 1 hour. Covered by `TokenSecurityTests`. Missing: multi factor authentication, lockout, and stricter limits on login attempts. |
| A08 Software and Data Integrity | Partly | Changes reach main through pull requests with required checks, the frontend installs from the lockfile (`npm ci`), and the schema changes through migrations. Missing: signed commits and artifact signing. |
| A09 Logging and Monitoring | Partly | Writes and sign in events go to an audit log (who, what, which project, IP address, when). Missing: failed sign ins are not recorded, there is no central logging or alerting, and the IP address is the proxy's when deployed behind one. |
| A10 Server Side Request Forgery | Not applicable | The server makes no outgoing requests based on user input. |

## Authentication and sessions

- Registration takes an email, a username (3 to 50 characters) and a password of at least 8 characters with an uppercase letter, a digit and a special character. Passwords are hashed with bcrypt, cost 10.
- Login returns a JWT access token (HS256, 15 minutes, issuer and audience checked) and sets the refresh token cookie (7 days, httpOnly, Secure, SameSite Strict, path `/api/v1/auth`). The browser keeps the access token in memory, never in localStorage.
- `POST /auth/refresh` reads the cookie, retires the used token, issues a new one and returns a new access token and the user. `POST /auth/logout` revokes the token and clears the cookie. Only SHA-256 hashes of tokens are stored.
- Password reset: `forgot-password` always answers 200, so it does not reveal which emails are registered. The token is valid for 1 hour, works once, and a successful reset revokes every session. Emails are currently mocked and written to the server log.

## Authorization

- Each project has one owner, the user who created it. Others can be invited as editor or viewer.
- Viewers can read. Editors can also create, change and delete test cases, runs, results and defects. Only the owner can change or delete the project and invite or remove members.
- A defect can only be raised from a failed result of a run in the same project.
- The audit log endpoint returns entries for your own actions and for projects you own.
- A signed in user can tell whether a project id exists (404 versus 403). Ids are random GUIDs.

## Input validation

- The server validates email, username and password rules, required titles and names, and the allowed values for priority, severity, run status, defect status and test result. Invalid values return 400 with a code.
- The browser does not yet check everything the server checks (the password rules beyond length, and the username length).
- Responses are JSON and the frontend renders values through React, which escapes them.

## CORS

Allowed origins come from `CORS_ALLOWED_ORIGINS` (comma separated). The API stops at startup if it is missing or contains `*`. Credentials are allowed, because the refresh cookie needs them. Any method and any header are currently allowed, which should be narrowed.

## Rate limiting

A global limit of 100 requests per minute per IP address (AspNetCoreRateLimit). There is no stricter limit on sign in and registration yet, and behind a proxy every client shares the proxy's address until forwarded headers are configured.

## Security headers

Every API response includes:

- `Strict-Transport-Security: max-age=31536000; includeSubDomains`
- `X-Content-Type-Options: nosniff`
- `X-Frame-Options: DENY`
- `X-XSS-Protection: 1; mode=block` (legacy)
- `Content-Security-Policy: default-src 'self'`
- `Referrer-Policy: strict-origin-when-cross-origin`

## Secrets and configuration

The API reads `ConnectionStrings__DefaultConnection`, `JWT_SECRET` and `CORS_ALLOWED_ORIGINS` from configuration: user secrets locally, environment variables when deployed. The frontend reads `VITE_API_URL`. Nothing secret is committed, `.env` is not tracked, and the API refuses to start when a required value is missing. The JWT secret in `docker-compose.yml` and `.env.example` is a local development default only.

## Testing

The API tests (xUnit, in-memory database) cover authorization, the audit log, token handling, input validation, CORS and the hardening fixes. They run on every pull request and the checks are required before merging. Not covered yet: rate limiting, behaviour on a real PostgreSQL database, and end to end browser tests in CI.

## Known limitations

- The `Microsoft.OpenApi` advisory (GHSA-v5pm-xwqc-g5wc) is open. The package is only used for the OpenAPI specification, which is not served yet.
- A refresh token that is reused after rotation is rejected, but this does not end the user's other sessions.
- SameSite Strict needs the frontend and the API to share a site when deployed.
- Invitations add the member straight away. The emailed link is not used, and emails are mocked.
- Tokens that existed before they were hashed stopped working when that change was deployed.

### Past incident: committed database credential

A live database connection string, password included, was committed to `api/appsettings.json` on 24 September 2026 and was visible in the public repository from then on. The password worked for 13 days, until the database was deleted on 7 October 2026. The string was removed from the code on 14 October 2026 and scrubbed from the git history on 15 and 16 October 2026. GitHub may still serve the old commits by their exact hash until GitHub Support purges them, but the password no longer works. Full write-up: Decision 29 in `docs/DECISIONS.md`.

## Reporting a vulnerability

Please open a private security advisory on GitHub: https://github.com/CHRIS8530/testify-app/security/advisories

Do not open public issues for security findings.
