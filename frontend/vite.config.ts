import react from '@vitejs/plugin-react'
import { defineConfig } from 'vitest/config'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    // Dev proxy to the API (https profile, launchSettings.json:7093).
    // The refresh cookie is Secure + SameSite=Strict + path=/api/auth, so it is
    // only stored when the browser page is served over https. Login still works
    // over http; silent refresh needs an https dev origin.
    proxy: {
      '/api': {
        target: 'https://localhost:7093',
        changeOrigin: true,
        secure: false,
      },
    },
  },
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
  },
})
