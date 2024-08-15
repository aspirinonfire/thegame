interface ApiError {
  type: "api_error",
  statusCode: number | undefined,
  exception: any | undefined
}

interface AuthError {
  type: "auth_error",
  error: string
}

interface SerializationError {
  type: "json_error",
  error: any | undefined,
  statusCode: number | undefined
}

export interface ApiTokenResponse {
  accessToken: string
}

export type GameApiError = ApiError | AuthError | SerializationError;

function isApiError(response: any | GameApiError ) : response is ApiError {
  return (response as ApiError).type === "api_error";
}

function isAuthError(response: any | GameApiError ) : response is AuthError {
  return (response as AuthError).type === "auth_error";
}

function isJsonError(response: any | GameApiError ) : response is SerializationError {
  return (response as SerializationError).type === "json_error";
}

export function handleApiResponse<T, TResp>(apiResponse: T | GameApiError,
  onSuccess: (parsedResponse: T) => TResp,
  onError: (apiError: GameApiError) => TResp
) : TResp {
  if (isApiError(apiResponse) || isAuthError(apiResponse) || isJsonError(apiResponse)) {
    return onError(apiResponse);
  } else {
    return onSuccess(apiResponse);
  }
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

export async function sendAuthenticatedApiRequest<T>(url: string,
  method: "GET" | "POST",
  body?: any,
  skipTokenRefreshOn401?: boolean) : Promise<T | GameApiError> {
    // TODO need to handle offline. Main goal is not to break UI.

    const authToken = getFromLocalStorage<string>(authTokenKey);
    if (authToken == null) {
      console.warn("Api auth token is missing. Need to re-login.");
      return {
        type: "auth_error",
        error: "missing_token"
      };
    }

    const request: RequestInit = {
      cache: "no-store",
      method: method,
      headers: {
        "Authorization": `bearer ${authToken}`,
        "Content-Type": "application/json; charset=utf-8"
      },
      body: body != null ? JSON.stringify(body) : undefined
    };
    
    let apiResponse: Response = null!;
    
    try {
      apiResponse = await fetch(`/api/${url}`, request);
    } catch (apiException) {
      console.log(apiException);
      
      return {
        type: "api_error",
        exception: apiException,
        statusCode: undefined
      }
    }

    if (apiResponse.ok) {
      try {
        return await apiResponse.json() as T;
      } catch (jsonException) {
        return {
          type: "json_error",
          error: jsonException,
          statusCode: apiResponse.status
        }
      }
    } else if (apiResponse.status == 401 && !skipTokenRefreshOn401) {
      // Refresh token and try again
      const refreshResult = await sendAuthenticatedApiRequest<ApiTokenResponse>("user/refresh-token",
        "POST",
        true);

      const shouldRetryOriginalRequest = handleApiResponse(refreshResult,
        newToken => {
          setLocalStorage(authTokenKey, newToken.accessToken);
          return true;
        },
        error => {
          console.error(`Failed to refresh access token: ${error}`);
          setLocalStorage(authTokenKey, null);
          return false;
        }
      );

      if (shouldRetryOriginalRequest) {
        return await sendAuthenticatedApiRequest<T>(url, method, body, true);
      }
    }

    console.error(`Failed to retrieve data. status: ${apiResponse.status}`);
    
    return {
      type: "api_error",
      exception: undefined,
      statusCode: apiResponse.status
    };
}