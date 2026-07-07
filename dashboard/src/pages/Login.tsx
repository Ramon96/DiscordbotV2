import { authApi } from '../api'
import { ApertureLogo, DiscordIcon } from '../components/Icons'

export default function LoginPage() {
  const hadAuthError = new URLSearchParams(window.location.search).has('authError')

  return (
    <div className="center">
      <div className="card login">
        <ApertureLogo size={48} className="aperture" />
        <h1 className="brand">GLaDOS</h1>
        <p className="login-sub">Aperture Science</p>
        <p className="muted">Authenticate to enter the Enrichment Center</p>

        {hadAuthError && (
          <p className="error">
            Couldn’t sign you in — you need to be a member of the GLaDOS Discord server.
          </p>
        )}

        <a className="discord-login" href={authApi.loginUrl}>
          <DiscordIcon size={20} /> Continue with Discord
        </a>
      </div>
    </div>
  )
}
