export type ApiResponse<T> = {
  success: boolean
  data: T
  message?: string | null
  errors: string[]
  meta?: unknown
}

export type PagedResult<T> = {
  data: T[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
  hasPreviousPage: boolean
  hasNextPage: boolean
}

export type UserRole = 'Admin' | 'Inventario' | 'Contador' | 'Comprador'

export type UserInfo = {
  id: number
  userName: string
  rut: string | null
  fullName: string
  role: UserRole
  isActive: boolean
  createdAt: string
  lastLoginAt: string | null
}

export type LoginResponse = {
  accessToken: string
  refreshToken: string
  expiresAt: string
  user: UserInfo
}

export type CategoryRef = {
  id: number
  name: string
}

export type ProductListItem = {
  id: number
  name: string
  sku: string | null
  price: number
  stockQuantity: number
  isActive: boolean
  isLowStock: boolean
  category: CategoryRef
}

export type Product = ProductListItem & {
  description: string | null
  cost: number
  minStockLevel: number
  unitOfMeasure: string | null
  createdAt: string
  updatedAt: string | null
  marginPercentage: number
}

export type MovementType = 'Entrada' | 'Salida' | 'Ajuste'

export type StockMovement = {
  id: number
  type: MovementType
  quantity: number
  reason: string | null
  reference: string | null
  createdAt: string
  unitCost: number | null
  previousStock: number
  newStock: number
}

export type CreateMovement = {
  type: MovementType
  quantity: number
  reason?: string
  reference?: string
  unitCost?: number
}

export type KardexSummary = {
  productId: number
  productName: string
  productSKU: string
  currentStock: number
  averageCost: number
  stockValue: number
  totalEntradas: number
  totalSalidas: number
  totalAjustes: number
  totalEntradasValue: number
  totalSalidasValue: number
}
