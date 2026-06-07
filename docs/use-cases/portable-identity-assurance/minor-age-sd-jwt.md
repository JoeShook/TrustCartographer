# Minor Age Check — SD-JWT Selective Disclosure

A guardian's wallet proves to an urgent-care clinic whether a minor is over 18 — revealing **nothing
else**: no name, no date of birth. This is the foundational credential mechanic for the whole
[Portable Identity Assurance](README.md) program and the single biggest qualitative differentiator of
Verifiable Credentials over X.509 / SMART Health Cards.

**Live at:** `/pia/minor-age-check`. Every signature, digest, and verification is computed live — no mock data.

> Source context: the architecture this implements is from Templeton/Cambia (DRAFT v0.9) —
> <https://docs.google.com/document/d/11P-XSaal9cgpXTzp8W5Jc50ppZoeN7PF/edit>. Our own analysis only.

## What it demonstrates

| Actor | Role | Real-world analog |
|---|---|---|
| **Health Identity Issuer** | Issuer | Payer / state that proofs the guardian and issues the minor's credential |
| **Guardian Wallet** | Holder | Holds the minor's credential; chooses what to reveal; proves possession |
| **Urgent Care** | Verifier | Relying party that only needs "is the patient over 18?" |

The whole point: the issuer commits to **every** claim, but the holder discloses only the **subset**
the verifier asked for, and the verifier can still prove those disclosed claims are authentic and
unmodified — without ever seeing the withheld ones.

## How to use it for learning

Work the scenarios in order; each toggles a different break and shows which check catches it.

### 1. Happy path
Click **Start**, then **Next** through all six steps. The two artifacts to open:
- **Step 2 (Deliver credential):** the `SdJwtViewer` shows the issuer-signed JWT and **all** disclosures
  the wallet holds. Note the payload contains only `_sd` digests + `cnf` — never the claim values.
- **Step 5 (Present):** the viewer now marks `age_over_18` **🔓 revealed** and name/DOB/age_over_13
  **🔒 withheld**, and shows the **Key Binding JWT**. The withheld values are simply *absent* from the
  presentation bytes.
- **Step 6 (Verify):** every check is ✓ and the result bar reads **PRESENTATION VERIFIED**. Disclosed:
  `age_over_18 = false`. Withheld: 3 claims. (Verification success ≠ "over 18" — it means the wallet
  proved the issuer-attested answer, which here is *false*.)

### 2. Reveal everything
The wallet discloses **all** claims even though only `age_over_18` was requested. It still verifies —
but an **over-disclosure** banner appears: the clinic now knows the minor's name and DOB. *Selective
disclosure is a policy the holder must actually exercise; the format only makes it possible.*

### 3. Tampered disclosure
A character in the presented `age_over_18` disclosure is flipped in transit. Verification fails at
**digests**: the disclosure's SHA-256 no longer matches any digest committed in `_sd`.

### 4. Replayed nonce
The wallet presents with a stale nonce. Issuer signature and disclosures are fine, but the **nonce**
check fails — the Key Binding JWT is bound to a challenge the verifier didn't issue.

### 5. No key binding
The wallet sends the SD-JWT with no Key Binding JWT. The disclosures verify, but **key binding** fails:
there's no proof the sender controls the holder key the issuer bound via `cnf` — a stolen credential
could be replayed.

## Concepts you should walk away with

- **SD-JWT** — a JWT plus a set of **disclosures**, separated by `~`. The signed payload commits to
  salted **digests** of each selectively-disclosable claim in the `_sd` array; the values live only in
  the disclosures, which travel alongside and can be dropped individually.
- **Disclosure** — `base64url(JSON [salt, claimName, claimValue])`. The random **salt** makes a
  withheld claim's digest unguessable (no dictionary attack on, say, `["…","age_over_18",true]`).
- **`_sd` / `_sd_alg`** — the array of committed digests and the hash name (`sha-256`).
- **`cnf`** — the holder's public key (as a JWK), bound by the issuer so only that holder can present.
- **Key Binding JWT (`kb+jwt`)** — signed by the holder at presentation; carries `aud` (which verifier),
  `nonce` (anti-replay), and `sd_hash` (a hash over exactly the disclosures being presented). Proves
  possession and pins the presentation to one verifier + one challenge.
- **`vct`** — the verifiable credential type (the SD-JWT VC analog of a VC `type`).
- **Holder binding** — the verifier confirms the presenter (KB-JWT signer) is the key in `cnf`.

## Wire-format dissection

A presented SD-JWT looks like this (`~`-delimited):

```
<issuer-signed JWT> ~ <disclosure: age_over_18> ~ <Key Binding JWT>
└───── 3 segments ──┘  └ base64url[salt,name,val] ┘  └─ 3 segments, signed by holder ─┘
```

- **Issuer JWT** — `base64url(header).base64url(payload).base64url(sig)`.
  - header: `{ alg: EdDSA, kid: <issuer did:key vm>, typ: vc+sd-jwt }`
  - payload: `{ iss, iat, vct, _sd_alg: "sha-256", _sd: [<digest>…], cnf: { jwk: {…holder key…} }, exp }`
- **Disclosures** (zero or more, only the revealed ones at presentation): each is
  `base64url(["<salt>","<claimName>",<value>])`. Its digest = `base64url(SHA256(ascii(disclosure)))`,
  which must appear in `_sd`.
- **Key Binding JWT** (last segment, present only at presentation):
  - header: `{ alg: EdDSA, typ: kb+jwt }`
  - payload: `{ iat, aud, nonce, sd_hash }` where `sd_hash = base64url(SHA256(ascii(everything up to and including the last ~ before the KB-JWT)))`.

Issuance form ends with a trailing `~` and no KB-JWT: `<jwt>~<d1>~<d2>~…~`.

## Code map

| UI step | Core call | File |
|---|---|---|
| Mint SD-JWT VC | `SdJwtService.IssueSdJwtVc(...)` | `src/TrustCartographer.Core/SdJwt/SdJwtService.cs` |
| Wallet selects + presents | `SdJwtService.Present(issuerJwt, chosen, holderKey, aud, nonce)` | same |
| Verifier verifies | `SdJwtService.Verify(presentation, issuerPubKey, aud, nonce, required)` | same |
| Models / result | `IssuedSdJwt`, `Disclosure`, `SdJwtVerifyResult` | `src/TrustCartographer.Core/SdJwt/SdJwtModels.cs` |
| Constants | `vc+sd-jwt`, `kb+jwt`, `sha-256`, `~` | `src/TrustCartographer.Core/SdJwt/SdJwtConstants.cs` |
| Ed25519 sign/verify, SHA-256 | `CryptoService` | `src/TrustCartographer.Core/Services/CryptoService.cs` |
| base64url, holder JWK | `Base64Url`, `Jwk.FromEd25519PublicKey` | `src/TrustCartographer.Core/Oid4Vci/` |
| issuer/holder DIDs | `DidKeyService` | `src/TrustCartographer.Core/Services/DidKeyService.cs` |
| Walkthrough page | scenario build + tamper toggles | `src/TrustCartographer.UI/Pages/Pia/MinorAgeCheck.razor(.cs)` |
| Byte viewer | revealed vs withheld + KB-JWT | `src/TrustCartographer.UI/Shared/SdJwtViewer.razor(.cs)` |

Specs: IETF **SD-JWT** (Selective Disclosure for JWTs) and **SD-JWT VC**; OpenID **OID4VP** for the
presentation exchange; W3C **VC Data Model** for the credential semantics.

## Where this connects to UDAP (the comparison story)

| SD-JWT VC (this walkthrough) | UDAP / X.509 equivalent |
|---|---|
| Issuer commits salted digests; holder reveals a subset | An X.509 cert (or UDAP Certification) exposes **all** of its SAN/DN attributes — there is no "reveal only age_over_18" |
| Holder proves possession via a Key Binding JWT (`cnf` + `nonce` + `aud`) | Holder proves possession via UDAP **JWT client auth** signed by the client cert's key |
| Verifier needs only the issuer's public key (from `did:key` here) | Verifier walks the X.509 chain to a Trust Community anchor + checks revocation |

**Where SSI/SD-JWT is stronger (what this proves):** data minimization — the verifier learns exactly
one claim, with cryptographic authenticity, and *cannot* see the rest. UDAP has no equivalent of
per-attribute selective disclosure.

**Where UDAP remains stronger (to surface later):** purpose-of-use propagation (Authorization Extension
Object), first-class third-party Certifications & Endorsements, and a single TLS+OAuth transport every
IAM team already runs. Those comparisons land in the governance (Tier 4) workflow.

## Limitations and what comes next

- **No assurance level yet.** This workflow is the *format* foundation. FAST IDIAL / delegation
  ceiling + discount arrive in the delegated-proofing workflow and get carried as claims here.
- **No real wire capture yet.** The crypto is real and in-process; the OID4VCI/OID4VP HTTP path +
  Live Flow capture for SD-JWT is the next increment (issuer issues `vc+sd-jwt`, holder presents, verifier verifies over the proxy).
- **No trust list.** The verifier accepts any issuer `did:key` (proves *who* signed, not that they're
  authoritative) — closed by the Tier 4 governance workflow, the UDAP-comparison pivot.
- **`cnf.jwk` key binding only.** A real deployment might also support `did`-based holder binding.
- **One disclosure scheme.** Nested/array `_sd` and decoy digests are out of scope for the first cut.

## SDK notes

`SdJwtService` is deliberately a pure, dependency-light primitive (Ed25519 + SHA-256 + base64url +
JSON; no UI/transport). It is the kind of building block a future C# VC/VP SDK would expose directly:

- `IssueSdJwtVc(issuerDid, vmId, issuerPrivateKey, vct, selectivelyDisclosableClaims, holderCnfJwk, alwaysVisibleClaims?, validity?)` → `IssuedSdJwt`
- `Present(issuerJwt, selectedDisclosures, holderPrivateKey, audience, nonce)` → presentation string
- `Verify(presentation, issuerPublicKey, expectedAudience, expectedNonce, requiredClaims?)` → `SdJwtVerifyResult` (each sub-check reported)

Round-trip and negative behavior are exercised by the walkthrough's scenarios (happy / tampered /
replayed / no-KB / over-disclosure). Promoting this to a package would mean: pluggable hash + signature
suites, resolver-based issuer/holder key lookup, and a typed claims model.
