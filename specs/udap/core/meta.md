# UDAP Core Specifications

- **Status:** UDAP.org published specs
- **Canonical URL:** https://www.udap.org/
- **Source:** Copied from `udap-tools/udap-dotnet/docs/specifications/UDAP.org` on 2026-05-21

## Contents

| File | Purpose |
|------|---------|
| `udap-server-metadata.md` | `.well-known/udap` discovery endpoint — analog to OAuth's well-known metadata, but for UDAP communities |
| `udap-dynamic-client-registration.md` | DCR using software statements signed with X.509 certs (no admin pre-registration) |
| `udap-jwt-client-auth.md` | Client authentication via signed JWT (signed with the client's UDAP cert key) |
| `udap-tls-client-auth.md` | Alternative: mTLS-based client authentication |
| `udap-client-authorization-grants.md` | Authorization grant types — including the **Authorization Extension Object** that carries purpose-of-use |
| `udap-certifications-and-endorsements.md` | Third-party assertions attached to the trust chain |
| `udap-user-auth.md` | User authentication flow within UDAP |

## Why these matter for the comparison goal

UDAP gets compared to SSI/A2A through three lenses where it shines:

1. **Authorization Extension Object** (`udap-client-authorization-grants.md`) — propagates purpose-of-use *through* the trust chain. Neither SSI's VPs nor A2A's identity tokens carry this kind of structured purpose statement out of the box.

2. **Certifications & Endorsements** (`udap-certifications-and-endorsements.md`) — third-party assertions about an entity, cryptographically attached. Maps closely to VCs about an Issuer, but operates inside the X.509 trust chain rather than via separate credentials.

3. **Trust Communities** (cross-cutting across all specs) — explicit governance domains. Maps to a "trust registry per TrustDomain" in the SSI world, but more formalized.

## Profiles
See `../profiles/` for vertical-specific profiles (currently SSRAA v2.0.0).
