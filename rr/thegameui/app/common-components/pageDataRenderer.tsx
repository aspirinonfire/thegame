import React from "react";
import LoadingWidget from "./loading";

type PageDataRendererProps = {
  loading: boolean;
  error: unknown;
  data: unknown;
  children: React.ReactNode;
  // optional overrides
  loadingFallback?: React.ReactNode;
  errorFallback?: (error: unknown) => React.ReactNode;
  noDataFallback?: React.ReactNode;
};

export function PageDataRenderer({
  loading,
  error,
  data,
  children,
  loadingFallback,
  errorFallback,
  noDataFallback,
}: PageDataRendererProps) {
  if (loading) {
    return <>{loadingFallback ?? <LoadingWidget/> }</>;
  }
  if (error) {
    return <>{errorFallback ? errorFallback(error) : <p>Failed to load page data - {String(error)}</p>}</>;
  }
  if (data === null || data === undefined) {
    return <>{noDataFallback ?? <p>No Data</p>}</>;
  }
  return <>{children}</>;
}