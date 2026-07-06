import { authApi, type CurrentUser } from '../api'

interface DashboardPageProps {
  user: CurrentUser
  onLogout: () => void
}

export default function DashboardPage({ user, onLogout }: DashboardPageProps) {
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
        <div className="spacer" />
        <span className="muted">{user.username}</span>
        <button className="ghost" onClick={handleLogout}>
          Log out
        </button>
      </header>

      <main className="content">
        <div className="card">
          <h2>Welcome, {user.username}</h2>
          <p className="muted">
            The skeleton is live. Server metrics, logs, and job-status widgets arrive in the
            next phases.
          </p>
        </div>
      </main>
    </div>
  )
}
