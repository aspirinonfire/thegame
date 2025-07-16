export interface apiError {
  title: string;
  status: number;
  detail: string | null;
  GameRequestCorrelationId: string;
  traceId: string;
  errors?: {
    [key: string]: string[];
  };
}

export function isApiError<TResponse>(result: TResponse | apiError): result is apiError {
  return typeof(result as apiError).status === 'number' && (result as apiError).status >= 400;
}