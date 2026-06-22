import React, { createContext, useContext, useState } from 'react'
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
}

const AuthContext = createContext<AuthContextType | undefined>(undefined)

export const AuthProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
  const [authState, setAuthState] = useState<AuthState>({
    isAuthenticated: false,
    role: null,
    userEmail: null,
  })

  const login = (data: LoginResponse, email: string | null = null) => {
    setAccessToken(data.accessToken)
    setAuthState({ isAuthenticated: true, role: data.role ?? null, userEmail: email })
  }

  const logout = async () => {
    try {
      await authService.logout()
    } catch {
      // ignore
    }
    setAccessToken(null)
    setAuthState({ isAuthenticated: false, role: null, userEmail: null })
  }

  return <AuthContext.Provider value={{ authState, login, logout }}>{children}</AuthContext.Provider>
}

export const useAuth = (): AuthContextType => {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used within AuthProvider')
  return ctx
}

export default AuthContext
