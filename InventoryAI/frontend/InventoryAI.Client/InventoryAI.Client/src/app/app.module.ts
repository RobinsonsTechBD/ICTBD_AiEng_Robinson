// app.module.ts — Root module (Non-Standalone)
import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
import { RouterModule } from '@angular/router';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';

// Layout
import { LayoutComponent } from './layout/layout.component';

// Auth
import { LoginComponent } from './modules/auth/login/login.component';

// Dashboard
import { DashboardComponent } from './modules/dashboard/dashboard.component';

// Products
import { ProductListComponent } from './modules/products/product-list/product-list.component';
import { ProductFormComponent } from './modules/products/product-form/product-form.component';

// Orders
import { OrderListComponent } from './modules/orders/order-list/order-list.component';
import { OrderFormComponent } from './modules/orders/order-form/order-form.component';
import { OrderDetailComponent } from './modules/orders/order-detail/order-detail.component';

// Alerts
import { AlertsComponent } from './modules/alerts/alerts.component';

// Reports
import { ReportsComponent } from './modules/reports/reports.component';

// AI Chat
import { AiChatComponent } from './modules/ai-chat/ai-chat.component';

// Shared
import { AuthInterceptor } from './core/interceptors/auth.interceptor';
import { CategoryListComponent } from './modules/catagory-list/catagory-list.component';
import { StockMovementComponent } from './modules/stock-movement/stock-movement.component';

@NgModule({
  declarations: [
    AppComponent,
  ],
  imports: [
    BrowserModule,
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    HttpClientModule,
    RouterModule,
    AppRoutingModule,
    LayoutComponent,
    LoginComponent,
    DashboardComponent,
    ProductListComponent,
    ProductFormComponent,
    OrderListComponent,
    OrderFormComponent,
    OrderDetailComponent,
    CategoryListComponent,
    AlertsComponent,
    ReportsComponent,
    AiChatComponent,
    StockMovementComponent
  ],
  providers: [
    { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true }
  ],
  bootstrap: [AppComponent]
})
export class AppModule {}
