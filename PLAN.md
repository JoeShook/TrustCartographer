# TrustCartographer

An interactive Blazor application for exploring and comparing trust models across decentralized identity, blockchain, and agent authentication paradigms.

## Motivation

Build hands-on understanding of blockchain technology and self-sovereign identity (SSI) patterns by visualizing the mechanics — similar to the approach used with UDAP.org SDKs and reference implementations. The end goal is to frame a comparative understanding across SSI, Google A2A, and UDAP, ultimately building the argument for UDAP's strengths in agent-to-agent authentication, particularly around:

- **Authorization Extension Object** — purpose-of-use propagation from initiator (user or B2B) through the trust chain
- **Certifications & Endorsements** — cross-cutting third-party trust assertions
- **Trust Communities** — governance frameworks for organizational trust

## Core Concepts to Learn

- **Decentralized Identifiers (DIDs)** — W3C standard for self-owned identifiers (no central registry needed)
- **Verifiable Credentials (VCs)** — W3C standard for tamper-proof, cryptographically signed claims
- **DID Methods** — How DIDs resolve (e.g., `did:key`, `did:web`, `did:ion`, `did:ethr`)
- **DIDComm** — Messaging protocol between DID-based agents
- **Blockchain basics** — Hashing, Merkle trees, consensus, smart contracts (as they relate to anchoring DIDs)

## Key .NET / Open-Source Libraries

| Library | Purpose |
|---|---|
| **Hyperledger Aries Framework .NET** (`aries-framework-dotnet`) | Agent framework for SSI — credential issuance, verification, DIDComm |
| **DID.NET** / **SimpleIdServer DID** | DID creation, resolution, and management in .NET |
| **NBitcoin** | .NET Bitcoin library — great for learning crypto primitives (keys, signing, hashing) |
| **Nethereum** | .NET Ethereum library — interact with smart contracts, `did:ethr` |
| **Bouncy Castle (Portable.BouncyCastle)** | Crypto primitives (Ed25519, secp256k1, etc.) |
| **Microsoft.IdentityModel.Tokens** | JWT/JWS handling (VCs are often JWT-encoded) |

## Blazor App Architecture

```
TrustCartographer/
├── src/
│   ├── TrustCartographer.Core/            # Shared logic, models, services
│   │   ├── Models/
│   │   │   ├── KeyPairInfo.cs
│   │   │   ├── SignatureResult.cs
│   │   │   ├── Block.cs                   # (planned)
│   │   │   ├── DIDDocument.cs             # (planned)
│   │   │   └── VerifiableCredential.cs    # (planned)
│   │   └── Services/
│   │       ├── CryptoService.cs
│   │       ├── DIDService.cs              # (planned)
│   │       ├── CredentialService.cs        # (planned)
│   │       └── BlockchainService.cs        # (planned)
│   └── TrustCartographer.Web/             # Blazor Server interactive app
│       └── Components/Pages/
│           ├── Home.razor                  # Landing page with module cards
│           ├── KeyPairExplorer.razor        # Key gen, sign, verify, hash
│           ├── DIDExplorer.razor            # (planned) Create/resolve DIDs
│           ├── CredentialWorkshop.razor     # (planned) Issue, hold, verify VCs
│           ├── BlockchainSimulator.razor    # (planned) Chain visualization
│           └── TrustTriangle.razor         # (planned) Issuer -> Holder -> Verifier flow
```

## Phased Learning Path

### Phase 1 — Crypto Fundamentals (Blazor pages) ✅ STARTED
- [x] Generate key pairs (Ed25519 / secp256k1)
- [x] Visualize keys in multiple encodings (hex, base64url)
- [x] Sign and verify messages (Ed25519)
- [x] SHA-256 hashing
- [ ] Hash chain visualizer (the core of any blockchain)

### Phase 2 — DIDs
- [ ] Implement `did:key` (simplest method — the DID *is* the public key)
- [ ] Display the DID Document JSON structure
- [ ] Add `did:web` resolution (fetches a DID Document from a URL)
- [ ] Side-by-side comparison of DID methods

### Phase 3 — Verifiable Credentials
- [ ] Issue a VC (JWT-based is simplest to start)
- [ ] Display credential structure — claims, proof, issuer
- [ ] Build a verifier page that checks the signature
- [ ] Explore credential formats (JWT vs JSON-LD)

### Phase 4 — Trust Triangle Visualization
- [ ] Interactive flow: Issuer issues -> Holder stores -> Verifier requests & verifies
- [ ] Animate the credential exchange with DIDComm-like messaging
- [ ] Show trust chain provenance at each step

### Phase 5 — Blockchain Anchoring & Comparative Views
- [ ] Simple in-memory blockchain to anchor DID operations
- [ ] Or connect to a testnet (Ethereum Sepolia via Nethereum) for real anchoring
- [ ] Side-by-side comparison page:
  - **DIDComm-based agent auth** — peer DIDs, signed messages, no central authority
  - **A2A-style agent auth** — Agent Cards, OAuth token exchange, centralized IdP
  - **UDAP-style agent auth** — Authorization Extension Objects, certifications, trust communities
- [ ] Visualize the trust chain in each — where does trust originate?

## Comparative Trust Model Reference

### Where Google A2A Sits

| Layer | SSI / Decentralized | Google A2A | UDAP |
|---|---|---|---|
| **Discovery** | DID Resolution -> DID Document | Agent Card (`/.well-known/agent.json`) | UDAP Metadata (`/.well-known/udap`) |
| **Authentication** | DIDs + cryptographic proofs | OAuth 2.0, OpenID Connect, API keys | X.509 + dynamic client registration |
| **Communication** | DIDComm messaging | HTTP + JSON-RPC, SSE | FHIR + signed metadata (JWT) |
| **Trust** | VCs, trust frameworks | Delegated via OAuth scopes, enterprise IAM | Certifications & Endorsements, Trust Communities |
| **Identity anchor** | Blockchain / decentralized ledger | Centralized identity providers | X.509 certificate chains + trust communities |
| **Purpose of use** | VC claims (limited) | OAuth scopes (limited) | Authorization Extension Object (rich, propagated) |

### UDAP Mapping to SSI Concepts

| UDAP Concept | SSI/Blockchain Equivalent | What to Compare |
|---|---|---|
| Authorization Extension Object | Verifiable Presentations + credential scopes | How purpose-of-use propagates through a trust chain |
| Certifications & Endorsements | Verifiable Credentials from trusted issuers | Third-party attestation models |
| Trust Communities | Trust Frameworks / Governance Frameworks (ToIP Layer 4) | Who decides who's trusted and how |
| Signed metadata (JWT) | DID Documents + cryptographic proofs | Machine-readable trust anchors |
| X.509 + UDAP dynamic registration | DIDs + DIDComm connection protocol | How agents establish relationships |

## Resources

- **W3C DID Core spec** — the definitive reference for DIDs
- **W3C Verifiable Credentials Data Model** — the VC spec
- **Decentralized Identity Foundation (DIF)** — protocols and working groups
- **Hyperledger Aries RFCs** — practical agent/credential exchange protocols
- **ToIP (Trust over IP) Foundation** — governance and trust framework layers

## Repository

- GitHub: github.com/joeshook/TrustCartographer
- Tech Stack: .NET 10, Blazor Server (Interactive), Portable.BouncyCastle, System.IdentityModel.Tokens.Jwt, SimpleBase
