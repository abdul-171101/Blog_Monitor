import React, { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { authService } from '../services/authService'
import { useAuth } from '../store/AuthContext'
import LoadingSpinner from '../components/LoadingSpinner'

const Register: React.FC = () => {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [confirm, setConfirm] = useState('')
  const [role, setRole] = useState('Viewer')
  const [errors, setErrors] = useState<Record<string, string>>({})
  const [isLoading, setIsLoading] = useState(false)
  const { login } = useAuth()
  const navigate = useNavigate()

  const validate = () => {
    const e: Record<string, string> = {}
    if (!email) e.email = 'Email is required.'
    if (password.length < 8) e.password = 'Password must be at least 8 characters.'
    if (password !== confirm) e.confirm = 'Passwords do not match.'
    setErrors(e)
    return Object.keys(e).length === 0
  }

  const handleSubmit = async (ev: React.FormEvent) => {
    ev.preventDefault()
    if (!validate()) return
    setIsLoading(true)
    try {
      await authService.register({ email, password, role })
      const res = await authService.login({ email, password })
      login(res, email)
      navigate('/dashboard')
    } catch (err: any) {
      setErrors({ form: err?.response?.data?.message || 'Registration failed.' })
    } finally {
      setIsLoading(false)
    }
  }

  return (
    <div className="min-h-screen bg-gray-100 flex items-center justify-center">
      <div className="w-[400px] bg-white rounded shadow p-6">
        <h2 className="text-2xl font-bold">Blog Monitor</h2>
        <p className="text-sm text-gray-500 mb-4">Create your account</p>
        <form onSubmit={handleSubmit} className="space-y-3">
          <div>
            <label className="block text-sm font-medium">Email</label>
            <input value={email} onChange={(e) => setEmail(e.target.value)} type="email" className="mt-1 w-full border px-3 py-2 rounded" />
            {errors.email && <div className="text-sm text-red-600">{errors.email}</div>}
          </div>
          <div>
            <label className="block text-sm font-medium">Password</label>
            <input value={password} onChange={(e) => setPassword(e.target.value)} type="password" className="mt-1 w-full border px-3 py-2 rounded" />
            {errors.password && <div className="text-sm text-red-600">{errors.password}</div>}
          </div>
          <div>
            <label className="block text-sm font-medium">Confirm Password</label>
            <input value={confirm} onChange={(e) => setConfirm(e.target.value)} type="password" className="mt-1 w-full border px-3 py-2 rounded" />
            {errors.confirm && <div className="text-sm text-red-600">{errors.confirm}</div>}
          </div>
          <div>
            <label className="block text-sm font-medium">Role</label>
            <select value={role} onChange={(e) => setRole(e.target.value)} className="mt-1 w-full border px-3 py-2 rounded">
              <option>Admin</option>
              <option>Viewer</option>
            </select>
          </div>
          <div>
            <button disabled={isLoading} className="w-full bg-blue-600 text-white px-3 py-2 rounded flex items-center justify-center">
              {isLoading ? <><LoadingSpinner size="small" /> <span className="ml-2">Registering...</span></> : 'Register'}
            </button>
          </div>
          {errors.form && <div className="text-sm text-red-600">{errors.form}</div>}
        </form>
        <p className="mt-4 text-sm">Already have an account? <Link to="/login" className="text-blue-600">Sign in</Link></p>
      </div>
    </div>
  )
}

export default Register
