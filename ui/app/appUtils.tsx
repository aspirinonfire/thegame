export default function refreshOnNewVersion() {
  if (!('serviceWorker' in navigator)) {
    return;
  }

  navigator.serviceWorker.ready.then(registration => {
    registration.onupdatefound = () => {
      const installingWorker = registration.installing;
      if (!installingWorker) {
        return;
      }

      installingWorker.onstatechange = () => {
        if (installingWorker.state !== 'installed') {
          return;
        }

        if (!navigator.serviceWorker.controller) {
          console.log('Content is cached for offline use.');
          return;
        }

        // New content is available; refresh the page automatically
        console.log('New content is available; refreshing...');
        window.location.reload();
      };
    };
  }).catch(error => {
    console.log('SW registration failed: ', error);
  });
}