# Portable Identity Assurance in Healthcare

A runnable, educational build of the architecture proposed in **"Portable Identity Assurance in
Healthcare: A Framework for Minor Identity and a Foundation for Federated Trust."**

> **Source:** Max Templeton, Cambia Health Solutions — DRAFT v0.9 —
> <https://docs.google.com/document/d/11P-XSaal9cgpXTzp8W5Jc50ppZoeN7PF/edit>
>
> The guides in this folder are **our own analysis and re-expression**; we do not reproduce the
> paper's text. Read the source for the authoritative proposal.

**Live hub:** `/pia` when running the solution (see [How to run](../../claude/reference_running_the_app.md)).

## Why this exists

The paper's stack — **SD-JWT Verifiable Credentials issued/presented via OID4VC**, proofed under
**NIST 800-63A rev4 delegated proofing**, assurance expressed via **FAST IDIAL**, governed by a
**TEFCA-aligned trust framework** — is exactly what TrustCartographer demonstrates. Building the
paper's workflows turns an architecture document into something you can step through, inspect on the
wire, and reason about — and it produces SDK-grade Core primitives toward a future C# VC/VP SDK.

## The four-tier model (our summary)

1. **Tier 1 — Delegate identity verification.** A responsible adult is proofed → an assurance level.
2. **Tier 2 — Delegated proofing & issuance.** The adult vouches for the minor; an SD-JWT VC is issued
   via OID4VCI carrying the (discounted) assurance level.
3. **Tier 3 — Presentation & relying-party decision.** The wallet presents only the needed claims via
   OID4VP; the verifier weighs assurance against transaction risk.
4. **Tier 4 — Governance / trust framework.** Who is authoritative for what — the trust list the
   verifier consults.

Two **foundations** cut across every tier and are built first: **SD-JWT selective disclosure** (the
credential format) and **delegated proofing + assurance levels** (the trust basis). The issuance
pathways, custody lifecycle, verification, and governance layer on top.

## Workflows

| Workflow | Tier | Guide | Status |
|---|---|---|---|
| Minor Age Check — SD-JWT selective disclosure | Foundation / Tier 3 format | [minor-age-sd-jwt.md](minor-age-sd-jwt.md) | ✅ Live (`/pia/minor-age-check`) |
| Delegated proofing + assurance levels | Tier 1–2 | [pathways-and-custody.md](pathways-and-custody.md) | ✅ Live (`/identity-csp`) |
| Issuance Pathway A — payer enrollment | Tier 1–2 | [pathways-and-custody.md](pathways-and-custody.md) | ✅ Live (`/payer-issuer`) |
| Issuance Pathway B — provider in-person | Tier 1–2 | [pathways-and-custody.md](pathways-and-custody.md) | ✅ Live (`/provider-issuer`) |
| Issuance Pathway C — IAL2 CSP (Login.gov) | Tier 1–2 | [pathways-and-custody.md](pathways-and-custody.md) | ✅ Live (`/csp-issuer`) |
| Guardian → minor custody transfer | Custody | [pathways-and-custody.md](pathways-and-custody.md) | ✅ Live (`/holder` → `/holder-minor`) |
| Full flow animated (CSP-backed) | Tier 1–3 | [pathways-and-custody.md](pathways-and-custody.md) | ✅ Live (`/live-flow`) |
| Verification & trust decision → OIDC token | Tier 3 | — | Planned |
| Governance / issuer trust list | Tier 4 | — | Planned |

## How to learn from these

Each guide mirrors the [`practitioner-license`](../../walkthroughs/practitioner-license.md) pattern and
adds depth for SDK work:

1. **What it demonstrates** — actors and what they do.
2. **How to use it** — scenario order (happy path first; tamper toggles teach failure modes).
3. **Concepts** — the SD-JWT / OID4VC terminology grounded in what you just saw.
4. **Wire-format dissection** — the actual byte structure on the wire.
5. **Code map** — UI step → Core method → spec section, so you can re-enter the code cold.
6. **UDAP comparison** — the comparison story this project builds toward.
7. **SDK notes** — how the Core primitive is shaped for reuse in a future SDK.

## Related

- [`../../walkthroughs/practitioner-license.md`](../../walkthroughs/practitioner-license.md) — the JWT-VC trust triangle (start here if VC/VP is new).
- [`../../pki-vs-ssi.md`](../../pki-vs-ssi.md) — the concept-mapping table.
- [`../../../specs/README.md`](../../../specs/README.md) — the W3C / IETF / OpenID specs.
