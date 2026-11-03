import { NavLink, Outlet } from 'react-router'
import { useAuth } from '../auth/AuthContext'

export default function Layout() {
  const { user, logout } = useAuth()

  return (
    <>
      <header className="header">
        <div className="header-left">
          <span className="brand">InventoryManagement</span>
          <NavLink to="/productos" className="nav-link">
            Productos
          </NavLink>
          {/* La API también restringe la bandeja a Admin; el enlace solo evita mostrar algo inútil */}
          {user?.role === 'Admin' && (
            <NavLink to="/aprobaciones" className="nav-link">
              Aprobaciones
            </NavLink>
          )}
        </div>

        <div className="header-right">
          <span className="user-name">{user?.fullName}</span>
          {user && <span className="badge">{user.role}</span>}
          <button type="button" className="btn-secondary" onClick={() => logout()}>
            Cerrar sesión
          </button>
        </div>
      </header>

      <main className="container">
        <Outlet />
      </main>
    </>
  )
}
