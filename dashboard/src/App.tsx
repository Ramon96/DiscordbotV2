import { useCallback, useEffect, useState } from 'react'
import { Navigate, Route, Routes } from 'react-router-dom'
import { ApiError, authApi, type CurrentUser } from './api'
import LoginPage from './pages/Login'
import DashboardLayout from './components/DashboardLayout'
import OverviewPage from './pages/OverviewPage'
import ServerPage from './pages/ServerPage'
import MetricsPage from './pages/MetricsPage'
import PlayersPage from './pages/PlayersPage'
import PlayerDetailPage from './pages/PlayerDetailPage'
import JobsPage from './pages/JobsPage'
import LogsPage from './pages/LogsPage'
import CommandsPage from './pages/CommandsPage'
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
        <Route index element={<OverviewPage />} />
        <Route path="runescape" element={<PlayersPage />} />
        <Route path="runescape/:id" element={<PlayerDetailPage />} />
        <Route path="server" element={<ServerPage />}>
          <Route index element={<MetricsPage />} />
          <Route path="jobs" element={<JobsPage />} />
          <Route path="logs" element={<LogsPage />} />
        </Route>
        <Route path="commands" element={<CommandsPage />} />
        <Route path="changelog" element={<ChangelogPage />} />
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}
