import { Component } from "react";
import type { ErrorInfo, ReactNode } from "react";

interface Props {
  children: ReactNode;
}

interface State {
  hasError: boolean;
  error: Error | null;
  errorInfo: ErrorInfo | null;
}

export class ErrorBoundary extends Component<Props, State> {
  constructor(props: Props) {
    super(props);
    this.state = { hasError: false, error: null, errorInfo: null };
  }

  static getDerivedStateFromError(error: Error): Partial<State> {
    return { hasError: true, error };
  }

  componentDidCatch(error: Error, errorInfo: ErrorInfo): void {
    this.setState({ errorInfo });
    // Log error to console in development
    console.error("ErrorBoundary caught an error:", error, errorInfo);
  }

  handleReload = (): void => {
    window.location.reload();
  };

  handleGoHome = (): void => {
    window.location.href = "/";
  };

  render(): ReactNode {
    if (this.state.hasError) {
      return (
        <div
          dir="rtl"
          className="min-h-screen bg-gray-100 flex items-center justify-center p-4 font-sans"
        >
          <div className="bg-white rounded-lg shadow-lg p-8 max-w-lg w-full text-center">
            <div className="text-red-500 text-6xl mb-4">⚠️</div>
            <h1 className="text-2xl font-bold text-gray-800 mb-2">
              حدث خطأ غير متوقع
            </h1>
            <p className="text-gray-600 mb-6">
              نعتذر عن هذا الخطأ. يرجى تحديث الصفحة أو العودة للصفحة الرئيسية.
            </p>

            {import.meta.env.DEV && this.state.error && (
              <div className="bg-red-50 border border-red-200 rounded p-4 mb-6 text-left overflow-auto max-h-48">
                <p className="text-red-800 font-sans text-sm">
                  {this.state.error.toString()}
                </p>
                {this.state.errorInfo && (
                  <pre className="text-red-600 font-sans text-xs mt-2 whitespace-pre-wrap">
                    {this.state.errorInfo.componentStack}
                  </pre>
                )}
              </div>
            )}

            <div className="flex gap-4 justify-center">
              <button
                onClick={this.handleReload}
                className="bg-blue-600 text-white px-6 py-2 rounded-lg hover:bg-blue-700 transition-colors"
              >
                تحديث الصفحة
              </button>
              <button
                onClick={this.handleGoHome}
                className="bg-gray-200 text-gray-800 px-6 py-2 rounded-lg hover:bg-gray-300 transition-colors"
              >
                الصفحة الرئيسية
              </button>
            </div>
          </div>
        </div>
      );
    }

    return this.props.children;
  }
}
