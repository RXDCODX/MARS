import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// Собираем статику в отдельный каталог, а не в wwwroot. wwwroot — это
// git-версионируемый том хранилища: будь UI внутри него, каждый ребилд
// порождал бы коммит с минифицированными файлами, а 1984 файла UI попадали бы
// в таблицу записей как «медиа». Каталог отдаётся сервисом через отдельный
// StaticFileOptions с RequestPath /storage-ui.
export default defineConfig({
  plugins: [react()],
  base: '/storage-ui/',
  build: {
    outDir: '../ui-dist',
    emptyOutDir: true,
  },
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: 'http://localhost:9155',
        changeOrigin: true,
      },
    },
  },
});
