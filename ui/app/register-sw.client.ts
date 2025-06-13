import { registerSW } from 'virtual:pwa-register'

if (import.meta.env.PROD) {
  registerSW({
    immediate: true,   // auto-register as soon as the module is evaluated
    onNeedRefresh()  {/* optional UI */},
    onOfflineReady() {/* optional UI */}
  });
}