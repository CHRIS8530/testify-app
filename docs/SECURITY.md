# Testify Security Documentation

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

## Contact

Security questions or vulnerabilities: innasol.official@gmail.com