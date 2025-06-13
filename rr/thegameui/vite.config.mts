import { reactRouter } from "@react-router/dev/vite";
import tailwindcss from "@tailwindcss/vite";
import { defineConfig } from "vite";
import tsconfigPaths from "vite-tsconfig-paths";
import flowbiteReact from "flowbite-react/plugin/vite";
import { VitePWA } from 'vite-plugin-pwa'

export default defineConfig({
  plugins: [tailwindcss(), reactRouter(), tsconfigPaths(), flowbiteReact(),
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
          '**\/*.{js,json,css,html,woff2,ico,png,jpg,jpeg,svg,avif,webp,gif}'
        ],
        navigateFallback: '/index.html',
        cleanupOutdatedCaches: true,
        additionalManifestEntries: [{ url: '/index.html', revision: Date.now().toString() }]
      },
      manifest: {
        name: "The License Plate Game",
        short_name: "The License Plate Game",
        icons: [
          {
            src: "/icons/map-blue-128.png",
            sizes: "128x128",
            type: "image/png"
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
            sizes: "430x584",
            type: "image/png",
            form_factor: "narrow",
            label: "The Game"
          },
          {
            src: "screenshots/GameUI.png",
            sizes: "430x584",
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
  }
});