import withSerwistInit from "@serwist/next";

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
  },

  env: {
    NEXT_PUBLIC_BUILD_ID: process.env.NEXT_PUBLIC_BUILD_ID
  }
};

const withSerwist = withSerwistInit({
  swSrc: "app/sw.ts", // add the path where you create sw.ts
  swDest: "public/sw.js",
  disable: process.env.NODE_ENV === "development", // to disable pwa in development
  register: true,
  reloadOnOnline: false
});

// Export the combined configuration for Next.js with PWA support
export default withSerwist(nextConfig);