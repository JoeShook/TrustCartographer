# DIDComm Messaging Specification v2.1

- **Status:** DIF Recommendation
- **Canonical URL:** https://identity.foundation/didcomm-messaging/spec/v2.1/
- **Fetched:** 2026-05-21
- **Local file:** `didcomm-v2.html`

## Why we care
**The DIF camp's answer to agent-to-agent communication.** Direct counterpoint to Google A2A and (eventually) to UDAP-based agent flows. End-to-end encrypted, authenticated, transport-agnostic messages routed between DID-controlled agents.

## Key concepts
- **DIDComm Message** — JWE-encrypted JSON envelope addressed to a recipient DID
- **Service endpoint** — `DIDCommMessaging` entry in a DID Document tells the world where to send messages
- **Routing** — Messages can be relayed via mediators; recipient DID's service endpoints define the route
- **Protocols** — Higher-level protocols (issue-credential, present-proof, basic-message) ride on top
- **Key agreement** — Uses `keyAgreement` verification methods (X25519 typically) for encryption

## Position in the comparison
- **Google A2A**: agent identity is a Google account / managed service
- **DIDComm**: agent identity is a DID; trust comes from a trust registry, no central authority
- **UDAP**: agent identity is an X.509 cert in a community; trust comes from the community trust anchors

DIDComm and UDAP both reject the "single platform authority" model but solve trust establishment differently. TrustCartographer's comparative view should show all three side-by-side.
