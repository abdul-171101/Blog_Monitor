import React from 'react'

interface ErrorBoundaryState {
  hasError: boolean
}

class ErrorBoundary extends React.Component<React.PropsWithChildren<{}>, ErrorBoundaryState> {
  constructor(props: React.PropsWithChildren<{}>) {
    super(props)
    this.state = { hasError: false }
  }

  static getDerivedStateFromError() {
    return { hasError: true }
  }

  componentDidCatch(error: Error, errorInfo: React.ErrorInfo) {
    console.error('Unhandled React error:', error, errorInfo)
  }

  render() {
    if (this.state.hasError) {
      return (
        <div className="min-h-screen flex items-center justify-center bg-gray-100 px-4">
          <div className="max-w-xl w-full bg-white rounded-xl shadow p-8 text-center">
            <h1 className="text-2xl font-semibold mb-3">Something went wrong</h1>
            <p className="text-gray-600 mb-6">An unexpected error occurred while loading the page. Please refresh or try again later.</p>
            <button onClick={() => window.location.reload()} className="inline-flex items-center justify-center rounded bg-blue-600 px-4 py-2 text-white hover:bg-blue-700">
              Reload Page
            </button>
          </div>
        </div>
      )
    }

    return this.props.children
  }
}

export default ErrorBoundary
