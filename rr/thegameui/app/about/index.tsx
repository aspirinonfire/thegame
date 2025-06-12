import type { Route } from "./+types";
import { Card } from "flowbite-react";
import { usePromiseLoader } from "~/common-components/usePromiseLoader";
import { PageDataRenderer } from "~/common-components/pageDataRenderer";
import { useAppStore } from "~/useAppStore";

export async function clientLoader({
  params,
}: Route.ClientLoaderArgs) {
  const dataPromise = new Promise<string>((resolve) => setTimeout(() => resolve("This is a test message!"), 1000));
  return {
    dataPromise
  };
}

export default function AboutPage({
  loaderData,
} : Route.ComponentProps) {
  const { data, error, loading } = usePromiseLoader<string>(loaderData.dataPromise);
  const userAccount = useAppStore(state => state.activeUser);

  return <PageDataRenderer data={data} error={error} loading={loading}>
    <Card className="bg-gray-600">
      <h1>About</h1>
      <p>Hello, {userAccount?.name}!</p>
      <p className="text-sm">{data}</p>
    </Card>
  </PageDataRenderer>
}