# Walkthroughs

Interactive teaching pages in TrustCartographer that demonstrate trust-model flows end-to-end. Each walkthrough is a live Razor page (live keys, live signatures, live verification) plus a guide in this folder explaining what the page demonstrates and what concepts to take away.

## Available walkthroughs

| Walkthrough | URL | Guide | Status |
|---|---|---|---|
| Practitioner License Credential | `/practitioner-license` | [practitioner-license.md](practitioner-license.md) | ✅ Live |

## How to learn from these

Each guide is structured the same way:

1. **What it demonstrates** — the actors, what they do
2. **How to use it** — recommended scenario order (start with happy path; tamper toggles teach failure modes)
3. **Concepts you should walk away with** — DID/VC/JWT terminology grounded in what you just saw
4. **Where to look in the code** — pointers if you want to read the implementation
5. **Where this connects to UDAP** — the comparison story this project is ultimately building toward

Read the guide *while* running the page. The intent is that you do a step in the UI, expand the payload, then read what the guide says about it.

## Adding a new walkthrough

The `<SsiSequenceDiagram>` component (in `src/TrustCartographer.UI/Shared/`) is data-driven — pass it a list of actors and a list of steps. To add a new use case:

1. **New page** in `src/TrustCartographer.UI/Pages/` with the same shape as `PractitionerLicense.razor(.cs)` — actor strip at top, controls + diagram + explanation card below.
2. **Compose your scenario** in the code-behind: generate keys/DIDs/VCs as needed, build the `List<SsiStep>` describing each arrow, write the explanation strings.
3. **Add a nav link** under the "Walkthroughs" group in `src/TrustCartographer.UI/Layout/NavMenu.razor`.
4. **Add a guide doc** in this folder mirroring `practitioner-license.md`'s structure.
5. **No host changes needed** — pages in the RCL are picked up automatically by the Web host's `AddAdditionalAssemblies(typeof(TrustCartographer.UI.Routes).Assembly)` call.

Candidate next walkthroughs (rough order of pedagogical value):

- **Patient Consent Receipt** — Subject is also Issuer (Patient issues consent to App); shows holder-as-issuer pattern
- **OID4VCI Issuance Protocol** — same data, but wrapped in the OpenID for VC Issuance wire protocol; pairs nicely with UDAP server metadata for side-by-side
- **`did:web` Resolution** — real HTTPS fetch of `/.well-known/did.json`; introduces publishing and rotation
- **SD-JWT-VC Selective Disclosure** — Holder presents only some claims; SSI's biggest qualitative differentiator over X.509
- **UDAP Equivalent** — same Practitioner License scenario but using UDAP DCR + JWT client auth; side-by-side comparison

## Conventions

- **Self-contained crypto.** Walkthroughs do all their work in TrustCartographer's own services (`DidKeyService`, `VcService`, `CryptoService`). No Sigil dependency, no DB. The point is to see the mechanics, not consume an API.
- **Live, not mocked.** Every signature is real. Every verify call actually runs Ed25519. Hardcoded mock data hides the very thing we're trying to teach.
- **Tamper toggles over happy-only.** A walkthrough that only shows the happy path teaches half. Each walkthrough should support at least one or two failure modes so the user sees *which step* catches what.
- **One guide per walkthrough.** A walkthrough without a guide is undocumented; a guide without a walkthrough is theory.

## Related docs

- [`../sigil-and-trustcartographer.md`](../sigil-and-trustcartographer.md) — why TrustCartographer is the visualizer and Sigil is the backend
- [`../pki-vs-ssi.md`](../pki-vs-ssi.md) — concept-mapping table the walkthroughs ground in concrete flows
- [`../phased-roadmap.md`](../phased-roadmap.md) — the bigger Sigil-side roadmap; walkthroughs map onto its phases
- [`../../specs/README.md`](../../specs/README.md) — the W3C / IETF / OpenID / DIF specs that define the formats used here
