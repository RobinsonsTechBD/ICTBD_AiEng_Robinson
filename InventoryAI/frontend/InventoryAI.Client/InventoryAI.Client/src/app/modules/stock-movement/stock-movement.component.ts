import { Component, OnInit } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ProductService } from '../../core/services/services';

@Component({
  standalone: true,
  selector: 'app-stock-movement',
  imports: [CommonModule, FormsModule, DatePipe],
  templateUrl: './stock-movement.component.html'
})
export class StockMovementComponent implements OnInit {
  products: any[] = [];
  productId  = '';
  qty        = 1;
  movType    = 'IN';
  notes      = '';
  isSaving   = false;
  success    = false;
  error      = '';
  selectedProduct: any = null;

  // Movement history (loaded from products stock movements)
  recentMovements: any[] = [];

  constructor(private productSvc: ProductService) {}

  ngOnInit() {
    this.productSvc.getAll().subscribe({
      next: p => this.products = p,
      error: () => {}
    });
  }

  onProductChange() {
    this.selectedProduct = this.products.find(p => p.productId == +this.productId) || null;
  }

  getNewStock(): number {
    if (!this.selectedProduct) return 0;
    const current = this.selectedProduct.quantityInStock;
    if (this.movType === 'IN')     return current + (this.qty || 0);
    if (this.movType === 'OUT')    return current - (this.qty || 0);
    if (this.movType === 'ADJUST') return this.qty || 0;
    return current;
  }

  getNewStockClass(): string {
    const newQty = this.getNewStock();
    if (!this.selectedProduct) return '';
    if (newQty <= 0)                             return 'text-danger fw-bold';
    if (newQty <= this.selectedProduct.lowStockThreshold) return 'text-warning fw-semibold';
    return 'text-success fw-semibold';
  }

  submit() {
    if (!this.productId) { this.error = 'Please select a product'; return; }
    if (this.qty <= 0)   { this.error = 'Quantity must be greater than 0'; return; }
    if (this.movType === 'OUT' && this.selectedProduct &&
        this.qty > this.selectedProduct.quantityInStock) {
      this.error = `Cannot remove more than current stock (${this.selectedProduct.quantityInStock})`;
      return;
    }

    this.isSaving = true;
    this.error    = '';

    this.productSvc.updateStock(+this.productId, {
      quantity: this.qty,
      type:     this.movType,
      notes:    this.notes
    }).subscribe({
      next: () => {
        this.success = true;
        this.isSaving = false;
        // Add to local history
        this.recentMovements.unshift({
          productName: this.selectedProduct?.productName,
          movementType: this.movType,
          quantity: this.qty,
          notes: this.notes,
          createdAt: new Date()
        });
        // Reset form
        setTimeout(() => {
          this.success       = false;
          this.productId     = '';
          this.qty           = 1;
          this.notes         = '';
          this.movType       = 'IN';
          this.selectedProduct = null;
          // Reload products to get updated stock
          this.productSvc.getAll().subscribe(p => this.products = p);
        }, 1500);
      },
      error: (err) => {
        this.error   = err.error?.message || 'Failed to update stock';
        this.isSaving = false;
      }
    });
  }

  getMovBadge(type: string): string {
    return type === 'IN'     ? 'bg-success'
         : type === 'OUT'    ? 'bg-danger'
         : 'bg-warning text-dark';
  }

  getMovIcon(type: string): string {
    return type === 'IN'     ? 'bi-box-arrow-in-down text-success'
         : type === 'OUT'    ? 'bi-box-arrow-up text-danger'
         : 'bi-pencil text-warning';
  }
}
