export type ApiErrorType = "auth_error" | "api_error" | "json_error"

// TODO better union types
export interface ApiError {
  type: ApiErrorType,
  statusCode: number | undefined,
  exception: any | undefined
}

function isApiError(response: any | ApiError ) : response is ApiError {
  return (response as ApiError).type !== undefined;
}

export function handleApiResponse<T, TResp>(apiResponse: T | ApiError,
  onSuccess: (parsedResponse: T) => TResp,
  onError: (apiError: ApiError) => TResp
) : TResp {
  return isApiError(apiResponse) ? onError(apiResponse) : onSuccess(apiResponse);
}

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

export function getFromLocalStorage<T>(key: string) : T | null {
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

export function setLocalStorage(key: string, value: any): void {
  localStorage.setItem(key, JSON.stringify(value));
}

export async function sendAuthenticatedApiRequest<T>(url: string, method: string, body?: any) : Promise<T | ApiError> {
  // TODO need to handle offline. Main goal is not to break UI.

  const authToken = getFromLocalStorage<string>(authTokenKey);
  if (authToken == null) {
    console.warn("Api auth token is missing. Need to re-login.");
    return {
      type: "auth_error",
      exception: undefined,
      statusCode: undefined
    };
  }
  
  let apiResponse: Response = null!;
  
  try {
    const request: RequestInit = {
      cache: "no-store",
      method: method,
      headers: {
        "Authorization": `bearer ${authToken}`,
        "Content-Type": "application/json; charset=utf-8"
      },
      body: body != null ? JSON.stringify(body) : undefined
    };
    
    apiResponse = await fetch(`/api/${url}`, request);
  } catch (apiException) {
    console.log(apiException);
    
    return {
      type: "api_error",
      exception: apiException,
      statusCode: undefined
    }
  }

  if (apiResponse.status == 200) {
    try {
      return await apiResponse.json() as T;
    } catch (jsonException) {
      return {
        type: "json_error",
        exception: jsonException,
        statusCode: apiResponse.status
      }
    }
  } else if (apiResponse.status == 401) {
    console.error("Got 401 API status code. Need to re-login");
    setLocalStorage(authToken, null);

    // TODO refresh token and try again. then signal API if still 401

    return {
      type: "auth_error",
      exception: undefined,
      statusCode: apiResponse.status
    };
  }

  console.error(`Failed to retrieve data. status: ${apiResponse.status}`);
  
  return {
    type: "api_error",
    exception: undefined,
    statusCode: apiResponse.status
  };
}