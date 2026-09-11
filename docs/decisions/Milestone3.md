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

# M3 Decisions (To be filled as we build)

*Decisions will be added here as authentication, authorization, audit logging, and security features are implemented.*

---

**Last updated:** 2026-09-10
**Status:** M3 in progress. Decomposition complete, implementation starting.
**Next:** Begin 15-step implementation order for authentication and authorization.