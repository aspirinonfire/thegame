import { Button, Card } from "flowbite-react";
import { type ReactNode, Component } from "react";

type ErrorBoundaryProps = {
  children: ReactNode;
};
type ErrorBoundaryState = {
  hasError: boolean;
  error?: Error;
};

class ErrorBoundary extends Component<ErrorBoundaryProps, ErrorBoundaryState> {
  constructor(props: ErrorBoundaryProps) {
    super(props);
    this.state = { hasError: false };
  }

  componentDidCatch(error: Error, errorInfo: React.ErrorInfo) {
    // TODO log errors
    console.error(error, errorInfo);

    this.setState({ hasError: true, error});
  }

  render(): React.ReactNode {
    if (!this.state.hasError) {
      return this.props.children;
    }

    return <div className="cdnext-full-content flex gap-5 flex-col justify-start">
      <Card className="w-auto">
        <h5 className="tracking-tight text-gray-600">Something went wrong! Please refresh the page and try again.</h5>
        <Button size="sm" onClick={() => window.location.href = '/'}>
          Go back to Dashboard
        </Button>
      </Card>
    </div>;
  }
}

export default ErrorBoundary;