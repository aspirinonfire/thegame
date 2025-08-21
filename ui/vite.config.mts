import { reactRouter } from "@react-router/dev/vite";
import tailwindcss from "@tailwindcss/vite";
import { defineConfig } from "vite";
import tsconfigPaths from "vite-tsconfig-paths";
import flowbiteReact from "flowbite-react/plugin/vite";
import { VitePWA } from 'vite-plugin-pwa'
import { viteStaticCopy } from 'vite-plugin-static-copy';

export default defineConfig({
  plugins: [tailwindcss(), reactRouter(), tsconfigPaths(), flowbiteReact(),
    viteStaticCopy({
      targets: [
        { src: 'node_modules/onnxruntime-web/dist/ort-wasm-simd-threaded.wasm', dest: 'assets' },
        { src: 'node_modules/onnxruntime-web/dist/ort-wasm-simd-threaded.mjs',  dest: 'assets' }
      ]
    }),
    VitePWA({
      base: '/',
      registerType: 'autoUpdate',
      strategies: 'generateSW',
      filename: 'sw.js',
      injectRegister: 'auto',
      disable: false,

      workbox: {
        clientsClaim: true,
        skipWaiting: true,
        globPatterns: [
          '**\/*.{js,json,css,html,woff2,ico,png,jpg,jpeg,svg,avif,webp,gif,mjs,onnx,wasm}'
        ],
        navigationPreload: false,
        navigateFallback: '/index.html',
        cleanupOutdatedCaches: true,
        additionalManifestEntries: [{ url: '/index.html', revision: Date.now().toString() }],
        runtimeCaching: [
          // we must cache react-router manifest manually due to an unresolved bug
          // see https://github.com/remix-run/react-router/issues/12659
          {
            urlPattern: ({ url }) => url.pathname.startsWith('/assets/manifest-') && url.pathname.endsWith('.js'),
            handler: 'CacheFirst',
            options: {
              cacheName: 'rr-manifest',
              plugins: [],
              expiration: {
                maxEntries: 1,
                purgeOnQuotaError: true
              }
            }
          }
        ],
        maximumFileSizeToCacheInBytes: 1024 * 1024 * 30
      },
      manifest: {
        name: "The License Plate Game",
        short_name: "The License Plate Game",
        icons: [
          {
            src: "/icons/map-blue-512.png",
            sizes: "512x512",
            type: "image/png",
            purpose: "any"
          }
        ],
        theme_color: "#FFFFFF",
        background_color: "#FFFFFF",
        start_url: "/",
        display: "standalone",
        orientation: "portrait",
        screenshots: [
          {
          src: "screenshots/GameUI.png",
            sizes: "693x789",
            type: "image/png",
            form_factor: "narrow",
            label: "The Game"
          },
          {
            src: "screenshots/GameUI.png",
            sizes: "693x789",
            type: "image/png",
            form_factor: "wide",
            label: "The Game"
          }
        ]
      }
    })
  ],
  build: {
    minify: 'esbuild',
    sourcemap: false
  },
  optimizeDeps: {
    exclude: ["onnxruntime-web"],
  }
});