# Security Concept

Sequence diagrams: [diagrams/sequence-login.drawio](diagrams/sequence-login.drawio), [diagrams/sequence-token-refresh.drawio](diagrams/sequence-token-refresh.drawio)

## 1. Authentication — JWT + rotating refresh token

| Item | Decision | Why |
|---|---|---|
| Password storage | ASP.NET Core Identity `PasswordHasher` (PBKDF2) | Proven; never hand-roll hashing |
| Access token | JWT, **15 min**, HS256; claims: `sub`, `email`, `role`, `employee_id`, `sst` (session stamp) | Short life limits damage if stolen |
| Access token storage (browser) | **In memory** (Angular signal), never `localStorage` | Not readable by XSS from storage |
| Refresh token | Random 64 bytes, **7 days**, `HttpOnly; Secure; SameSite=Strict` cookie, path `/api/auth` | JavaScript cannot read it |
| Refresh token in DB | Store **SHA-256 hash** only (`refresh_tokens`, `xmin` concurrency token so two parallel refreshes can't both rotate) | DB leak ≠ valid tokens |
| Rotation | Every refresh issues a new token and revokes the old one | |
| Reuse detection | Revoked token used again → revoke **all** tokens of that user | Detects token theft |
| Brute force | Identity lockout (5 failures → 15 min) + `RateLimiter` on `/auth/login` (5/min/IP) | |
| Error messages | Always generic: "Invalid email or password" | No account enumeration |
| Ending sessions | Terminating the employee, locking the account, changing its role or password gives the account a new Identity security stamp. `sst` is a fingerprint of that stamp, checked on **every request** (`SessionValidator`, cached 30 s per user, evicted on the change): tokens issued before the change get `401` at once, not when they expire. An unlock doesn't revive them | A fired employee or a demoted manager can't keep using an old token for 15 minutes |

On page reload the Angular app calls `POST /api/auth/refresh`; the cookie is sent automatically and a new access token is returned, restoring the session.

## 2. Threats & mitigations

| Threat | Mitigation |
|---|---|
| SQL injection | EF Core LINQ, plus two interpolated raw queries (row lock, sequence) that EF turns into parameters; no string-concatenated SQL |
| XSS | Angular auto-escapes; never use `bypassSecurityTrust*`; CSP header in Nginx |
| CSRF | API uses Bearer header, not cookies; refresh cookie is `SameSite=Strict` |
| Broken access control | Permission policy on every endpoint + **data scope** check in handlers (see [rbac.md](rbac.md)) |
| Mass assignment | Commands are explicit records; entities never bound from requests |
| Stack trace leaks | Global exception handler returns RFC 7807 `ProblemDetails` without internals |
| Secrets in git | `dotnet user-secrets` (dev), `.env` (gitignored) for Docker; commit `.env.example` |
| Transport | HTTPS + HSTS in production; Nginx adds `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy` |
| Sensitive logs | Serilog request logging; never log passwords, tokens, or auth request bodies |
| Accountability | Audit columns (`CreatedBy`/`UpdatedBy`) + `ReviewedById`/`ReviewedAt` on leave approvals |

## 3. Exception → HTTP mapping

| Exception | HTTP |
|---|---|
| `ValidationException` (FluentValidation) | 400 + field errors |
| `UnauthorizedException` | 401 |
| `ForbiddenAccessException` | 403 |
| `NotFoundException` | 404 |
| `DomainException` (business rule broken) | 422 |
| Anything else | 500, generic message, logged with correlation ID |
