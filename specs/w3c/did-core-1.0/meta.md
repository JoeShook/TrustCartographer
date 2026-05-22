# Decentralized Identifiers (DIDs) v1.0

- **Status:** W3C Recommendation (19 July 2022)
- **Canonical URL:** https://www.w3.org/TR/did-1.0/
- **Fetched:** 2026-05-21
- **Local file:** `did-core-1.0.html`

## Why we care
The foundational spec for DIDs. Defines the DID syntax, DID Document data model, verification methods, services, and method-agnostic resolution requirements. This is the root of every DID-related decision in Sigil and TrustCartographer.

## Key concepts to map into Sigil
- **DID** — `did:method:method-specific-id` (the identifier itself)
- **DID Document** — JSON-LD or JSON with `id`, `verificationMethod[]`, `authentication[]`, `assertionMethod[]`, `keyAgreement[]`, `capabilityInvocation[]`, `capabilityDelegation[]`, `service[]`
- **Verification Method** — A public key with a purpose; the SSI analog to an X.509 SubjectPublicKeyInfo + KeyUsage
- **DID Controller** — The entity authorized to make changes to the DID Document (analog to a cert's private-key holder)
- **DID Resolution** — Method-dispatched lookup that returns a DID Document
- **Deactivation** — How a DID is "revoked" (not deleted — DID Documents are immutable history)
