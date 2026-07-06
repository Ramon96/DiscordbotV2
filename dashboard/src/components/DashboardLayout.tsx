import { NavLink, Outlet } from 'react-router-dom'
import { authApi, type CurrentUser } from '../api'

export interface DashboardOutletContext {
  onLogout: () => void
}

interface DashboardLayoutProps {
  user: CurrentUser
  onLogout: () => void
}

export default function DashboardLayout({ user, onLogout }: DashboardLayoutProps) {
  async function handleLogout() {
    try {
      await authApi.logout()
    } finally {
      onLogout()
    }
  }

  return (
    <div className="shell">
      <header className="topbar">
        <span className="brand">GLaDOS Dashboard</span>
        <nav className="nav">
          <NavLink to="/" end className={({ isActive }) => (isActive ? 'active' : '')}>
            Overview
          </NavLink>
          <NavLink to="/players" className={({ isActive }) => (isActive ? 'active' : '')}>
            Players
          </NavLink>
          <NavLink to="/jobs" className={({ isActive }) => (isActive ? 'active' : '')}>
            Jobs
          </NavLink>
          <NavLink to="/logs" className={({ isActive }) => (isActive ? 'active' : '')}>
            Logs
          </NavLink>
        </nav>
        <div className="spacer" />
        <span className="muted">{user.username}</span>
        <button className="ghost" onClick={handleLogout}>
          Log out
        </button>
      </header>

      <main className="content wide">
        <Outlet context={{ onLogout } satisfies DashboardOutletContext} />
      </main>
    </div>
  )
}
