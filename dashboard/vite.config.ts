import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// Served by the ASP.NET app under /dashboard, so assets must resolve under that base.
export default defineConfig({
  base: '/dashboard/',
  plugins: [react()],
  server: {
    // Local `npm run dev` proxies API calls to the running .NET app.
    proxy: {
      '/api': 'http://localhost:8080',
    },
  },
})
