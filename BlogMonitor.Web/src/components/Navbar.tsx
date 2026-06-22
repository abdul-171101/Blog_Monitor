import React from 'react'
import { NavLink, useNavigate } from 'react-router-dom'
import { useAuth } from '../store/AuthContext'

const NavItem: React.FC<{ to: string; children: React.ReactNode }> = ({ to, children }) => (
  <NavLink
    to={to}
    className={({ isActive }) =>
      `px-3 py-2 rounded text-sm ${isActive ? 'text-white bg-blue-700 border-b-2 border-white' : 'text-gray-300'}`
    }
  >
    {children}
  </NavLink>
)

const RoleBadge: React.FC<{ role: string | null }> = ({ role }) => {
  if (!role) return null
  const cls = role === 'Admin' ? 'bg-green-100 text-green-800' : 'bg-blue-100 text-blue-800'
  return <span className={`text-xs px-2 py-1 rounded ${cls}`}>{role}</span>
}

const Navbar: React.FC = () => {
  const { authState, logout } = useAuth()
  const navigate = useNavigate()

  const handleLogout = async () => {
    await logout()
    navigate('/login')
  }

  return (
    <header className="bg-blue-800 text-white">
      <div className="max-w-7xl mx-auto px-4 py-3 flex items-center justify-between">
        <div className="flex items-center gap-6">
          <div className="text-lg font-bold">Blog Monitor</div>
          <nav className="flex items-center gap-2">
            <NavItem to="/dashboard">Dashboard</NavItem>
            <NavItem to="/urls">URLs</NavItem>
            <NavItem to="/reports">Reports</NavItem>
            {authState.role === 'Admin' && <NavItem to="/logs">Logs</NavItem>}
            {authState.role === 'Admin' && <NavItem to="/clients">Clients</NavItem>}
          </nav>
        </div>
        <div className="flex items-center gap-4">
          <div className="flex items-center gap-2">
            <div className="text-sm text-white/90">{authState.userEmail ?? ''}</div>
            <RoleBadge role={authState.role} />
          </div>
          <button onClick={handleLogout} className="bg-white/10 hover:bg-white/20 px-3 py-1 rounded text-sm">
            Logout
          </button>
        </div>
      </div>
    </header>
  )
}

export default Navbar
