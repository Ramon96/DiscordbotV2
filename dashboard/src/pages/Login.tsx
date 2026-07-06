import { authApi } from '../api'

export default function LoginPage() {
  const hadAuthError = new URLSearchParams(window.location.search).has('authError')

  return (
    <div className="center">
      <div className="card login">
        <h1 className="brand">GLaDOS</h1>
        <p className="muted">Sign in to view the dashboard</p>

        {hadAuthError && (
          <p className="error">
            Couldn’t sign you in — you need to be a member of the GLaDOS Discord server.
          </p>
        )}

        <a className="discord-login" href={authApi.loginUrl}>
          Continue with Discord
        </a>
      </div>
    </div>
  )
}
