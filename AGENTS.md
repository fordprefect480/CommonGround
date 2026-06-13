# Agent Notes

## Running the app locally

Use the Aspire CLI from the repo root:

- `aspire run` - start the AppHost (SQL + Server + Vite together) in the foreground.
- `aspire stop` - stop the running AppHost cleanly (or `aspire stop --all`).
- `aspire ps` - list running AppHosts.

Don't run `npm run dev` in `frontend/` on its own - the Vite proxy needs env vars that only the Aspire AppHost injects, so the SPA hits a `/api/config` 500 and won't render.
