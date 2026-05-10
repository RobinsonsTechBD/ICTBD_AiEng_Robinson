// order-list.component.ts
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { OrderService } from '../../../core/services/services';

@Component({
  standalone: true,
  selector: 'app-order-list',
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './order-list.component.html'
})
export class OrderListComponent implements OnInit {
  orders: any[] = [];
  filtered: any[] = [];
  searchTerm = '';
  isLoading  = false;

  constructor(private orderService: OrderService) {}
  ngOnInit() { this.load(); }

  load() {
    this.isLoading = true;
    this.orderService.getAll().subscribe({
      next: o => { this.orders = this.filtered = o; this.isLoading = false; },
      error: () => this.isLoading = false
    });
  }

  search() {
    const q = this.searchTerm.toLowerCase();
    this.filtered = this.orders.filter(o =>
      o.orderNumber.toLowerCase().includes(q) || o.customerName.toLowerCase().includes(q)
    );
  }

  getStatusBadge(s: string) {
    return s === 'Completed' ? 'bg-success' : s === 'Processing' ? 'bg-primary' :
           s === 'Cancelled' ? 'bg-danger' : 'bg-secondary';
  }

  updateStatus(id: number, status: string) {
    this.orderService.updateStatus(id, { status }).subscribe(() => this.load());
  }
}
