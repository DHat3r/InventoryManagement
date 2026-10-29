import { Navigate, Route, Routes } from 'react-router'
import RequireAuth from './auth/RequireAuth'
import Layout from './components/Layout'
import KardexPage from './pages/KardexPage'
import LoginPage from './pages/LoginPage'
import ProductsPage from './pages/ProductsPage'

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />

      <Route element={<RequireAuth />}>
        <Route element={<Layout />}>
          <Route path="/productos" element={<ProductsPage />} />
          <Route path="/productos/:id/kardex" element={<KardexPage />} />
        </Route>
      </Route>

      <Route path="*" element={<Navigate to="/productos" replace />} />
    </Routes>
  )
}
