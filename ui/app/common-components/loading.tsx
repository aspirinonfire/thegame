import { Spinner } from "flowbite-react"

const LoadingWidget = () => {
  return <div className="flex flex-col gap-5 items-center justify-center p-5 min-h-[calc(100vh-20rem)] min-w-[calc(100dvw-5dvw)]">
    <Spinner className="h-50 w-50 fill-primary-900" />
  </div>
}

export default LoadingWidget;