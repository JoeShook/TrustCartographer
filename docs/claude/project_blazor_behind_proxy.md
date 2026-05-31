---
name: project-blazor-behind-proxy
description: How the Issuer/Holder Blazor UIs are made to work behind the TC.Web YARP path-prefix proxy
metadata: 
  node_type: memory
  type: project
  originSessionId: 6b1ccb6a-277f-4760-92dd-c0f12f5d5930
---

The Issuer (`/issuer`) and Holder (`/holder`) Blazor Server UIs are served behind TC.Web's YARP proxy, which strips the prefix (`PathRemovePrefix`). For Blazor to work through the proxy three things must line up:

1. **YARP must explicitly set `X-Forwarded-Prefix`.** The `{ "X-Forwarded": ..., "Prefix": "Set" }` shorthand does NOT carry the stripped prefix — it reflects the (empty) incoming `Request.PathBase`, so the downstream header arrives empty. Fix: add an explicit `{ "RequestHeader": "X-Forwarded-Prefix", "Set": "/issuer" }` transform per route in `TrustCartographer.Web/appsettings.json`.
2. **Each host reinstates PathBase** from that header via middleware in `Program.cs` (runs before `UseAntiforgery`/`MapStaticAssets`/`MapRazorComponents`).
3. **`App.razor` emits a dynamic `<base href>`** from `HttpContext.Request.PathBase` (cascading `HttpContext` parameter), and all nav/links are **relative** (`Href="mint"`, `Href=""` for home, `NavigateTo("wallet")`) so they resolve under the base.

Same header also drives `Oid4VciEndpoints.IssuerBaseUrl`, so fixing it makes `credential_issuer` advertise `https://localhost:7257/issuer` and the OID4VCI metadata round-trips correctly. Verified: mint→redeem produces a real JWT-VC through the proxy. See [[project-rcl-layout]].
