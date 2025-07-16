import type { StateCreator } from "zustand";
import type { apiError } from "~/appState/apiError";
import type { AppStore } from "./AppStore";

export interface ApiSlice {
  apiErrors: apiError[];

  enqueueError: (apiError: apiError) => void;
  dequeueError: () => apiError | null;

  sendAuthenticatedRequest: <TBody, TResponse>(endpoint: string, method: string, body: TBody | null, includeCreds: boolean) => Promise<TResponse | apiError>;
  sendUnauthenticatedRequest: <TBody, TResponse>(url: string, method: string, body: TBody | null, includeCreds: boolean) => Promise<TResponse | apiError>;
  get: <TResponse>(endpoint: string) => Promise<TResponse | apiError>;
  post: <TResponse, TBody = void>(endpoint: string, body?: TBody) => Promise<TResponse | apiError>;
}

export const createApiSlice: StateCreator<AppStore, [], [], ApiSlice> = (set, get) => ({
  apiErrors: [],
  enqueueError: (apiError: apiError) => {
    set((s) => ({ apiErrors: [...s.apiErrors, apiError] }));
  },

  dequeueError: () => {
    const allErrors = get().apiErrors;

    set((s) => ({ apiErrors: allErrors.slice(1) }));

    return allErrors[0];
  },

  sendUnauthenticatedRequest: async <TBody, TResponse>(endpoint: string, method: string, body: TBody | null, includeCreds: boolean) => {
    const normalizedEndpointUrl = (endpoint || '').replace(/^\//, '');
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

    const errorData: apiError = {
      status: apiResponse.status,
      title: 'Failed to send request.',
      detail: await apiResponse.text(),
      GameRequestCorrelationId: '',
      traceId: ''
    };

    get().enqueueError(errorData);
    return errorData;
  },

  sendAuthenticatedRequest: async <TBody, TResponse>(endpoint: string, method: string, body: TBody | null, includeCreds: boolean) => {
    let accessToken = await get().retrieveAccessToken();

    if (!accessToken) {
      const errorData: apiError = {
        status: 401,
        title: 'Failed to retrieve Access Token.',
        detail: 'Please contact IT Support for assistance.',
        GameRequestCorrelationId: '',
        traceId: ''
      };

      get().enqueueError(errorData);

      return errorData;
    }

    const makeRequest = async (bearerToken: string) => {
      const normalizedEndpointUrl = (endpoint || '').replace(/^\//, '');
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
        const errorData: apiError = {
          status: 401,
          title: 'Failed to retrieve Access Token.',
          detail: 'Please contact IT Support for assistance.',
          GameRequestCorrelationId: '',
          traceId: ''
        };

        get().enqueueError(errorData);

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
    const errorData: apiError = await apiResponse.json();
    get().enqueueError(errorData);
    return errorData;
  },

  get: async <TResponse>(endpoint: string) => await get().sendAuthenticatedRequest<unknown, TResponse>(endpoint, "get", null, false),

  post: async <TBody, TResponse>(endpoint: string, body: TBody) => await get().sendAuthenticatedRequest<TBody, TResponse>(endpoint, "post", body, false)
});

