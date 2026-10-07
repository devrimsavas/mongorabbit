import { HttpClient } from '@angular/common/http';
import { Component,OnInit,inject,signal } from '@angular/core';

export interface Product
{
    id:string;
    name:string;
    price:number;
    quantityInStock:number;
    brand:string;
}

@Component({
  selector: 'app-products',
  templateUrl: '../pages/products.component.html',
  imports: [],
})
export class ProductsComponent implements OnInit{
    products=signal<Product[]>([]);
    private http=inject(HttpClient);
    public url:string='http://localhost:5035/api/Product?pageSize=10&pageNumber=1';
    ngOnInit(): void {
        this.loadProducts();
    }
    loadProducts() {
        this.http.get<Product[]>(this.url).subscribe({
            next:(data)=> {
                this.products.set(data);
                console.log(data)
            },
            error:(err)=> {
                console.error("failed to fetch",err.message)
            }
        })
    }



}
