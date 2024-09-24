import type { PrecacheEntry, SerwistGlobalConfig } from "serwist";
import { Serwist, NetworkOnly } from "serwist";

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

// ensure manual pre-cache gets evicted on new service worker
const manualPrecacheRevision = process.env.NEXT_PUBLIC_BUILD_ID || `${new Date().getTime()}`;

const serwist = new Serwist({
  // A list of URLs that should be cached. Usually, you don't generate
  // this list yourself; rather, you'd rely on a Serwist build tool/your framework
  // to do it for you. In this example, it is generated by `@serwist/next`.
  precacheEntries: self.__SW_MANIFEST?.concat([
    // These urls are required for a fully working offline ux however, they are not included during sw build by default.
    // Additionally, additionalPrecacheEntries cannot be used because of an issue that overrides public asset caching,
    // see https://github.com/serwist/serwist/issues/139
    { url: "/favicon.ico", revision: manualPrecacheRevision },
    { url: "/index.html", revision: manualPrecacheRevision },
    { url: "/index.txt", revision: manualPrecacheRevision },
    { url: "/game/index.html", revision: manualPrecacheRevision },
    { url: "/game/index.txt", revision: manualPrecacheRevision },
    { url: "/history/index.html", revision: manualPrecacheRevision },
    { url: "/history/index.txt", revision: manualPrecacheRevision },
    { url: "/about/index.html", revision: manualPrecacheRevision },
    { url: "/about/index.txt", revision: manualPrecacheRevision },
  ]),
  // Options to customize how Serwist precaches the URLs.
  precacheOptions: {
    // Whether outdated caches should be removed.
    cacheName: 'pre-game',
    cleanupOutdatedCaches: true,
    concurrency: 10,
    ignoreURLParametersMatching: []
  },
  // Whether the service worker should skip waiting and become the active one.
  skipWaiting: true,
  // Whether the service worker should claim any currently available clients.
  clientsClaim: true,
  navigationPreload: true,
  // A list of runtime caching entries. When a request is made and its URL match
  // any of the entries, the response to it will be cached according to the matching
  // entry's `handler`. This does not apply to precached URLs.
  runtimeCaching: [
    {
      // non-precached requests should always go through network
      matcher: /.*/i,
      handler: new NetworkOnly()
    }
  ],
});

serwist.addEventListeners();