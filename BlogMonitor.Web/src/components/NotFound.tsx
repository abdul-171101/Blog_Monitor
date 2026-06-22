import React from 'react'
import { Link } from 'react-router-dom'

const NotFound: React.FC = () => (
  <div className="min-h-screen bg-gray-100 flex items-center justify-center px-4 py-10">
    <div className="max-w-2xl w-full bg-white rounded-xl shadow p-8 text-center">
      <h1 className="text-4xl font-bold mb-4">404</h1>
      <p className="text-lg text-gray-700 mb-6">The page you are looking for does not exist.</p>
      <div className="flex flex-col sm:flex-row justify-center gap-3">
        <Link to="/dashboard" className="inline-flex items-center justify-center rounded bg-blue-600 px-4 py-2 text-white hover:bg-blue-700">
          Return to Dashboard
        </Link>
        <Link to="/reports" className="inline-flex items-center justify-center rounded border border-gray-300 bg-white px-4 py-2 text-gray-700 hover:bg-gray-50">
          View Reports
        </Link>
      </div>
    </div>
  </div>
)

export default NotFound
