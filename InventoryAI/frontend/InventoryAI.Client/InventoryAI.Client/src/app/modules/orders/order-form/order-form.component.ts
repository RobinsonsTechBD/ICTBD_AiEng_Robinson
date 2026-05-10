// order-form.component.ts
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, FormArray, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ProductService, OrderService } from '../../../core/services/services';

@Component({
  standalone: true,
  selector: 'app-order-form',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './order-form.component.html'
})
export class OrderFormComponent implements OnInit {
  form: FormGroup;
  products: any[] = [];
  isLoading = false;
  error = '';

  constructor(
    private fb: FormBuilder,
    private productService: ProductService,
    private orderService: OrderService,
    private router: Router
  ) {
    this.form = this.fb.group({
      customerName:  ['', Validators.required],
      customerPhone: [''],
      discount:      [0],
      notes:         [''],
      items: this.fb.array([])
    });
  }

  ngOnInit() {
    this.productService.getAll().subscribe(p => {
      this.products = p.filter((x: any) => x.isActive && x.quantityInStock > 0);
    });
    this.addItem();
  }

  get items(): FormArray { return this.form.get('items') as FormArray; }

  addItem() {
    this.items.push(this.fb.group({
      productId: ['', Validators.required],
      quantity:  [1, [Validators.required, Validators.min(1)]],
      unitPrice: [{ value: 0, disabled: true }]
    }));
  }

  removeItem(i: number) {
    if (this.items.length > 1) this.items.removeAt(i);
  }

  onProductChange(index: number) {
    const productId = this.items.at(index).get('productId')?.value;
    const product   = this.products.find(p => p.productId == productId);
    if (product) {
      this.items.at(index).patchValue({ unitPrice: product.unitPrice });
      this.items.at(index).get('unitPrice')?.disable();
    }
  }

  getProductName(productId: any): string {
    return this.products.find(p => p.productId == productId)?.productName || '';
  }

  getItemTotal(item: any): number {
    const productId = item.get('productId')?.value;
    const quantity = item.get('quantity')?.value || 0;
    const product = this.products.find((p: any) => p.productId == productId);
    return (product?.unitPrice || 0) * quantity;
  }

  get subtotal(): number {
    return this.items.controls.reduce((sum, ctrl) => {
      const productId = ctrl.get('productId')?.value;
      const qty       = ctrl.get('quantity')?.value || 0;
      const product   = this.products.find((p: any) => p.productId == productId);
      return sum + (product?.unitPrice || 0) * qty;
    }, 0);
  }

  get total(): number {
    return this.subtotal - (this.form.get('discount')?.value || 0);
  }

  submit() {
    if (this.form.invalid || this.isLoading) return;
    this.isLoading = true;
    this.error     = '';

    const raw = this.form.getRawValue();
    const payload = {
      customerName:  raw.customerName,
      customerPhone: raw.customerPhone,
      discount:      raw.discount || 0,
      notes:         raw.notes,
      items: raw.items.map((i: any) => ({
        productId: +i.productId,
        quantity:  +i.quantity,
        unitPrice: this.products.find(p => p.productId == i.productId)?.unitPrice || 0
      }))
    };

    this.orderService.create(payload).subscribe({
      next: () => this.router.navigate(['/orders']),
      error: (err) => {
        this.error     = err.error?.message || 'Failed to create order';
        this.isLoading = false;
      }
    });
  }
}
