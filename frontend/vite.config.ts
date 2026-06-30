import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    // Listen on all interfaces (0.0.0.0) so the dev server is reachable
    // from other devices on the LAN (e.g. a phone for mobile previews).
    host: true,
    proxy: {
      // Proxy API calls to the app service
      '/api': {
        target: process.env.SERVER_HTTPS || process.env.SERVER_HTTP,
        changeOrigin: true,
      },
      // The unsubscribe confirmation page is rendered by the backend (it has no
      // React route). In production the server serves it on the same origin; in
      // dev the link points at the Vite server, so forward it to the backend.
      '/unsubscribe': {
        target: process.env.SERVER_HTTPS || process.env.SERVER_HTTP,
        changeOrigin: true,
      },
    },
  },
});
