// product-list.component.ts
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { ProductService } from '../../../core/services/services';

@Component({
  standalone: true,
  selector: 'app-product-list',
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './product-list.component.html'
})
export class ProductListComponent implements OnInit {
  products: any[]  = [];
  filtered: any[]  = [];
  searchTerm  = '';
  isLoading   = false;
  showConfirm = false;
  deleteTarget: any = null;

  constructor(private productService: ProductService) {}

  ngOnInit() { this.load(); }

  load() {
    this.isLoading = true;
    this.productService.getAll().subscribe({
      next: p => { this.products = this.filtered = p; this.isLoading = false; },
      error: () => this.isLoading = false
    });
  }

  search() {
    const q = this.searchTerm.toLowerCase();
    this.filtered = this.products.filter(p =>
      p.productName.toLowerCase().includes(q) ||
      p.sku.toLowerCase().includes(q) ||
      p.category?.categoryName?.toLowerCase().includes(q)
    );
  }

  confirmDelete(product: any) { this.deleteTarget = product; this.showConfirm = true; }

  doDelete() {
    if (!this.deleteTarget) return;
    this.productService.delete(this.deleteTarget.productId).subscribe({
      next: () => { this.load(); this.showConfirm = false; },
      error: () => this.showConfirm = false
    });
  }

  getStockClass(p: any): string {
    if (p.quantityInStock <= 0)                  return 'text-danger fw-bold';
    if (p.quantityInStock <= p.lowStockThreshold) return 'text-warning fw-semibold';
    return 'text-success';
  }

  getStockBadge(p: any): string {
    if (p.quantityInStock <= 0)                  return 'bg-danger';
    if (p.quantityInStock <= p.lowStockThreshold) return 'bg-warning text-dark';
    return 'bg-success';
  }
}
