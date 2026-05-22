# DIF Presentation Exchange

- **Status:** DIF Recommendation (v2.0)
- **Canonical URL:** https://identity.foundation/presentation-exchange/
- **Fetched:** 2026-05-21
- **Local file:** `presentation-exchange.html`

## Why we care
**The language Verifiers use to formally request specific VCs from Holders.** Conceptual analog to UDAP's scope/claims requests. Foundational input format to OID4VP and to most wallet UX flows.

## Key concepts
- **Presentation Definition** — A JSON doc declaring what the Verifier needs: input descriptors, accepted formats, accepted issuers, required fields
- **Input Descriptor** — One requirement (e.g., "a VC of type DriverLicense from issuer X with field 'dateOfBirth' satisfying age >= 21")
- **Submission Requirements** — Logical combinations (require one of these groups, all of those, etc.)
- **Presentation Submission** — Holder's response mapping descriptors to actual credentials provided
- **JSONPath expressions** — Used to point at claims within VCs

## Sigil relevance
Sigil is primarily Issuer-side, but if it ever needs to verify presentations (e.g., to validate trust-registry membership requests), Presentation Exchange is the request format.

## TrustCartographer relevance
Excellent material to teach. The Presentation Definition format makes the "what does a Verifier actually ask for?" question concrete and showable in a UI.
