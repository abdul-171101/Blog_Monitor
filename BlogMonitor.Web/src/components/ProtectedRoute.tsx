import React from 'react'
import { Navigate, Outlet } from 'react-router-dom'
import { useAuth } from '../store/AuthContext'

const ProtectedRoute: React.FC<{ requiredRole?: string }> = ({ requiredRole }) => {
  const { authState, isRestoring } = useAuth()

  if (isRestoring) return <div className="flex items-center justify-center h-screen text-gray-500">Loading...</div>
  if (!authState.isAuthenticated) return <Navigate to="/login" replace />
  if (requiredRole && authState.role !== requiredRole) return <Navigate to="/dashboard" replace />
  return <Outlet />
}

export default ProtectedRoute
