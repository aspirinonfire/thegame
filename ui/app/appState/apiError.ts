export interface ApiError {
  title: string;
  status: number;
  detail: string | null;
  GameRequestCorrelationId: string;
  traceId: string;
  errors?: {
    [key: string]: string[];
  };
}

export function isApiError<TResponse>(result: TResponse | ApiError): result is ApiError {
  return typeof(result as ApiError).status === 'number' && (result as ApiError).status >= 400;
}