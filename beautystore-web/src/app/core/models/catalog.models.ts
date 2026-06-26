export interface CatalogProduct {
  id: number;
  categoryId: number;
  categoryName: string;
  name: string;
  brand: string;
  description: string | null;
  price: number;
  rating: number;
  stock: number;
  imageUrl: string | null;
  isFeatured: boolean;
}

export interface CatalogProductDetail extends CatalogProduct {
  categorySlug: string;
  description: string | null;
  relatedProducts: CatalogProduct[];
}

export interface Category {
  id: number;
  name: string;
  slug: string;
  description: string | null;
  imageUrl: string | null;
  displayOrder: number;
  productCount: number;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}
