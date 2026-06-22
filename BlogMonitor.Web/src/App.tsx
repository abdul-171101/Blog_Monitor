import React from 'react'
import { Routes, Route, Navigate } from 'react-router-dom'
import Login from './pages/Login'
import Register from './pages/Register'
import Dashboard from './pages/Dashboard'
import UrlManager from './pages/UrlManager'
import ReportViewer from './pages/ReportViewer'
import LogViewer from './pages/LogViewer'
import ClientManager from './pages/ClientManager'
import ProtectedRoute from './components/ProtectedRoute'
import Layout from './components/Layout'
import ErrorBoundary from './components/ErrorBoundary'
import NotFound from './components/NotFound'

const App: React.FC = () => {
  return (
    <ErrorBoundary>
      <Routes>
        <Route path="/login" element={<Login />} />
        <Route path="/register" element={<Register />} />
        <Route path="/" element={<Navigate to="/login" replace />} />

        <Route element={<ProtectedRoute />}>
          <Route element={<Layout />}>
            <Route path="/dashboard" element={<Dashboard />} />
            <Route path="/urls" element={<UrlManager />} />
            <Route path="/reports" element={<ReportViewer />} />
          </Route>
        </Route>

        <Route element={<ProtectedRoute requiredRole="Admin" />}>
          <Route element={<Layout />}>
            <Route path="/logs" element={<LogViewer />} />
            <Route path="/clients" element={<ClientManager />} />
          </Route>
        </Route>

        <Route path="*" element={<NotFound />} />
      </Routes>
    </ErrorBoundary>
  )
}

export default App
