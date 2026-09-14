import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

const backend = process.env.BACKEND || "http://localhost:7654";

export default defineConfig({
  plugins: [react()],
  server: {
    host: true, // listen on all addresses so container and host can reach it
    port: 3000,
    strictPort: false,
    proxy: {
      "/api": {
        target: backend,
        changeOrigin: true,
        secure: false,
      },
    },
  },
});
