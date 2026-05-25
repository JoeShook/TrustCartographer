# Walkthrough: Practitioner License Credential

A guided demonstration of the full SSI loop — DID minting, VC issuance, presentation, and verification — using the canonical "doctor proves licensure to hospital" scenario.

**Live at:** `/practitioner-license` when running `TrustCartographer.Web`.

## What it demonstrates

The Verifiable Credentials trust triangle in its simplest possible form:

| Actor | Role | Real-world analog |
|---|---|---|
| **State Medical Board** | Issuer | Authoritative source that asserts MD licensure |
| **Dr. Smith** | Holder | The practitioner whose license is being attested |
| **Hospital EHR** | Verifier | The relying party that needs to trust the assertion before granting access |

Every step is computed live in the browser (server-side Blazor) — no Sigil running, no database. Each `did:key` is freshly minted on **Reset**, the VC is freshly signed, and verification runs the actual Ed25519 check. There's no mock data.

## How to use it for learning

Open the page, then **work through these scenarios in order** — each one teaches a different concept by showing what success and failure look like.

### 1. Happy path (start here)

1. Click **Start** to reveal step 1, then **Next step** repeatedly. Read the explanation card at the bottom after each step.
2. After each arrow appears, click the 📨 icon on it to expand the payload inline. You'll see:
   - **Step 1 / 2 payloads**: the synthesized DID Documents (notice there's no chain, no CA — just an `id`, a `verificationMethod`, and purpose arrays)
   - **Step 3 payload**: the VC body *before* signing — pure JSON
   - **Step 4 payload**: the JWT-VC as decoded by `JwtViewer` — header / payload / signature visible side by side
   - **Step 6 payload**: what the Holder actually presents (same JWT in the happy path)
   - **Step 8 payload**: the structured verify result
3. The final result bar should be **✅ VERIFIED**.

What this teaches:
- A DID is just a string that encodes a public key — no infrastructure needed for `did:key`
- A VC is just JSON wrapped in a signed envelope
- Verification is just: re-hash the signing input, check the signature against the issuer's public key
- The Holder is in the middle — Issuer never talks to Verifier directly

### 2. Tampered signature

Switch the radio to **Tampered signature** and replay. The credential delivered in step 4 is correctly signed, but on the way from Holder to Verifier (step 6) the **first character of the signature segment is flipped**, simulating any kind of in-flight or storage corruption.

What this teaches:
- Signature verification is **byte-exact** — change one character of base64url and Ed25519 verify fails
- The `kid` lookup still works fine (header is intact); it's the signature check that fails at step 8
- This is the closest analog to what UDAP's mTLS or JWT-bearer client auth protects against — any tampering of the signed payload trips the check

### 3. Unknown issuer key

Switch to **Unknown issuer key** and replay. The VC is correctly signed by the real State Medical Board, but the simulated Verifier **resolves the issuer DID to a different public key** — as if the Verifier doesn't trust the actual issuer, or is talking to an attacker who presents a forged DID Document.

What this teaches:
- Trust in SSI doesn't come from the cryptographic chain (there isn't one) — it comes from the **Verifier knowing the right public key for the issuer's DID**
- For `did:key` this is trivial (the DID IS the key — you can't be "wrong" about the key unless you're talking to the wrong DID)
- For `did:web` and others this matters a LOT: the Verifier might cache, mis-resolve, or be deceived
- This is conceptually equivalent to a UDAP Verifier trusting the wrong Trust Community anchor

### 4. Expired credential

Switch to **Expired credential** and replay. The Issuer signs the VC with `validUntil` set to one hour in the past. Verification then has to decide whether the signature being valid is enough.

What this teaches:
- A VC can be cryptographically valid but **temporally invalid** — sig check passes, `exp` check fails
- The Verifier owns the policy decision about whether to honor `exp`
- This is the entry point to understanding **status lists** — `exp` is the simple time-based version; `BitstringStatusList` (deferred to Sigil Phase E) is the revocation-list version

## Concepts you should walk away with

### DID (Decentralized Identifier)

A string of the form `did:<method>:<method-specific-id>`. There's no central registry. Each *method* has its own resolution rules.

- **`did:key`** (this walkthrough): the identifier literally encodes a public key. No network, no infrastructure. Resolution is pure arithmetic. Limitation: no key rotation — to "rotate" you mint a new DID.
- **`did:web`** (Sigil's Phase B): the identifier names an HTTPS endpoint that hosts the DID Document. You can rotate by republishing.
- **Other methods** (did:jwk, did:ion, did:ebsi, ...): different storage / discovery mechanisms, same DID Document shape.

A **DID Document** is the JSON object you get when you "resolve" a DID. It contains:
- `id`: the DID itself
- `verificationMethod`: array of public keys, each with its own id (a fragment under the DID, e.g. `did:key:z6Mk...#z6Mk...`)
- Purpose arrays: `authentication`, `assertionMethod`, `keyAgreement`, `capabilityInvocation`, `capabilityDelegation` — each lists which verification method ids are authorized for that purpose

In the walkthrough: the Issuer's DID Document has its key in both `assertionMethod` (used to sign VCs) and `authentication` (could be used for login). The Holder's is in `authentication` only — she's not currently issuing anything.

### VC (Verifiable Credential)

A signed JSON document where an Issuer makes claims about a Subject. W3C VC Data Model 2.0 defines the shape:

```json
{
  "@context": ["https://www.w3.org/ns/credentials/v2"],
  "type": ["VerifiableCredential", "PractitionerLicenseCredential"],
  "id": "urn:uuid:...",
  "issuer": "did:key:z6Mk...",
  "validFrom": "2026-05-24T...",
  "validUntil": "2028-05-23T...",
  "credentialSubject": {
    "id": "did:key:z6Mk...",  // subject DID
    "practitionerName": "Dr. Jane Smith",
    "licenseNumber": "MD-2026-04891",
    "specialty": "Internal Medicine",
    "jurisdiction": "WA"
  }
}
```

The data model is **format-agnostic**. The walkthrough uses **JWT-VC** (W3C `vc-jose-cose` securing format) because it's the simplest and reuses existing JWT tooling. The other major option is **Data Integrity** (JSON-LD with embedded proofs), which supports selective disclosure with BBS+ but requires JSON-LD canonicalization.

### JWT-VC

A JWT whose payload contains the VC. Three base64url-encoded segments separated by dots: **header.payload.signature**.

- **Header**: `alg: EdDSA`, `kid: <issuer DID + verification method fragment>`, `typ: vc+jwt`
- **Payload**: standard JWT claims (`iss`, `sub`, `iat`, `exp`, `jti`) plus a `vc` claim containing the full VC body
- **Signature**: Ed25519 signature over the ASCII bytes of `header.payload`

Click 📨 on step 4 in the walkthrough to see `JwtViewer` decode this in place. Note how the `kid` in the header tells the Verifier exactly which key in which DID Document to look up.

### Multicodec + Multibase (the did:key encoding)

For `did:key`, the DID is constructed as:

```
did:key: + "z" + base58btc( multicodec_prefix || raw_public_key_bytes )
                              ↑                    ↑
                              Ed25519 = 0xED 0x01  32 bytes for Ed25519
```

- **Multibase** is a tiny standard: the first character of the encoded string identifies the encoding (`z` = base58btc, `m` = base64, etc.)
- **Multicodec** is a 1–2 byte prefix identifying the key algorithm
- Together they let `did:key` describe any key type without ambiguity

Code: `TrustCartographer.Core/Services/DidKeyService.cs` — small enough to read end to end.

### EdDSA (Ed25519 signatures)

Used everywhere in modern SSI. Key properties:
- 32-byte public keys, 32-byte private keys, 64-byte signatures — small
- Deterministic (no random nonce needed at signing time — important for replayable verification)
- Hashes internally (the `hashAlgorithm` you'd pass to a generic signing API is ignored)
- Fast verify, decent sign

Implementation in walkthrough: BouncyCastle's `Ed25519Signer` (see `TrustCartographer.Core/Services/CryptoService.cs`).

### Trust Triangle

Issuer → Holder → Verifier. Three observations make SSI distinct from PKI:
1. **No issuer issues the DID itself.** The Issuer's DID is self-asserted. Trust in the Issuer comes from the Verifier knowing/recognizing the DID, not from a signing chain.
2. **Verifier never talks to Issuer at verification time.** Privacy-by-design — Issuer doesn't see who you show your VC to.
3. **Status is opt-in.** `exp` is the simple version; status lists are the formal version. PKI assumes CRL/OCSP exist; SSI lets Issuers skip status entirely if they want.

## Where to look in the code

| File | What it does |
|---|---|
| `src/TrustCartographer.Core/Services/CryptoService.cs` | Ed25519 key gen, sign, verify (BouncyCastle) |
| `src/TrustCartographer.Core/Services/DidKeyService.cs` | `did:key` encode/decode, DID Document synthesis |
| `src/TrustCartographer.Core/Services/VcService.cs` | JWT-VC build + verify, manual base64url + JSON |
| `src/TrustCartographer.UI/Pages/PractitionerLicense.razor` | The walkthrough page (markup) |
| `src/TrustCartographer.UI/Pages/PractitionerLicense.razor.cs` | The walkthrough page (orchestration: builds steps, drives state, applies tamper modes) |
| `src/TrustCartographer.UI/Shared/SsiSequenceDiagram.razor*` | Reusable data-driven diagram component |
| `src/TrustCartographer.UI/Shared/JwtViewer.razor*` | JWT decoder/pretty-printer with epoch annotations |

If you want to read the underlying specs, see `specs/w3c/did-core-1.0/`, `specs/w3c/vc-data-model-2.0/`, `specs/w3c/vc-jose-cose/`, `specs/did-methods/did-key/`.

## Where this connects to UDAP (the comparison story)

The whole point of TrustCartographer is to eventually argue for UDAP's strengths. Map this walkthrough to the UDAP equivalent and the differences become concrete:

| SSI version (this walkthrough) | UDAP equivalent |
|---|---|
| State Medical Board mints a `did:key` | State Medical Board is a CA in a Trust Community with an X.509 root |
| Board issues a JWT-VC about Dr. Smith's license | Board issues an X.509 cert with the licensure attributes in the SAN/SubjectDN, OR a separate Certification & Endorsement JWT |
| Dr. Smith holds the VC in her wallet | Dr. Smith's app holds a UDAP client cert + its key |
| Dr. Smith presents the VC to the Hospital | Dr. Smith's app authenticates via UDAP JWT client auth (signed with her cert) |
| Hospital resolves issuer DID, verifies signature | Hospital walks the X.509 chain to the Trust Community anchor, checks CRL/OCSP |
| Hospital decides whether to honor `exp` | Hospital decides whether to trust the cert (notBefore/notAfter + revocation status) |

**Where UDAP is stronger** (and what the eventual side-by-side will show):
- UDAP can carry a structured **Authorization Extension Object** with purpose-of-use that propagates through the trust chain. SSI's VCs don't have a standard for purpose propagation through a multi-hop flow.
- UDAP's **Certifications & Endorsements** are first-class third-party assertions. SSI does this with "VCs about an Issuer" but the discovery story is weaker.
- UDAP rides on one transport (TLS + OAuth) every IAM team already knows; SSI's transport story is split between OID4VC and DIDComm.

**Where SSI is stronger**:
- Selective disclosure (SD-JWT, BBS+) — a UDAP cert reveals all its attributes; an SSI credential can reveal only "subject is over 21" without revealing DOB.
- Holder-mediated privacy — Issuer doesn't see who you present to.
- Identifier rotation / multi-key per identifier is first-class.

These are the comparisons the eventual side-by-side walkthroughs will make concrete.

## Adding more walkthroughs

The `<SsiSequenceDiagram>` component is data-driven. To add another use case (Patient Consent, TEFCA Participant, OID4VCI issuance flow, etc.):

1. New page under `Components/Pages/` (e.g. `PatientConsent.razor` + `.razor.cs`)
2. Define the actor list and step list — same `SsiActor` and `SsiStep` types
3. Compose your own scenario logic (which crypto runs, what tampers to support)
4. Add a nav link under the "Walkthroughs" group in `Components/Layout/NavMenu.razor`

No changes to the diagram component or the services should be needed. If you find yourself needing diagram features (more arrow styles, branching, multiple parallel flows), that's a signal to enhance the component rather than fork it.

## Limitations and what comes next

- **Subject DID isn't used to authenticate the Holder.** A real flow would have the Holder sign a Verifiable Presentation with her own DID's key. Skipped here for clarity; could add as an optional final arrow.
- **No Presentation Definition.** A real OID4VP flow would have the Verifier send a structured request (which credential type, which claims required). Here the request is just a label.
- **No status list.** Revocation is binary via `exp` only. Add a Bitstring Status List walkthrough when ready.
- **No actual `did:web` resolution.** The Verifier "resolves" by holding a reference to the expected key. A `did:web` walkthrough would add a real HTTPS GET and an `https.json` artifact.
- **JWT-VC only.** SD-JWT-VC and JSON-LD Data Integrity each deserve their own walkthrough.
