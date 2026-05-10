// ─── order-detail.component.ts ───────────────────────────────
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { OrderService } from '../../../core/services/services';

@Component({
  standalone: true,
  selector: 'app-order-detail',
  imports: [CommonModule],
  templateUrl: './order-detail.component.html'
})
export class OrderDetailComponent implements OnInit {
  order: any = null;
  isLoading  = true;

  constructor(private orderService: OrderService, private route: ActivatedRoute) {}

  ngOnInit() {
    const id = +(this.route.snapshot.paramMap.get('id') || 0);
    this.orderService.getById(id).subscribe({
      next: o => { this.order = o; this.isLoading = false; },
      error: () => this.isLoading = false
    });
  }

  getStatusBadge(s: string) {
    return s === 'Completed' ? 'bg-success' : s === 'Processing' ? 'bg-primary' :
           s === 'Cancelled' ? 'bg-danger' : 'bg-secondary';
  }
}
