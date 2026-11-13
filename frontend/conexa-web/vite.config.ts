import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// host: true => escuta em 0.0.0.0 (acesso por outro computador da rede local)
export default defineConfig({
  plugins: [react()],
  server: { host: true, port: 5173 },
});
