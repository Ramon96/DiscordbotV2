import { useEffect, useState } from 'react'
import { Outlet, useLocation } from 'react-router-dom'
import { authApi, type CurrentUser } from '../api'
import Sidebar from './Sidebar'
import { ApertureLogo, MenuIcon } from './Icons'

export interface DashboardOutletContext {
  onLogout: () => void
  user: CurrentUser
}

interface DashboardLayoutProps {
  user: CurrentUser
  onLogout: () => void
}

export default function DashboardLayout({ user, onLogout }: DashboardLayoutProps) {
  const [menuOpen, setMenuOpen] = useState(false)
  const location = useLocation()

  // Close the drawer whenever the route changes (e.g. after tapping a link).
  useEffect(() => {
    setMenuOpen(false)
  }, [location.pathname])

  // Lock body scroll while the mobile drawer is open.
  useEffect(() => {
    document.body.style.overflow = menuOpen ? 'hidden' : ''
    return () => {
      document.body.style.overflow = ''
    }
  }, [menuOpen])

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
        <button
          className="ghost icon-btn"
          aria-label="Open menu"
          aria-expanded={menuOpen}
          onClick={() => setMenuOpen(true)}
        >
          <MenuIcon />
        </button>
        <span className="topbar-brand">
          <ApertureLogo size={22} className="aperture" />
          <strong>GLaDOS</strong>
        </span>
      </header>

      {menuOpen && (
        <div className="drawer-backdrop" onClick={() => setMenuOpen(false)} aria-hidden="true" />
      )}

      <Sidebar
        user={user}
        onLogout={handleLogout}
        open={menuOpen}
        onClose={() => setMenuOpen(false)}
      />

      <main className="main">
        <div className="main-inner">
          <Outlet context={{ onLogout, user } satisfies DashboardOutletContext} />
        </div>
      </main>
    </div>
  )
}
