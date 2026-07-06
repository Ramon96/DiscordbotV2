import { useCallback, useEffect, useState } from 'react'
import { Navigate, Route, Routes } from 'react-router-dom'
import { ApiError, authApi, type CurrentUser } from './api'
import LoginPage from './pages/Login'
import DashboardLayout from './components/DashboardLayout'
import MetricsPage from './pages/MetricsPage'
import PlayersPage from './pages/PlayersPage'
import PlayerDetailPage from './pages/PlayerDetailPage'
import JobsPage from './pages/JobsPage'
import LogsPage from './pages/LogsPage'
import ChangelogPage from './pages/ChangelogPage'

type AuthState =
  | { status: 'loading' }
  | { status: 'authed'; user: CurrentUser }
  | { status: 'anon' }

export default function App() {
  const [auth, setAuth] = useState<AuthState>({ status: 'loading' })

  const refresh = useCallback(async () => {
    try {
      setAuth({ status: 'authed', user: await authApi.me() })
    } catch (error) {
      if (!(error instanceof ApiError)) {
        console.error('Failed to resolve auth state', error)
      }
      setAuth({ status: 'anon' })
    }
  }, [])

  useEffect(() => {
    refresh()
  }, [refresh])

  if (auth.status === 'loading') {
    return <div className="center muted">Loading…</div>
  }

  if (auth.status === 'anon') {
    return <LoginPage />
  }

  return (
    <Routes>
      <Route
        path="/"
        element={<DashboardLayout user={auth.user} onLogout={() => setAuth({ status: 'anon' })} />}
      >
        <Route index element={<MetricsPage />} />
        <Route path="players" element={<PlayersPage />} />
        <Route path="players/:id" element={<PlayerDetailPage />} />
        <Route path="jobs" element={<JobsPage />} />
        <Route path="logs" element={<LogsPage />} />
        <Route path="changelog" element={<ChangelogPage />} />
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}
