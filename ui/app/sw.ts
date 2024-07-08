import type { PrecacheEntry, SerwistGlobalConfig } from "serwist";
import { Serwist, CacheFirst, enableNavigationPreload } from "serwist";

// This declares the value of `injectionPoint` to TypeScript.
// `injectionPoint` is the string that will be replaced by the
// actual precache manifest. By default, this string is set to
// `"self.__SW_MANIFEST"`.
declare global {
  interface WorkerGlobalScope extends SerwistGlobalConfig {
    __SW_MANIFEST: (PrecacheEntry | string)[] | undefined;
  }
}

declare const self: ServiceWorkerGlobalScope;

enableNavigationPreload();

const serwist = new Serwist({
  precacheEntries: self.__SW_MANIFEST,
  skipWaiting: true,
  clientsClaim: true,
  navigationPreload: true,
  runtimeCaching: [
    // non-api and non-account GET requests
    {
      matcher: ({ sameOrigin, url: { pathname } }) => sameOrigin && !pathname.startsWith("/api/") && !pathname.startsWith("/account/"),
      handler: new CacheFirst({
        cacheName: "non-api-assets"
      }),
      method: "GET"
    }
  ],
});

serwist.addEventListeners();