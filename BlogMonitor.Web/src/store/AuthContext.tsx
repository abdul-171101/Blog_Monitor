import React, { createContext, useContext, useState, useEffect } from 'react'
import type { ReactNode } from 'react'
import type { LoginResponse } from '../types'
import { setAccessToken } from '../services/apiClient'
import { authService } from '../services/authService'

export interface AuthState {
  isAuthenticated: boolean
  role: string | null
  userEmail: string | null
}

export interface AuthContextType {
  authState: AuthState
  login: (data: LoginResponse, email?: string | null) => void
  logout: () => void
  isRestoring: boolean
}

const SESSION_KEY = 'bm_auth'

const AuthContext = createContext<AuthContextType | undefined>(undefined)

export const AuthProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
  const [isRestoring, setIsRestoring] = useState(true)
  const [authState, setAuthState] = useState<AuthState>({
    isAuthenticated: false,
    role: null,
    userEmail: null,
  })

  // On mount: try to restore session via refresh token cookie
  useEffect(() => {
    const stored = sessionStorage.getItem(SESSION_KEY)
    if (stored) {
      try {
        const parsed = JSON.parse(stored) as { role: string | null; userEmail: string | null }
        // Attempt silent token refresh using the HttpOnly cookie
        authService.refresh()
          .then((res) => {
            setAccessToken(res.accessToken)
            setAuthState({ isAuthenticated: true, role: parsed.role, userEmail: parsed.userEmail })
          })
          .catch(() => {
            sessionStorage.removeItem(SESSION_KEY)
          })
          .finally(() => setIsRestoring(false))
      } catch {
        sessionStorage.removeItem(SESSION_KEY)
        setIsRestoring(false)
      }
    } else {
      setIsRestoring(false)
    }
  }, [])

  const login = (data: LoginResponse, email: string | null = null) => {
    setAccessToken(data.accessToken)
    const state = { isAuthenticated: true, role: data.role ?? null, userEmail: email }
    setAuthState(state)
    sessionStorage.setItem(SESSION_KEY, JSON.stringify({ role: data.role ?? null, userEmail: email }))
  }

  const logout = async () => {
    try {
      await authService.logout()
    } catch {
      // ignore
    }
    setAccessToken(null)
    sessionStorage.removeItem(SESSION_KEY)
    setAuthState({ isAuthenticated: false, role: null, userEmail: null })
  }

  return (
    <AuthContext.Provider value={{ authState, login, logout, isRestoring }}>
      {children}
    </AuthContext.Provider>
  )
}

export const useAuth = (): AuthContextType => {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used within AuthProvider')
  return ctx
}

export default AuthContext
