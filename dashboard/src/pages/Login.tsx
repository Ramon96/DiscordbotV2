import { useState, type FormEvent } from 'react'
import { ApiError, authApi } from '../api'

interface LoginPageProps {
  onAuthenticated: () => Promise<void> | void
}

export default function LoginPage({ onAuthenticated }: LoginPageProps) {
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setSubmitting(true)
    setError(null)

    try {
      await authApi.login(username, password)
      await onAuthenticated()
    } catch (err) {
      setError(
        err instanceof ApiError && err.status === 401
          ? 'Invalid username or password.'
          : 'Something went wrong. Please try again.',
      )
      setSubmitting(false)
    }
  }

  return (
    <div className="center">
      <form className="card login" onSubmit={handleSubmit}>
        <h1 className="brand">GLaDOS</h1>
        <p className="muted">Sign in to the dashboard</p>

        <label>
          Username
          <input
            value={username}
            onChange={(event) => setUsername(event.target.value)}
            autoFocus
            autoComplete="username"
          />
        </label>

        <label>
          Password
          <input
            type="password"
            value={password}
            onChange={(event) => setPassword(event.target.value)}
            autoComplete="current-password"
          />
        </label>

        {error && <p className="error">{error}</p>}

        <button type="submit" disabled={submitting}>
          {submitting ? 'Signing in…' : 'Sign in'}
        </button>
      </form>
    </div>
  )
}
