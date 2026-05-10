import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuthGuard, RoleGuard } from './core/interceptors/auth.interceptor';
import { LayoutComponent } from './layout/layout.component';
import { LoginComponent } from './modules/auth/login/login.component';
import { DashboardComponent } from './modules/dashboard/dashboard.component';
import { ProductListComponent } from './modules/products/product-list/product-list.component';
import { ProductFormComponent } from './modules/products/product-form/product-form.component';
import { OrderListComponent } from './modules/orders/order-list/order-list.component';
import { OrderFormComponent } from './modules/orders/order-form/order-form.component';
import { OrderDetailComponent } from './modules/orders/order-detail/order-detail.component';
import { AlertsComponent } from './modules/alerts/alerts.component';
import { ReportsComponent } from './modules/reports/reports.component';
import { AiChatComponent } from './modules/ai-chat/ai-chat.component';
import { CategoryListComponent } from './modules/catagory-list/catagory-list.component';
import { StockMovementComponent } from './modules/stock-movement/stock-movement.component';

const routes: Routes = [
  { path: 'login', component: LoginComponent },
  {
    path: '',
    component: LayoutComponent,
    canActivate: [AuthGuard],
    children: [
      { path: '',              redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard',    component: DashboardComponent },
      { path: 'products',     component: ProductListComponent },
      { path: 'products/new', component: ProductFormComponent },
      { path: 'products/:id', component: ProductFormComponent },
      { path: 'orders',       component: OrderListComponent },
      { path: 'orders/new',   component: OrderFormComponent },
      { path: 'orders/:id',   component: OrderDetailComponent },
      { path: 'alerts',       component: AlertsComponent },
      { path: 'reports',      component: ReportsComponent },
      { path: 'ai-chat',      component: AiChatComponent },
      { path: 'categories', component: CategoryListComponent},
      { path: 'stock-movements', component: StockMovementComponent }
    ]
  },
  { path: '**', redirectTo: '' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule {}
