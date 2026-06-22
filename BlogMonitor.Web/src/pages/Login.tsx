import React, { useState } from 'react'
import { useNavigate, Link } from 'react-router-dom'
import { authService } from '../services/authService'
import { useAuth } from '../store/AuthContext'
import LoadingSpinner from '../components/LoadingSpinner'

const Login: React.FC = () => {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [isLoading, setIsLoading] = useState(false)
  const { login } = useAuth()
  const navigate = useNavigate()

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError('')
    setIsLoading(true)
    try {
      const res = await authService.login({ email, password })
      login(res, email)
      navigate('/dashboard')
    } catch (err: any) {
      const msg = err?.response?.data?.message || 'Invalid email or password.'
      setError(msg)
    } finally {
      setIsLoading(false)
    }
  }

  return (
    <div className="min-h-screen bg-gray-100 flex items-center justify-center">
      <div className="w-[400px] bg-white rounded shadow p-6">
        <h2 className="text-2xl font-bold">Blog Monitor</h2>
        <p className="text-sm text-gray-500 mb-4">Sign in to your account</p>
        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-sm font-medium">Email</label>
            <input value={email} onChange={(e) => setEmail(e.target.value)} type="email" className="mt-1 w-full border px-3 py-2 rounded" required />
          </div>
          <div>
            <label className="block text-sm font-medium">Password</label>
            <input value={password} onChange={(e) => setPassword(e.target.value)} type="password" className="mt-1 w-full border px-3 py-2 rounded" required />
          </div>
          <div>
            <button disabled={isLoading} className="w-full bg-blue-600 text-white px-3 py-2 rounded flex items-center justify-center">
              {isLoading ? <><LoadingSpinner size="small" /> <span className="ml-2">Signing in...</span></> : 'Sign In'}
            </button>
          </div>
          {error && <p className="text-sm text-red-600">{error}</p>}
        </form>
        <p className="mt-4 text-sm">Don't have an account? <Link to="/register" className="text-blue-600">Register</Link></p>
      </div>
    </div>
  )
}

export default Login
