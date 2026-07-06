import { useCallback, useEffect, useState } from 'react'
import { Navigate, Route, Routes } from 'react-router-dom'
import { ApiError, authApi, type CurrentUser } from './api'
import LoginPage from './pages/Login'
import DashboardPage from './pages/Dashboard'

type AuthState =
  | { status: 'loading' }
  | { status: 'authed'; user: CurrentUser }
  | { status: 'anon' }

export default function App() {
  const [auth, setAuth] = useState<AuthState>({ status: 'loading' })

  const refresh = useCallback(async () => {
    try {
      const user = await authApi.me()
      setAuth({ status: 'authed', user })
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

  return (
    <Routes>
      <Route
        path="/login"
        element={
          auth.status === 'authed'
            ? <Navigate to="/" replace />
            : <LoginPage onAuthenticated={refresh} />
        }
      />
      <Route
        path="/*"
        element={
          auth.status === 'authed'
            ? <DashboardPage user={auth.user} onLogout={() => setAuth({ status: 'anon' })} />
            : <Navigate to="/login" replace />
        }
      />
    </Routes>
  )
}
