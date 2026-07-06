import { NavLink, Outlet, useOutletContext } from 'react-router-dom'
import type { DashboardOutletContext } from '../components/DashboardLayout'
import PageHeader from '../components/PageHeader'

export default function ServerPage() {
  const context = useOutletContext<DashboardOutletContext>()

  return (
    <div>
      <PageHeader title="Server" subtitle="Health, background jobs and logs for the GLaDOS host." />

      <nav className="subtabs">
        <NavLink to="/server" end className={({ isActive }) => (isActive ? 'active' : '')}>
          Metrics
        </NavLink>
        <NavLink to="/server/jobs" className={({ isActive }) => (isActive ? 'active' : '')}>
          Jobs
        </NavLink>
        <NavLink to="/server/logs" className={({ isActive }) => (isActive ? 'active' : '')}>
          Logs
        </NavLink>
      </nav>

      <Outlet context={context} />
    </div>
  )
}
