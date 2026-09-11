# Testify: Decision Log - Milestone 3

**Milestone:** Days 6-7
**Status:** Registration and login working, authorization enforced and tested, security headers set, SECURITY.md written.

---

## Decision Template

**Context:** What problem was I solving? What was the trigger?

**What I asked the AI:** The substance of the prompt (paraphrased—not a copy-paste, just what I was asking for)

**What it gave me:** Summarised—what did the AI produce? How long was it? What was the shape of the response?

**What I changed and why:** This is the critical part. What did I alter? Why didn't the AI's suggestion work as-is? What did I remove, add, or rewrite?

**What I did not understand at first:** Be honest here. Did you have to read docs to understand it? Did you get an error and have to debug? Did you misread the code? This field is where learning lives.

**Source verification:** Any docs, RFCs, blog posts, or source code I consulted to verify the decision.

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

### Decision 10: JWT Token Implementation - 15 Minute Access Tokens

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

### Decision 11: JWT Token Implementation - 15 Minute Access Tokens

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
RESOLVED - JwtService complete, builds successfully, integrated into AuthController.

---

### Decision 12: Password Hashing with Bcrypt Cost 10

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

### Decision 13: Mock Email Service for MVP

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

### Decision 14: Audit Logging Service for Full Transparency

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

### Decision 15: AuthController - 6 Authentication Endpoints

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

### Decision 16: ProjectMembersController - Invitations and Removal

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

# M3 Decisions (To be filled as we build)

*Decisions will be added here as authentication, authorization, audit logging, and security features are implemented.*

---

**Last updated:** 2026-09-10
**Status:** M3 in progress. Decomposition complete, implementation starting.
**Next:** Begin 15-step implementation order for authentication and authorization.