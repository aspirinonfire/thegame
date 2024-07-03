import withPWAInit from '@ducanh2912/next-pwa'

/** @type {import('next').NextConfig} */
// Configuration options for Next.js
// see https://github.com/staticwebdev/nextjs-starter for static web app support
const nextConfig = {
  reactStrictMode: true, // Enable React strict mode for improved error handling
  swcMinify: true,      // Enable SWC minification for improved performance
  compiler: {
    removeConsole: process.env.NODE_ENV !== "development", // Remove console.log in production
  },
  
  trailingSlash: true,
  output: 'export',
  distDir: 'next_out',
  // define custom image loader that will work with static files
  images: {
    loader: "custom",
    loaderFile: "./staticImgLoader.ts"
  }
};

// Configuration object tells the next-pwa plugin
// https://javascript.plainenglish.io/building-a-progressive-web-app-pwa-in-next-js-with-serwist-next-pwa-successor-94e05cb418d7
const withPWA = withPWAInit({
  disable: process.env.NODE_ENV === "development",
  dest: "public",
  register: true,
  reloadOnOnline: false,
  cacheStartUrl: true,
  dynamicStartUrl: true,
  // cache frotend nav to ensure better offline ux
  // https://github.com/shadowwalker/next-pwa/blob/master/examples/cache-on-front-end-nav/README.md
  cacheOnFrontEndNav: true,
  workboxOptions: {
    skipWaiting: true,
    clientsClaim: true,
    cleanupOutdatedCaches: true,
    runtimeCaching: [
      {
        urlPattern: /^https?.*/,
        handler: "StaleWhileRevalidate",
        options: {
          cacheName: "http-cache",
          expiration: {
            maxEntries: 999,
            maxAgeSeconds: 7 * 24 * 60 * 60 // 7days
          },
          cacheableResponse: {
            statuses: [0, 200],
          },
        }
      }
    ]
  }
});

// Export the combined configuration for Next.js with PWA support
export default withPWA(nextConfig);