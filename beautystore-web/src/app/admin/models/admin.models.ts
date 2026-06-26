// ── Dashboard ─────────────────────────────────────────────────────────────────
export interface DashboardStats {
  totalOrders:   number;
  totalRevenue:  number;
  totalProducts: number;
  totalUsers:    number;
  recentOrders:  AdminOrder[];
}

// ── Products ──────────────────────────────────────────────────────────────────
export interface AdminProduct {
  id:           number;
  categoryId:   number;
  categoryName: string;
  name:         string;
  brand:        string;
  description:  string | null;
  price:        number;
  rating:       number;
  stock:        number;
  imageUrl:     string | null;
  isFeatured:   boolean;
  isActive:     boolean;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface CreateProductRequest {
  categoryId:  number;
  name:        string;
  brand:       string;
  description: string | null;
  price:       number;
  rating:      number;
  stock:       number;
  imageUrl:    string | null;
  isFeatured:  boolean;
}

export interface UpdateProductRequest extends CreateProductRequest {
  isActive: boolean;
}

// ── Categories ────────────────────────────────────────────────────────────────
export interface AdminCategory {
  id:           number;
  name:         string;
  slug:         string;
  description:  string | null;
  imageUrl:     string | null;
  displayOrder: number;
  isActive:     boolean;
  productCount: number;
}

export interface CreateCategoryRequest {
  name:         string;
  slug:         string;
  description:  string | null;
  imageUrl:     string | null;
  displayOrder: number;
}

export interface UpdateCategoryRequest extends CreateCategoryRequest {
  isActive: boolean;
}

// ── Orders ────────────────────────────────────────────────────────────────────
export interface AdminOrder {
  id:           number;
  userId:       string;
  productName:  string;
  quantity:     number;
  totalPrice:   number;
  status:       string;
  createdAtUtc: string;
  userEmail:    string;
  userName:     string;
}

export interface UpdateOrderStatusRequest {
  status: string;
}

// ── Users ─────────────────────────────────────────────────────────────────────
export interface AdminUser {
  id:        string;
  email:     string;
  userName:  string;
  roles:     string[];
  fullName:  string;
  createdAt: string;
}

// ── Shared ────────────────────────────────────────────────────────────────────
export interface PagedResult<T> {
  items:      T[];
  page:       number;
  pageSize:   number;
  totalCount: number;
  totalPages: number;
}

export const ORDER_STATUSES = ['Created', 'Confirmed', 'Shipped', 'Delivered', 'Cancelled'] as const;

// ── Analytics ─────────────────────────────────────────────────────────────────
export interface AnalyticsData {
  revenueToday:     number;
  revenueThisMonth: number;
  ordersToday:      number;
  ordersThisMonth:  number;
  customerCount:    number;
  productCount:     number;
  categoryCount:    number;
  lowStockCount:    number;
  topProducts:      TopProduct[];
  topCategories:    TopCategory[];
  revenueTrend:     RevenueTrendPoint[];
}

export interface TopProduct {
  productId:   number;
  productName: string;
  revenue:     number;
  totalSold:   number;
}

export interface TopCategory {
  categoryName: string;
  revenue:      number;
  orderCount:   number;
}

export interface RevenueTrendPoint {
  date:       string;   // "yyyy-MM-dd" (DateOnly serialized by .NET)
  revenue:    number;
  orderCount: number;
}

// ── Inventory ─────────────────────────────────────────────────────────────────
export interface InventoryItem {
  productId:    number;
  productName:  string;
  categoryName: string;
  currentStock: number;
  price:        number;
  isActive:     boolean;
  lowStock:     boolean;
}

export interface UpdateStockRequest {
  stock: number;
}

// ── Settings ──────────────────────────────────────────────────────────────────
export interface SettingsData {
  application: ApplicationInfo;
  azure:       AzureStatus;
  security:    SecurityInfo;
  system:      SystemHealth;
  deployment:  DeploymentInfo;
}

export interface ApplicationInfo {
  appName:        string;
  version:        string;
  environment:    string;
  apiBaseUrl:     string;
  buildTimestamp: string;
}

export interface ServiceStatus {
  status:  string;
  details: string;
}

export interface AzureStatus {
  sqlDatabase:         ServiceStatus;
  blobStorage:         ServiceStatus;
  serviceBus:          ServiceStatus;
  applicationInsights: ServiceStatus;
  openTelemetry:       ServiceStatus;
}

export interface SecurityInfo {
  jwtEnabled:                 boolean;
  identityEnabled:            boolean;
  roleAuthorizationEnabled:   boolean;
  exceptionMiddlewareEnabled: boolean;
  problemDetailsEnabled:      boolean;
}

export interface SystemHealth {
  healthEndpoint:    string;
  currentTimeUtc:    string;
  serverUptime:      string;
  databaseReachable: boolean;
  productCount:      number;
  categoryCount:     number;
  userCount:         number;
}

export interface DeploymentInfo {
  containerAppName:      string;
  containerRevision:     string;
  deploymentEnvironment: string;
  gitCommit:             string;
}
