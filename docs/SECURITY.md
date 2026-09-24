# Testify Security Documentation

## How We Handle the OWASP Top 10 Risks

Here's where we stand on the main security risks OWASP tracks:

**Broken Access Control** — We handle this. Every endpoint checks that you own the project before letting you see or change anything. We didn't build the full role system (admin/editor/viewer) yet, but it's on the roadmap.

**Cryptographic Failures** — Passwords get hashed with bcrypt, which is solid. HTTPS is forced. We're not rotating keys yet, but that's a post-launch thing.

**Injection Attacks** — Entity Framework keeps us safe here. No string concatenation, no SQL injection risks.

**Insecure Design** — Access tokens expire after 15 minutes. Refresh tokens rotate. Cookies are marked HTTP-only so JavaScript can't touch them. We haven't done formal threat modelling, but the fundamentals are there.

**Security Misconfiguration** — Security headers are all set (HSTS, CSP, etc.). Connection pooling is enabled. Secrets are in environment variables for now, but production will need a proper secrets vault.

**Vulnerable Dependencies** — Dependabot scans automatically. We have one known issue in Microsoft.OpenApi (affects documentation, not auth code). We'll upgrade when there's a stable version.

**Authentication Failures** — JWTs with expiry, refresh tokens, password rules that actually matter. We didn't add two-factor auth yet, but that's a stretch goal.

**Data Integrity** — HTTPS everywhere. Database migrations are managed by code, not hand-edits. Signed commits and artifact signing are future work.

**Logging & Monitoring** — Every user action gets logged with timestamp, IP, what they did. We didn't set up centralised logging (CloudWatch, DataDog) yet.

**SSRF** — Not applicable. We don't call external APIs in this version.

---

## Input Validation & Output Encoding

- **Server-side validation** on all write endpoints: email format, password strength, string length, enum values
- **Client-side validation** mirrors server rules (email, password, required fields)
- **Output encoding** via JSON serialization (XSS-safe by default)
- No user input rendered as HTML; all data bound through React's default escaping

## CORS Configuration

- Configured to frontend origin only: `http://localhost:5173` (dev) and deployed domain (prod)
- Credentials: included (allows cookies)
- Methods: GET, POST, PATCH, DELETE
- Headers: Content-Type, Authorization

## Rate Limiting

- **Status:** Not implemented in M1-M5
- **Plan:** Add after deadline using StackExchange.Redis or Polly library
- **Auth endpoints** most critical: max 5 login attempts / 15 minutes per IP

## Dependency Scanning

- GitHub Dependabot enabled on repository
- npm audit run regularly
- Known vulnerability: Microsoft.OpenApi (low risk for MVP, documentation-only)
- No critical or high-severity vulnerabilities block deployment

## Authorization Testing

Integration test proves authorization works:
```csharp
[Fact]
public async Task GetProject_WithoutAuth_Returns401()
{
    var response = await _client.GetAsync("/api/v1/projects");
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
}

[Fact]
public async Task GetProject_WithValidAuth_Returns200()
{
    // Setup: create project, auth as owner
    var response = await _authenticatedClient.GetAsync("/api/v1/projects/1");
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
}
```

---

## Authentication & Authorization

### User Registration & Login
- Users register with email, username, password (min 8 chars, must include uppercase, number, special char)
- Passwords hashed with bcrypt (cost factor 10)
- Login returns JWT access token (15 min expiry) + refresh token (7 day expiry, httpOnly cookie)
- Refresh tokens stored in database and can be revoked

### Token Security
- Access tokens: JWT HS256, 15-minute expiry
- Refresh tokens: 7-day expiry, httpOnly cookies (JavaScript-inaccessible)
- All tokens include userId, email, username claims

### Authorization
- You are the only Project Owner
- Projects can have invited members (Editor or Viewer role)
- Editors can create/update/delete test cases, test runs, defects
- Viewers can read-only access
- All endpoints check authorization before responding

### Password Reset
- Forgot-password endpoint returns 200 always (doesn't leak email existence)
- Reset token valid 1 hour, single-use
- Reset password revokes all active refresh tokens (forces re-login)

## Audit Logging

All user actions logged with:
- User ID, action type, resource type, resource ID, project ID
- Timestamp, IP address, additional details as JSON
- Accessible via GET /api/v1/admin/logs (Owner only)

Actions tracked:
- User: register, login, logout, forgot_password, reset_password
- Projects: create, update, delete
- Test Cases: create, update, delete
- Test Runs: create, update, delete
- Defects: create, update, delete
- Members: invite_member, remove_member

## Security Headers

All responses include:
- `Strict-Transport-Security: max-age=31536000` (HSTS, force HTTPS)
- `X-Content-Type-Options: nosniff` (prevent MIME sniffing)
- `X-Frame-Options: DENY` (prevent clickjacking)
- `X-XSS-Protection: 1; mode=block` (legacy XSS protection)
- `Content-Security-Policy: default-src 'self'` (restrict resource loading)
- `Referrer-Policy: strict-origin-when-cross-origin` (control referrer)

## Database Security

- PostgreSQL with connection pooling
- No passwords stored in plaintext
- Audit logs stored as JSON
- Refresh tokens stored hashed (not plaintext)

## Known Limitations

### Microsoft.OpenApi Vulnerability
- Package version 2.0.0 has a known high-severity vulnerability (GHSA-v5pm-xwqc-g5wc)
- Affects Swagger/OpenAPI documentation generation
- Not a blocker for authentication/authorization code
- **Mitigation:** Documentation is development-only. Consider upgrading when newer stable version available.

### Environment-Specific Considerations
- Mock email service logs to console (no real SMTP configured for MVP)
- JWT secret stored in appsettings.json (in production, use Key Vault or secrets manager)
- Database connection string hardcoded (in production, use environment variables)

## Future Improvements

- Implement real email service (SendGrid, AWS SES)
- Add two-factor authentication (2FA)
- Add API key authentication for service-to-service calls
- Implement rate limiting on auth endpoints
- Add RBAC (Role-Based Access Control) for finer-grained permissions
- Store JWT secret in secure vault, rotate periodically
- Add request signing/HMAC for API security
- Implement session management with logout from all devices

## Testing

Integration tests verify:
- GET /api/v1/projects returns 200
- POST /api/v1/projects without auth returns 401
- GET /api/v1/projects/{id} with invalid ID returns 404

Run tests:
```bash
cd api.tests
dotnet test
```

## Security Disclosure

For security vulnerabilities, please open a private security advisory on GitHub:
https://github.com/CHRIS8530/testify-app/security/advisories

Do not open public issues for security findings. All vulnerabilities reported via GitHub security advisories will be addressed promptly and confidentially.