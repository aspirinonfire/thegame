export default async function ResolveWithMinimumDelay<T>(
  promise: Promise<T>,
  delayMs: number = 250) : Promise<T>
{
  const delayPromise = new Promise(resolve => setTimeout(resolve, delayMs));
  const [resolvedMainPromiseResult] = await Promise.all([promise, delayPromise]);
  
  return resolvedMainPromiseResult;
}