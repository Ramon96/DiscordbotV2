import { Outlet } from 'react-router-dom'
import { authApi, type CurrentUser } from '../api'
import Sidebar from './Sidebar'

export interface DashboardOutletContext {
  onLogout: () => void
  user: CurrentUser
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
      <Sidebar user={user} onLogout={handleLogout} />
      <main className="main">
        <div className="main-inner">
          <Outlet context={{ onLogout, user } satisfies DashboardOutletContext} />
        </div>
      </main>
    </div>
  )
}
