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
  distDir: 'out',
  // define custom image loader that will work with static files
  images: {
    loader: "custom",
    loaderFile: "./staticImgLoader.ts"
  }
};

// Configuration object tells the next-pwa plugin
// https://javascript.plainenglish.io/building-a-progressive-web-app-pwa-in-next-js-with-serwist-next-pwa-successor-94e05cb418d7
const withPWA = withPWAInit({
  dest: "public",
  register: true,
  reloadOnOnline: false,
  cacheStartUrl: true,
  dynamicStartUrl: true,
  skipWaiting: true,
  workboxOptions: {
    skipWaiting: true,
    clientsClaim: true,
    cleanupOutdatedCaches: true,
    runtimeCaching: [
      {
        urlPattern: /^https?.*/,
        handler: "CacheFirst",
        options: {
          cacheName: "http-cache",
          networkTimeoutSeconds: 15,
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