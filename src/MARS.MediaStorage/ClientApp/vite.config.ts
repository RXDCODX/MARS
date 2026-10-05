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
        // Порт Gateway взят из docker-compose.yml: на 9155 на машине
        // разработчика висит старый монолит MARS.Server, и прокси уходил бы в
        // чужой процесс вместо стенда.
        target: 'http://localhost:10155',
        changeOrigin: true,
      },
    },
  },
});
