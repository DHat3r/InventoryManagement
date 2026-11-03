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

// Tres dimensiones independientes de un producto (ver Models/ProductStates.cs en la API)
export type ApprovalStatus = 'Pendiente' | 'Aprobado' | 'Rechazado'
export type Lifecycle = 'Activo' | 'Retirado' | 'Descontinuado'

export type ProductListItem = {
  id: number
  name: string
  sku: string | null
  price: number | null // null si aún no se aprueba o si el rol no ve precios de venta
  stockQuantity: number
  approvalStatus: ApprovalStatus
  lifecycle: Lifecycle
  isLowStock: boolean
  isOutOfStock: boolean
  isPossiblyUnavailable: boolean // retiro solicitado, esperando a Admin
  hasPendingEdit: boolean // edición propuesta, esperando a Admin
  category: CategoryRef
}

export type Product = ProductListItem & {
  description: string | null
  cost: number
  minStockLevel: number
  unitOfMeasure: string | null
  createdAt: string
  updatedAt: string | null
  rejectionReason: string | null
  marginPercentage: number | null
}

export type Category = {
  id: number
  name: string
}

// Body de POST /api/products y PUT /api/products/{id}. price: solo Admin (Inventario lo omite).
export type ProductInput = {
  name: string
  price?: number
  cost: number
  categoryId: number
  sku?: string
  description?: string
  stockQuantity: number
  minStockLevel: number
  unitOfMeasure?: string
}

export type ChangeRequestType = 'Edicion' | 'Retiro'
export type ChangeRequestStatus = 'Pendiente' | 'Aprobada' | 'Rechazada'

export type ProductCurrentValues = {
  name: string
  sku: string | null
  description: string | null
  cost: number
  categoryId: number
  minStockLevel: number
  unitOfMeasure: string | null
}

// Solicitud de Inventario que Admin revisa (GET /api/product-change-requests)
export type ProductChangeRequest = {
  id: number
  productId: number
  productName: string
  type: ChangeRequestType
  status: ChangeRequestStatus
  name: string | null
  sku: string | null
  description: string | null
  cost: number | null
  categoryId: number | null
  minStockLevel: number | null
  unitOfMeasure: string | null
  reason: string | null
  requestedBy: number
  requestedAt: string
  reviewedBy: number | null
  reviewedAt: string | null
  reviewComment: string | null
  current: ProductCurrentValues
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
