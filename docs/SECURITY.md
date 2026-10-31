# Security

## Reporting a vulnerability

Please do not open a public issue. Report it privately through
[GitHub private vulnerability reporting](https://github.com/eremeeveugene/CalorieTracker/security/advisories/new).

## Principles

- The code is public; security comes from secrets management and a hardened deployment, not from hiding the code.
- Secrets never enter git: `dotnet user-secrets` locally, GitHub Actions secrets in CI, environment variables on the server.
- Deny by default: every endpoint requires authentication, every network port is closed, every database user has the least privilege it needs.

## Checklist

### Repository (Phase 0)

- [x] Secret scanning with push protection
- [x] Dependabot alerts and security updates
- [x] Dependabot version updates (`.github/dependabot.yml`)
- [x] CodeQL code scanning
- [x] Private vulnerability reporting
- [x] `main` protected: changes by PR only, CI must pass
- [x] CI uses read-only permissions and `pull_request` (never `pull_request_target`)

### Application (Phase 1 onwards)

- [ ] Authentication required by default (fallback authorization policy); public endpoints opt out explicitly
- [x] Passwords hashed by ASP.NET Core Identity; account lockout after failed logins
- [x] Short-lived JWT access tokens with rotating refresh tokens; reuse of a rotated token revokes all sessions
- [ ] Rate limiting in the gateway: per IP and per user, stricter on login and AI endpoints
- [ ] Daily AI request quota per user
- [ ] Input validation on every command; request body size limits
- [ ] HTTPS only, HSTS, strict CORS, security headers
- [ ] No personal or health data in logs

### Database

- [ ] Not reachable from the internet: private Docker network only
- [ ] One database user per service, with access to its own database only
- [ ] Generated strong passwords stored as environment variables
- [ ] Daily encrypted off-site backups; restore tested

### Infrastructure (Phase 5, deployment)

- [ ] Cloudflare proxy in front of the app (DDoS protection, WAF, hidden origin IP)
- [ ] Cloudflare Tunnel: no inbound ports open on the server
- [ ] SSH with keys only, no password login
- [ ] Automatic OS security updates
- [ ] Budget alerts on every cloud account; no paid auto-scaling

### Monitoring

- [ ] Alerts on spikes of failed logins, 401 and 429 responses
- [ ] Alerts on unusual AI usage

### Privacy

- [ ] Users can export and delete their data (GDPR)
- [ ] Only the minimum data is sent to AI providers; users are told which provider is used
