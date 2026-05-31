---
name: project-rcl-layout
description: TrustCartographer three-project structure (Core / UI RCL / Web host) for MAUI portability
metadata: 
  node_type: memory
  type: project
  originSessionId: 6b1ccb6a-277f-4760-92dd-c0f12f5d5930
---

TrustCartographer is structured as three projects:

- `src/TrustCartographer.Core/` — pure .NET class library: services (`CryptoService`, `DidKeyService`, `VcService`), models. No UI deps.
- `src/TrustCartographer.UI/` — Razor Class Library (`Microsoft.NET.Sdk.Razor`): all components, layouts, pages, shared widgets (`JwtViewer`, `SsiSequenceDiagram`), `Routes.razor`. Namespace: `TrustCartographer.UI` (with `.Pages`, `.Layout`, `.Shared` sub-namespaces).
- `src/TrustCartographer.Web/` — Blazor Server host: `Program.cs`, `Components/App.razor` (HTML shell), `Components/Pages/Error.razor` (server-only — uses `HttpContext`), `wwwroot/`. Minimal — most code lives in `UI`.

**Why:** the user wants to eventually run this as a MAUI Blazor Hybrid app on iPad / Windows / Mac (Catalyst). RCL is the portable unit; a future `TrustCartographer.MAUI` head just adds `<ProjectReference>` to `UI` + `Core` and mounts `Routes.razor` in a `BlazorWebView`.

**Gotcha worth remembering:** when adding a new host (or after extracting pages to an RCL for the first time), `Program.cs` needs `app.MapRazorComponents<App>().AddInteractiveServerRenderMode().AddAdditionalAssemblies(typeof(TrustCartographer.UI.Routes).Assembly);`. Without `AddAdditionalAssemblies`, RCL pages return 404 with no error log — the framework rejects the request before the Router sees it.

**Other host-side wiring:** `App.razor` links the RCL's bundled CSS (`@Assets["_content/TrustCartographer.UI/TrustCartographer.UI.bundle.scp.css"]`). Web's `Components/_Imports.razor` has `@using TrustCartographer.UI` + `@using TrustCartographer.UI.Layout` so `<Routes />` and `<ReconnectModal />` resolve in `App.razor`.

**Portability rule:** anything in `UI/` that needs `HttpContext`, `IHttpContextAccessor`, or server-only APIs is a portability bug — move to the host. Currently clean. `Error.razor` stays in `Web/` for exactly this reason.

See [docs/project-layout.md](../../TrustCartographer/docs/project-layout.md) for the full structure + MAUI head recipe.
