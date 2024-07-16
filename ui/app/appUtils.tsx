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

export const authTokenKey: string = "authToken";

export function GetFromLocalStorage<T>(key: string) : T | null {
  const rawValue = localStorage.getItem(key);
  if (!rawValue) {
    return null;
  }

  try {
    return JSON.parse(rawValue) as T;
  }
  catch (jsonException) {
    console.error(`Failed to retrieve ${key} from local storage:`, jsonException);
    return null;
  }
}

export function SetLocalStorage(key: string, value: any): void {
  localStorage.setItem(key, JSON.stringify(value));
}

export async function SendApiRequest<T>(url: string, method: string, body?: any) : Promise<T | null> {
    // TODO validate auth token before making api requests
    const authToken = GetFromLocalStorage<string>(authTokenKey);
    if (authToken == null) {
      console.warn("Api auth token is missing. Need to re-login.");
      return null;
    }
  
  const userDataResponse = await fetch(url, {
    cache: "no-store",
    method: method,
    headers: {
      "Authorization": `bearer ${authToken}`,
      "Content-Type": "application/json; charset=utf-8"
    },
    body: body != null ? JSON.stringify(body) : undefined
  });

  // TODO handle offline, 401, 400, 500 separately!
  if (userDataResponse.status == 200) {
    return await userDataResponse.json() as T;
  } else if (userDataResponse.status == 401) {
    console.error("Got 401 API status code. Need to re-login");
    SetLocalStorage(authToken, null);
    return null;
  }

  console.error("Failed to retrieve data.");
  return null;
}