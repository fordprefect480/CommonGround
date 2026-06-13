# CommonGround

An open-source web application for community gardens - a public blog, member directory, and admin tools, built as a reusable platform any garden can self-host.

The codebase is a single .NET Aspire solution that orchestrates an ASP.NET Core API, a React single-page frontend, and a SQL Server database, so a developer can clone the repo and run the entire stack with one command.

## Tech stack

| Layer        | Technology                                                                 |
|--------------|----------------------------------------------------------------------------|
| Orchestration | [.NET Aspire 13.4](https://learn.microsoft.com/dotnet/aspire/) AppHost     |
| API           | ASP.NET Core 10 with [FastEndpoints 8](https://fast-endpoints.com)         |
| Auth          | ASP.NET Core Identity (Identity API endpoints, cookie auth)                |
| Database      | SQL Server (containerised via Aspire) + EF Core 10                         |
| Frontend      | React 19, React Router 7, Vite 8, TypeScript 5.9                           |
| Rich text     | [tiptap 3](https://tiptap.dev)                                             |
| Telemetry     | OpenTelemetry (traces, metrics, logs) via Aspire ServiceDefaults           |

## Key features

- Mobile-responsive public site - the homepage and public pages reflow from desktop to phone, with a slide-in nav drawer on small screens
- Public blog with categories (Newsletters, Events, How-to, Announcements), slugs, featured images
- Admin console (`/admin`) for member CRUD, blog post editing with rich-text + image upload, and tools
- Wix blog importer that pulls existing posts from a `feed.xml` source
- Member export to Excel (.xlsx)
- Cookie-based auth with role-gated admin routes
- Single-binary publish: the Vite-built SPA is bundled into the API's `wwwroot` for production

## Repository layout

```
.
├── CommonGround.AppHost/      Aspire orchestrator - start here
├── CommonGround.Server/       ASP.NET Core API
│   ├── Account/               /api/account/* - current user profile
│   ├── Auth/                  Endpoint groups, role constants, dev seed
│   ├── Blog/                  /api/blog/* (public) + /api/admin/blog/* (admin)
│   │   ├── Admin/             Blog CRUD + image upload
│   │   ├── AdminTools/        Wix import, orphan-image cleanup
│   │   ├── BlogImport/        WixBlogClient + BlogImporter
│   │   └── Public/            Public listing/detail
│   ├── Configuration/         GardenOptions (per-garden settings)
│   ├── Data/                  AppDbContext, entities, EF migrations
│   ├── Members/               /api/admin/members/* - admin member CRUD + Excel export
│   ├── Misc/                  Health ping, /api/config, logout
│   └── Extensions.cs          Aspire ServiceDefaults (OTel, health checks, service discovery)
├── frontend/                  React + Vite SPA (esproj - built by Aspire)
│   └── src/
│       ├── api/               Typed clients (auth, blog, adminTools, config)
│       ├── pages/             Route components (lazy-loaded via React.lazy)
│       │   ├── home/          Public home page sections + responsive.ts (useMediaQuery)
│       │   ├── blog/          Public blog index + post
│       │   └── admin/         Admin console pages
│       ├── App.tsx            Routes + code-split lazy imports
│       ├── AppConfigContext.tsx
│       └── AuthContext.tsx
├── CommonGround.slnx          Solution file (.slnx XML format)
└── LICENSE                    GPL-3.0
```

## Getting started

### Prerequisites

| Tool                  | Version | Notes                                                     |
|-----------------------|---------|-----------------------------------------------------------|
| .NET SDK              | 10.0+   | Required by Aspire AppHost and the API                    |
| Node.js               | 20.19+  | Vite 8 requires Node 20.19+ or 22.12+                     |
| A container runtime   | any     | Docker Desktop, Podman, or Rancher Desktop - Aspire spins up SQL Server in a container |
| ASP.NET dev cert      | -       | Run `dotnet dev-certs https --trust` once if you've never done so |

> **Windows note:** Aspire's container support uses your installed runtime as-is. On macOS/Linux, ensure the Docker daemon is running before starting AppHost.

### Clone and restore

```bash
git clone https://github.com/<your-fork>/CommonGround.git
cd CommonGround

# Restore .NET dependencies
dotnet restore CommonGround.slnx

# Install frontend dependencies
cd frontend && npm install && cd ..
```

### Run the stack

The Aspire AppHost is the single entry point. It launches SQL Server (in a container with a persistent volume), the API, and the Vite dev server, and shows them all in the Aspire dashboard.

```bash
dotnet run --project CommonGround.AppHost
```

Then open the Aspire dashboard at the URL printed in the console (typically <https://localhost:17158>). From the dashboard you can click into:

- **`webfrontend`** - the running React app (Vite dev server)
- **`server`** - the API, with live logs, traces, and metrics
- **`sql`** - the SQL Server container
- **`commongroundDb`** - the database (with a connection string ready to copy)

The API is also reachable directly at <https://localhost:7592> if you prefer.

### First-run behaviour

The API applies any pending EF Core migrations on startup (`Database.MigrateAsync`) in **every environment**. EF takes an exclusive lock on `__EFMigrationsHistory`, so concurrent replicas don't race.

When `ASPNETCORE_ENVIRONMENT=Development`, the API additionally seeds an admin user if one does not exist:

- **Email:** `admin@local`
- **Password:** `Password123!`
- **Role:** `Admin`

Sign in at the SPA's `/login` page using those credentials to access `/admin`. Production does **not** seed - bootstrap an admin manually (e.g. via a one-off Identity insert).

## Configuration

Configuration is layered in standard ASP.NET Core fashion: `appsettings.json` → `appsettings.{Environment}.json` → user-secrets (Development) → environment variables.

### Settings reference

| Key                      | Where                                  | Description                              | Default                                    |
|--------------------------|----------------------------------------|------------------------------------------|--------------------------------------------|
| `Garden:Name`            | `CommonGround.Server/appsettings.json` | Display name shown in the SPA title bar  | `Seaford Wetlands Community Garden`        |
| `ConnectionStrings:commongroundDb` | injected by Aspire           | SQL Server connection string             | provided by AppHost                        |
| `ASPNETCORE_ENVIRONMENT` | env var                                | `Development` enables the dev admin seed and OpenAPI UI; migrations run in every environment | `Development` (in launchSettings) |
| `Email:ApiToken`         | secret                                 | Resend API key - required for any outbound email (newsletters, contact form) | empty (sending disabled)               |
| `Email:FromAddress`      | `appsettings.json`                     | Verified sender address used as `From` on outbound mail | empty                                    |
| `Email:FromName`         | `appsettings.json`                     | Optional display name for the sender     | empty                                      |
| `Contact:RecipientAddress` | `appsettings.json`                   | Inbox that contact form submissions are delivered to | empty (contact form returns 503)        |
| `Contact:TurnstileSiteKey` | `appsettings.json`                   | Cloudflare Turnstile site key - sent to the frontend so it can render the widget | empty (captcha disabled, form still sends) |
| `Contact:TurnstileSecretKey` | secret                             | Cloudflare Turnstile secret key - used server-side to verify the captcha token | empty (captcha disabled, form still sends) |

### Per-developer secrets

User secrets are bound at the AppHost project (UserSecretsId `9f693f87-7a45-4716-bc0d-7eec835b2bd6`). Use them for anything you don't want to commit:

```bash
dotnet user-secrets --project CommonGround.AppHost set <Key> <Value>
```

### Contact form and captcha (Cloudflare Turnstile)

The contact form (rendered by `ContactPage` on the public site) posts to `POST /api/contact`. The API sends the submission as an email via Resend to `Contact:RecipientAddress`, with the submitter's address set as `Reply-To` so a regular "reply" in your inbox goes back to them.

The form is protected by [Cloudflare Turnstile](https://www.cloudflare.com/products/turnstile/) - a free, privacy-friendly CAPTCHA that doesn't depend on Google. Turnstile is optional: if no keys are configured, the form still works, just without bot protection.

**Set up Turnstile for production**

1. Sign in at <https://dash.cloudflare.com/?to=/:account/turnstile> and click **Add site**.
2. Pick a friendly name and add every domain the form will run on (e.g. `commonground.example.org`, plus any staging/preview domains). For local development, also add `localhost`.
3. Choose a widget mode:
   - **Managed** (recommended) - Cloudflare decides between an invisible check and an interactive challenge per request.
   - **Non-interactive** - runs invisibly but always renders a small "verifying" widget.
   - **Invisible** - fully hidden; failed challenges block submission silently.
4. Save. Cloudflare gives you a **site key** (public, safe to embed) and a **secret key** (server-side only).

**Configure the keys**

The site key lives in plain `appsettings.json`; the secret belongs in user secrets (dev) or an env var / secret store (prod):

```bash
# Site key (public - fine to commit to appsettings.{Environment}.json)
# appsettings.Production.json:
#   "Contact": {
#     "RecipientAddress": "contact@yourgarden.org",
#     "TurnstileSiteKey": "0x4AAAAAAA..."
#   }

# Secret key (never commit)
dotnet user-secrets --project CommonGround.AppHost set Contact:TurnstileSecretKey "0x4AAAAAAA..."

# Or as env vars in production:
#   Contact__TurnstileSecretKey=0x4AAAAAAA...
```

The `/api/config` endpoint exposes `turnstileSiteKey` to the SPA, which lazy-loads `https://challenges.cloudflare.com/turnstile/v0/api.js` and renders the widget. If `turnstileSiteKey` is null, the SPA skips the widget entirely.

**Local development**

`appsettings.Development.json` ships with Cloudflare's published [always-pass test keys](https://developers.cloudflare.com/turnstile/troubleshooting/testing/), so the captcha flow is exercised end-to-end without anyone provisioning a real Turnstile site. Other useful test keys when debugging:

| Site key                      | Secret key                                | Behaviour                  |
|-------------------------------|-------------------------------------------|----------------------------|
| `1x00000000000000000000AA`    | `1x0000000000000000000000000000000AA`     | Always passes (default in dev) |
| `2x00000000000000000000AB`    | `2x0000000000000000000000000000000AA`     | Always blocks              |
| `3x00000000000000000000FF`    | `1x0000000000000000000000000000000AA`     | Forces an interactive challenge |

To disable the captcha locally (e.g. while iterating on form layout), blank `Contact:TurnstileSiteKey` and `Contact:TurnstileSecretKey` in `appsettings.Development.json`.

**Disabling the contact form**

Leave `Contact:RecipientAddress` empty. The endpoint will return `503 Service Unavailable` for every submission, which the SPA surfaces as a "temporarily unavailable" error.

## Development workflow

### Run only the API (no Aspire)

If you just need to iterate on the API without the full Aspire stack, you can run the Server project directly - but you'll need to provide a SQL Server connection string yourself, since Aspire's container won't be there.

```bash
dotnet run --project CommonGround.Server
# API at https://localhost:7592 / http://localhost:5335
```

### Run only the frontend

The Vite dev server proxies `/api/*` calls to whatever URL is in `SERVER_HTTPS` or `SERVER_HTTP` env vars (set automatically by Aspire). To run standalone, point it at a running API yourself:

```bash
cd frontend
SERVER_HTTPS=https://localhost:7592 npm run dev
```

### Frontend scripts

```bash
cd frontend
npm run dev      # Vite dev server with HMR
npm run build    # Type-check (tsc -b) then production build to dist/
npm run lint     # ESLint over src/
npm run preview  # Serve the production build locally
```

After any TypeScript change, run `npx tsc -p tsconfig.app.json --noEmit` to type-check without producing output. The root `tsconfig.json` only holds project references, so a bare `tsc --noEmit` checks nothing - point at `tsconfig.app.json` (or run `tsc -b`, which is what `npm run build` does).

### Database migrations

EF Core migrations live in [`CommonGround.Server/Data/Migrations/`](CommonGround.Server/Data/Migrations/).

```bash
# Add a new migration
dotnet ef migrations add <MigrationName> --project CommonGround.Server

# Apply pending migrations to a target database manually (the API also applies on startup)
dotnet ef database update --project CommonGround.Server
```

Migrations are applied automatically on every startup, in all environments. The CLI command above is for when you want to apply ahead of a deploy or against a non-default database.

### API exploration

The API exposes OpenAPI in Development at:

```
https://localhost:7592/openapi/v1.json
```

The most relevant route prefixes:

| Prefix              | Purpose                                              |
|---------------------|------------------------------------------------------|
| `/api/auth/*`       | ASP.NET Core Identity API (register, login, etc.)    |
| `/api/account/*`    | Current user's profile (`GET`/`PUT /api/account/me`) |
| `/api/blog/*`       | Public blog listing, post detail, images             |
| `/api/admin/*`      | Admin-only CRUD (members, blog posts, tools)         |
| `/api/config`       | Public app config (garden name, Turnstile site key)  |
| `/api/contact`      | Public contact form submission (captcha-verified)    |
| `/api/health/ping`  | Liveness probe                                       |
| `/health`, `/alive` | Aspire health endpoints (Development only)           |

There's a small [`CommonGround.Server.http`](CommonGround.Server/CommonGround.Server.http) file you can use with VS Code's REST client extension for ad-hoc requests.

## Build and publish

### Solution build

```bash
dotnet build CommonGround.slnx
```

### Production publish

The AppHost bundles the Vite build output into the API's container image so the API can serve the SPA from `wwwroot`:

```csharp
// In AppHost.cs
server.PublishWithContainerFiles(webfrontend, "wwwroot");
```

To publish:

```bash
dotnet publish CommonGround.AppHost -c Release
```

Aspire generates a deployment manifest you can target at any container host.

## Architecture notes

- **Endpoint groups.** FastEndpoints' `Group<T>` and `SubGroup<T>` are used to compose route prefixes and auth requirements. `AdminGroup` enforces the `Admin` role on every nested endpoint - see [`Auth/EndpointGroups.cs`](CommonGround.Server/Auth/EndpointGroups.cs).
- **Aspire ServiceDefaults.** [`Extensions.cs`](CommonGround.Server/Extensions.cs) wires OpenTelemetry (ASP.NET Core, HttpClient, Runtime), health checks, and service discovery into every project that calls `AddServiceDefaults()`.
- **OTLP exporter.** Telemetry is exported via OTLP whenever `OTEL_EXPORTER_OTLP_ENDPOINT` is set - Aspire sets this automatically in the dashboard. To send to Azure Monitor or another backend, layer an additional exporter onto the OpenTelemetry builder.
- **HTML sanitisation.** Blog post HTML (whether typed in the editor or imported from Wix) is run through [HtmlSanitizer](https://github.com/mganss/HtmlSanitizer) - see [`BlogHtmlSanitizer.cs`](CommonGround.Server/Blog/BlogHtmlSanitizer.cs).
- **Code-split frontend.** Route components are lazy-loaded via `React.lazy` + `Suspense` (the `Home` landing page stays eager for fast first paint), so heavy dependencies - the tiptap editor and the Leaflet map - download only when their routes are visited. See [`frontend/src/App.tsx`](frontend/src/App.tsx).
- **Responsive home page.** The public home page composes inline-styled sections that adapt via a small `useMediaQuery` hook ([`frontend/src/pages/home/responsive.ts`](frontend/src/pages/home/responsive.ts)) rather than CSS media queries, keeping each section's styling co-located in its component.

## Troubleshooting

| Symptom                                                                | Likely cause / fix                                                                          |
|------------------------------------------------------------------------|---------------------------------------------------------------------------------------------|
| `dotnet run --project CommonGround.AppHost` hangs at "Starting `sql`"  | Container runtime isn't running. Start Docker / Podman.                                     |
| Browser shows certificate warning at the dashboard URL                 | Run `dotnet dev-certs https --trust` once.                                                  |
| `npm install` fails with peer-dep errors                               | Use Node 20.19+ (or 22.12+). Older Node versions don't satisfy Vite 8's `engines` constraint. |
| API responds but the SPA shows "Failed to load configuration"          | The Vite dev server can't reach the API. Confirm the AppHost is running and check the dashboard for the `webfrontend` resource's wired env vars. |
| EF migration command fails with `No project was found`                 | Run from the repo root with `--project CommonGround.Server`.                                |
| Want to wipe the dev database                                          | Stop AppHost, then `docker volume rm` the `sql-data` volume that Aspire created. The next run will re-seed. |

## Contributing

Contributions are welcome. Please:

1. Open an issue first to discuss any non-trivial change.
2. Branch from `main` and keep PRs focused - one logical change per PR.
3. Run `dotnet build CommonGround.slnx` and `npm run build` (in `frontend/`) before pushing - both must pass.
4. Match the existing code style: latest C# language features, FastEndpoints groups for routing, no docstrings/comments unless they document a non-obvious "why".

## Licence

CommonGround is licensed under the GNU General Public License v3.0. See [LICENSE](LICENSE) for the full text.
