import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// https://vite.dev/config/
// Both the API and the backend-rendered unsubscribe page live on the app
// service; in dev the Vite server forwards these paths to it.
const toBackend = {
  target: process.env.SERVER_HTTPS || process.env.SERVER_HTTP,
  changeOrigin: true,
};

export default defineConfig({
  plugins: [react()],
  server: {
    // Listen on all interfaces (0.0.0.0) so the dev server is reachable
    // from other devices on the LAN (e.g. a phone for mobile previews).
    host: true,
    // The AppHost pins this port (Aspire's proxy forwards to it); if it's
    // taken, drifting to 5174 would break the proxy anyway, so fail loudly.
    strictPort: true,
    proxy: {
      '/api': toBackend,
      // The unsubscribe confirmation page is rendered by the backend (it has no
      // React route); in production the server serves it on the same origin.
      '/unsubscribe': toBackend,
    },
  },
});
