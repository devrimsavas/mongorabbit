import { Routes } from '@angular/router';
import { HomeComponent } from './components/home.component';
import { ProductsComponent } from './components/products.component';
import { OrdersComponent } from './components/orders.component';


export const routes: Routes = [
    { path: '', component: HomeComponent },
    {path:'products',component:ProductsComponent},
    {path:'orders',component:OrdersComponent}


];
