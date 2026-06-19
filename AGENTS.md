# Agent Notes

## Commits

- Commit as the repository owner only. Do **not** add `Co-Authored-By: Claude`
  (or any AI co-author) trailers, and do not add `Claude-Session` links to
  commit messages.

## Running the app locally

Use the Aspire CLI from the repo root:

- `aspire run` - start the AppHost (SQL + Server + Vite together) in the foreground.
- `aspire stop` - stop the running AppHost cleanly (or `aspire stop --all`).
- `aspire ps` - list running AppHosts.

Don't run `npm run dev` in `frontend/` on its own - the Vite proxy needs env vars that only the Aspire AppHost injects, so the SPA hits a `/api/config` 500 and won't render.

If you only need to eyeball the UI (no real data), you *can* run `npm run dev` standalone and stub the backend via your browser-automation tool's request interception: fulfil `GET /api/config` with `{ "gardenName": "...", "applicationInsightsConnectionString": null, "turnstileSiteKey": null }` (and return `[]` for `/api/events/*`, `/api/instagram/*`, etc.). The home page then renders without the AppHost.

## Frontend

- **Type-checking.** The root `frontend/tsconfig.json` is references-only, so a bare `tsc --noEmit` checks nothing. Use `npx tsc -p tsconfig.app.json --noEmit` (or `npm run build`, which runs `tsc -b`). Run `npm run lint` too - `noUnusedLocals` is on and ESLint treats unused vars as **errors**, so an imported-but-unused symbol fails the build.
- **Routing is code-split.** Route components in `frontend/src/App.tsx` are loaded with `React.lazy(() => import('./pages/...'))` and rendered under one top-level `<Suspense>`; the `Home` landing page is kept eager for fast first paint. New routes must be `export default` components - add them as `lazy(...)` and they fall under the existing `<Suspense>` automatically.
- **Don't defeat the split.** If a heavy module (e.g. the tiptap-based `BlogEditor`) is `lazy`-imported in one place, don't *static*-import it elsewhere - Vite will refuse to split it and warn. Keep every importer of such a module dynamic.
- **Home page styling.** The public home page (`frontend/src/pages/home/`) uses inline `style={{}}` objects, not CSS modules, and adapts to viewport via the `useMediaQuery` hook in `responsive.ts` (breakpoints: `BP_MOBILE`, `BP_TABLET`, `BP_HEADER`). Keep new responsive logic going through that hook rather than adding media queries elsewhere.
