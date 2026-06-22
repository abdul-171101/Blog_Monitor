import React from 'react'
import { Navigate, Outlet } from 'react-router-dom'
import { useAuth } from '../store/AuthContext'

const ProtectedRoute: React.FC<{ requiredRole?: string }> = ({ requiredRole }) => {
  const { authState } = useAuth()

  if (!authState.isAuthenticated) return <Navigate to="/login" replace />
  if (requiredRole && authState.role !== requiredRole) return <Navigate to="/dashboard" replace />
  return <Outlet />
}

export default ProtectedRoute
