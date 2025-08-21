import type { StateCreator } from "zustand";
import type { ApiError } from "~/appState/ApiError";
import type { AppStore } from "./AppStore";

export interface AppApiSlice {
  apiErrors: ApiError[];

  enqueueError: (apiError: ApiError) => void;
  dequeueError: () => ApiError | null;

  sendAuthenticatedRequest: <TResponse>(
    endpoint: string,
    method: string,
    body: any | null,
    includeCreds: boolean,
    onErrorCallback?: (response: Response) => Promise<void>) => Promise<TResponse | ApiError>;
  
  sendUnauthenticatedRequest: <TResponse>(
    url: string,
    method: string,
    body: any | null,
    includeCreds: boolean,
    onErrorCallback?: (response: Response) => Promise<void>) => Promise<TResponse | ApiError>;
  
  apiGet: <TResponse>(endpoint: string) => Promise<TResponse | ApiError>;
  
  apiPost: <TResponse>(endpoint: string, body?: any) => Promise<TResponse | ApiError>;
}

export const createAppApiSlice: StateCreator<AppStore, [], [], AppApiSlice> = (set, get) => ({
  apiErrors: [],
  enqueueError: (apiError: ApiError) => {
    set((s) => ({ apiErrors: [...s.apiErrors, apiError] }));
  },

  dequeueError: () => {
    const allErrors = get().apiErrors;

    set((s) => ({ apiErrors: allErrors.slice(1) }));

    return allErrors[0];
  },

  sendUnauthenticatedRequest: async <TBody, TResponse>(
    endpoint: string,
    method: string,
    body: TBody | null,
    includeCreds: boolean,
    onErrorCallback?: (response: Response) => Promise<void>
  ) => {
    const normalizedEndpointUrl = (endpoint || "").replace(/^\//, "");
    const apiResponse = await fetch(`${import.meta.env.VITE_API_URL}/api/${normalizedEndpointUrl}`, {
      cache: "no-cache",
      method: method,
      body: body ? JSON.stringify(body) : null,
      headers: {
        "Content-Type": "application/json; charset=utf-8",
      },
      credentials: includeCreds ? "include" : undefined
    });

    if (apiResponse.ok) {
      // Parse the response JSON into the expected TResponse type
      const data: TResponse = await apiResponse.json();
      return data;
    }

    const errorData: ApiError = {
      status: apiResponse.status,
      title: 'Failed to send request.',
      detail: await apiResponse.text(),
      GameRequestCorrelationId: "",
      traceId: ""
    };

    if (onErrorCallback) {
      await onErrorCallback(apiResponse);
    } else {
      get().enqueueError(errorData);
    }

    return errorData;
  },

  sendAuthenticatedRequest: async <TBody, TResponse>(
    endpoint: string,
    method: string,
    body: TBody | null,
    includeCreds: boolean,
    onErrorCallback?: (response: Response) => Promise<void>
  ) => {
    let accessToken = await get().retrieveAccessToken();

    if (!accessToken) {
      const errorData: ApiError = {
        status: 401,
        title: "Failed to retrieve Access Token.",
        detail: "Please contact IT Support for assistance.",
        GameRequestCorrelationId: "",
        traceId: ""
      };

      if (get().activeUser.isAuthenticated) {
        get().enqueueError(errorData);
      }

      return errorData;
    }

    const makeRequest = async (bearerToken: string) => {
      const normalizedEndpointUrl = (endpoint || "").replace(/^\//, "");
      const apiResponse = await fetch(`${import.meta.env.VITE_API_URL}/api/${normalizedEndpointUrl}`, {
        cache: "no-cache",
        method: method,
        headers: {
          "Authorization": `Bearer ${bearerToken}`,
          "Content-Type": "application/json; charset=utf-8",
        },
        body: body ? JSON.stringify(body) : null,
        credentials: includeCreds ? "include" : undefined
      });

      return apiResponse;
    };

    let apiResponse = await makeRequest(accessToken);

    if (apiResponse.status == 401) {
      accessToken = await get().refreshAccessToken();

      if (!accessToken) {
        const errorData: ApiError = {
          status: 401,
          title: 'Failed to retrieve Access Token.',
          detail: 'Please contact IT Support for assistance.',
          GameRequestCorrelationId: "",
          traceId: ""
        };

        if (onErrorCallback) {
          await onErrorCallback(apiResponse);
        } else {
          get().enqueueError(errorData);
        }

        return errorData;
      }

      apiResponse = await makeRequest(accessToken);
    }

    if (apiResponse.ok) {
      // Parse the response JSON into the expected TResponse type
      const data: TResponse = await apiResponse.json();
      return data;
    }

    // API errors return standard rfc9110 payload
    const errorData: ApiError = await apiResponse.json();
    
    if (onErrorCallback) {
      await onErrorCallback(apiResponse);
    } else {
      get().enqueueError(errorData);
    }
    
    return errorData;
  },

  apiGet: async <TResponse>(endpoint: string) => await get().sendAuthenticatedRequest<TResponse>(endpoint, "get", null, false),

  apiPost: async <TBody, TResponse>(endpoint: string, body: TBody) => await get().sendAuthenticatedRequest<TResponse>(endpoint, "post", body, false)
});

