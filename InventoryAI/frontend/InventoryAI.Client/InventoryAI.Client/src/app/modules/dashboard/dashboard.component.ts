// dashboard.component.ts
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ProductService, OrderService, AlertService, ReportService } from '../../core/services/services';

@Component({
  standalone: true,
  selector: 'app-dashboard',
  imports: [CommonModule],
  templateUrl: './dashboard.component.html'
})
export class DashboardComponent implements OnInit {
  stats = { totalProducts: 0, totalOrders: 0, lowStockCount: 0, revenue: 0 };
  lowStockProducts: any[] = [];
  recentOrders: any[] = [];
  topProducts: any[] = [];
  aiInsight = '';
  isLoadingInsight = false;

  constructor(
    private productService: ProductService,
    private orderService: OrderService,
    private alertService: AlertService,
    private reportService: ReportService
  ) {}

  ngOnInit() {
    this.loadDashboard();
  }

  loadDashboard() {
    this.productService.getAll().subscribe(p => this.stats.totalProducts = p.length);

    this.orderService.getAll().subscribe(o => {
      this.stats.totalOrders = o.length;
      this.stats.revenue     = o.filter((x: any) => x.status === 'Completed')
                                 .reduce((sum: number, x: any) => sum + x.totalAmount, 0);
      this.recentOrders      = o.slice(0, 5);
    });

    this.alertService.getLowStockAlerts().subscribe(a => {
      this.stats.lowStockCount = a.length;
      this.lowStockProducts    = a.slice(0, 5);
    });

    this.reportService.getTopProducts(5).subscribe(p => this.topProducts = p);
  }

  loadAiInsight() {
    this.isLoadingInsight = true;
    this.reportService.getAiInsight('monthly').subscribe({
      next: res => { this.aiInsight = res.insight; this.isLoadingInsight = false; },
      error: ()  => { this.aiInsight = 'Could not load AI insight.'; this.isLoadingInsight = false; }
    });
  }

  getStockBadge(severity: string): string {
    return severity === 'Critical' ? 'bg-danger'
         : severity === 'High'     ? 'bg-warning text-dark'
         : 'bg-info text-dark';
  }

  getOrderBadge(status: string): string {
    return status === 'Completed' ? 'bg-success'
         : status === 'Processing'? 'bg-primary'
         : status === 'Cancelled' ? 'bg-danger'
         : 'bg-secondary';
  }
}
