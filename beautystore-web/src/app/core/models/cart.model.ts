export interface CartItem {
  productId: number;
  name: string;
  brand: string;
  price: number;
  imageUrl: string | null;
  quantity: number;
}
