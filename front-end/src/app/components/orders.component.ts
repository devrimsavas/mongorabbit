import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';

export interface Product {
  id: string;
  name: string;
  price: number;
  quantityInStock: number;
  brand: string;
}

export interface CartItem {
  productId: string;
  name: string;
  price: number;
  quantity: number;
}

@Component({
  selector: 'app-orders',
  templateUrl: '../pages/orders.component.html',
  imports: [],
})
export class OrdersComponent implements OnInit {
  private http = inject(HttpClient);
  private apiUrl = 'http://localhost:5035/api/Product?pageSize=10&pageNumber=1';

  products = signal<Product[]>([]);
  cart = signal<CartItem[]>([]);

  // Calculate cart total dynamically
  totalAmount = computed(() => {
    return this.cart().reduce((sum, item) => sum + item.price * item.quantity, 0);
  });

  // Calculate total item count in cart
  totalItemCount = computed(() => {
    return this.cart().reduce((sum, item) => sum + item.quantity, 0);
  });

  ngOnInit(): void {
    this.fetchProducts();
  }

  fetchProducts(): void {
    this.http.get<Product[]>(this.apiUrl).subscribe({
      next: (data) => this.products.set(data),
      error: (err) => console.error('Failed to fetch products', err)
    });
  }

  addToCart(product: Product, quantityInput: HTMLInputElement): void {
    const qty = parseInt(quantityInput.value, 10);
    if (isNaN(qty) || qty <= 0 || qty > product.quantityInStock) {
      return;
    }

    const currentCart = this.cart();
    const existingIndex = currentCart.findIndex((item) => item.productId === product.id);

    if (existingIndex > -1) {
      const updatedCart = [...currentCart];
      updatedCart[existingIndex].quantity += qty;
      this.cart.set(updatedCart);
    } else {
      this.cart.set([
        ...currentCart,
        {
          productId: product.id,
          name: product.name,
          price: product.price,
          quantity: qty,
        },
      ]);
    }

    quantityInput.value = '1';
  }

  removeFromCart(productId: string): void {
    this.cart.set(this.cart().filter((item) => item.productId !== productId));
  }

  checkout(): void {
    console.log('Sending cart payload to backend:', this.cart());
    // POST request logic to backend will be placed here
  }
}