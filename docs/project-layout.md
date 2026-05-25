# Project Layout

TrustCartographer is structured as three projects so the UI can be hosted from multiple shells — currently Blazor Server, eventually MAUI Blazor Hybrid for iPad / Windows / Mac.

```
src/
├── TrustCartographer.Core/        Class library — pure .NET, no UI deps
│   ├── Models/                    POCO types (KeyPairInfo, SignatureResult)
│   └── Services/                  Crypto, did:key, JWT-VC logic
│
├── TrustCartographer.UI/          Razor Class Library — all components
│   ├── Layout/                    MainLayout, NavMenu, ReconnectModal
│   ├── Pages/                     @page routables (Home, KeyPairExplorer, PractitionerLicense, ...)
│   ├── Shared/                    Reusable components (JwtViewer, SsiSequenceDiagram)
│   ├── Routes.razor               Router; uses typeof(Routes).Assembly so it finds pages here
│   └── _Imports.razor             Component-wide @using (FluentUI, RegularIcons alias, etc.)
│
└── TrustCartographer.Web/         Blazor Server host (current default head)
    ├── Components/
    │   ├── App.razor              <html>/<head>/<body> shell, <Routes> mount, <HeadOutlet>
    │   ├── Pages/Error.razor      Server-only (uses HttpContext) — must stay in the host
    │   └── _Imports.razor         Minimal imports for App.razor
    ├── Program.cs                 DI registration + endpoint mapping
    └── wwwroot/                   Host-specific static assets (app.css, favicon)
```

## Why three projects

- **Core has no UI deps.** Any host can consume it. Tests can target it directly.
- **UI is a Razor Class Library (RCL).** Components, layouts, pages, and CSS-isolated styles all live here. An RCL is the *portable unit*; it can be referenced by Blazor Server, Blazor WebAssembly, and Blazor Hybrid (MAUI) hosts identically.
- **Web is one possible host.** Adding a MAUI head is a matter of creating `TrustCartographer.MAUI/` that references the same `TrustCartographer.UI` and `TrustCartographer.Core` projects.

## What goes where

| Code | Project | Why |
|---|---|---|
| Crypto primitives, DID/VC logic, POJO models | `Core` | Pure .NET, reusable anywhere |
| `@page` routables, layouts, shared Razor components | `UI` | The portable component layer |
| `Program.cs`, DI registration, endpoint mapping | `Web` | Host-specific |
| `App.razor` root with `<html><head><body>` | `Web` | The shell is host-specific (server vs hybrid render differently) |
| Anything using `HttpContext`, server-rendered error pages | `Web` | Server-only — would not compile against a MAUI host |
| `wwwroot/app.css`, `favicon.png` | `Web` | Host-served static assets |
| Component-isolated styles (`Component.razor.css`) | `UI` | Bundled into `TrustCartographer.UI.bundle.scp.css` and served via `_content/TrustCartographer.UI/...` |

## Host wiring (the parts that aren't automatic)

### 1. Router finds pages in the RCL

`Routes.razor` (in the UI project) uses:
```razor
<Router AppAssembly="typeof(Routes).Assembly" ...>
```

`typeof(Routes).Assembly` resolves to **the UI project's assembly**, so the Router scans for `@page` components there.

### 2. Endpoint registration must also know about the RCL

This is the gotcha that bit us. In `Program.cs`:
```csharp
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(TrustCartographer.UI.Routes).Assembly);
```

Without `AddAdditionalAssemblies`, the endpoint mapping only scans the host project (`TrustCartographer.Web`) and your RCL pages return **404 with no error log** — the framework rejects the request before the Router ever sees it.

### 3. CSS isolation bundle from the RCL

In `App.razor`:
```html
<link rel="stylesheet" href="@Assets["_content/TrustCartographer.UI/TrustCartographer.UI.bundle.scp.css"]" />
```

Each RCL bundles its component-isolated CSS (`*.razor.css` files) into `<RclAssembly>.bundle.scp.css` and serves it under `_content/<RclAssembly>/`. The host needs to link it explicitly.

### 4. Imports

The RCL's `_Imports.razor` is for the RCL's own components. The host has its own `_Imports.razor` (in `Web/Components/`) with the minimum imports needed for `App.razor` to compile — including `@using TrustCartographer.UI` so `<Routes />` and `<ReconnectModal />` resolve.

## Future: adding a MAUI head

When ready to run TrustCartographer on iPad / Windows / Mac (Catalyst):

1. **Create `src/TrustCartographer.MAUI/`** as a .NET MAUI Blazor Hybrid project (`dotnet new maui-blazor`).
2. **Reference** `TrustCartographer.UI` and `TrustCartographer.Core` (drop the default MAUI template's component projects).
3. **Mount the RCL's root component** in `MainPage.xaml`:
   ```xml
   <BlazorWebView HostPage="wwwroot/index.html">
       <BlazorWebView.RootComponents>
           <RootComponent Selector="#app"
                          ComponentType="{x:Type ui:Routes}"
                          xmlns:ui="clr-namespace:TrustCartographer.UI;assembly=TrustCartographer.UI" />
       </BlazorWebView.RootComponents>
   </BlazorWebView>
   ```
4. **Register the same services** in `MauiProgram.cs`:
   ```csharp
   builder.Services.AddSingleton<CryptoService>();
   builder.Services.AddSingleton<DidKeyService>();
   builder.Services.AddSingleton<VcService>();
   builder.Services.AddFluentUIComponents();
   ```
5. **wwwroot/index.html** in the MAUI project needs a `<div id="app">` and the same `<link>` to `_content/TrustCartographer.UI/TrustCartographer.UI.bundle.scp.css`.

No changes to `TrustCartographer.UI` or `TrustCartographer.Core` should be needed. Anything in the UI that *would* need to change (e.g., a component that takes a hard dependency on `HttpContext` or server-only APIs) is a portability bug — keep those concerns in the host.

## Things that *would* break MAUI portability

Watch out for in the UI project:
- `@inject IHttpContextAccessor` or `HttpContext` references → server-only, move to host
- Server-side data-fetching that assumes ASP.NET pipeline (e.g. cookies, session)
- File I/O assuming server filesystem layout
- `HttpClient` registrations that depend on host config — accept `HttpClient` as injected, let the host configure it

The current UI project is clean of these. The Error page that *did* depend on `HttpContext` was kept in `Web/Components/Pages/`.

## Related

- [`walkthroughs/README.md`](walkthroughs/README.md) — how to add new walkthrough pages (they live in the UI project)
- [`sigil-and-trustcartographer.md`](sigil-and-trustcartographer.md) — TrustCartographer's role vs Sigil's role
