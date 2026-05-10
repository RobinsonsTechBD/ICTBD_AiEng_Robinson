// reports.component.ts
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ReportService } from '../../core/services/services';

@Component({
  standalone: true,
  selector: 'app-reports',
  imports: [CommonModule, FormsModule],
  templateUrl: './reports.component.html'
})
export class ReportsComponent implements OnInit {
  activeTab = 'monthly';
  dailyData: any = null;
  weeklyData: any = null;
  monthlyData: any = null;
  topProducts: any[] = [];
  stockLevels: any[] = [];
  aiInsight = '';
  isLoading = false;
  isLoadingAI = false;

  selectedYear  = new Date().getFullYear();
  selectedMonth = new Date().getMonth() + 1;

  months = [
    { value: 1, label: 'January' }, { value: 2, label: 'February' },
    { value: 3, label: 'March' },   { value: 4, label: 'April' },
    { value: 5, label: 'May' },     { value: 6, label: 'June' },
    { value: 7, label: 'July' },    { value: 8, label: 'August' },
    { value: 9, label: 'September' },{ value: 10, label: 'October' },
    { value: 11, label: 'November' },{ value: 12, label: 'December' }
  ];

  constructor(private reportService: ReportService) {}

  ngOnInit() { this.loadAll(); }

  loadAll() {
    this.isLoading = true;
    this.reportService.getDaily().subscribe(d => this.dailyData = d);
    this.reportService.getWeekly().subscribe(w => this.weeklyData = w);
    this.reportService.getMonthly(this.selectedYear, this.selectedMonth)
      .subscribe(m => { this.monthlyData = m; this.isLoading = false; });
    this.reportService.getTopProducts(10).subscribe(p => this.topProducts = p);
    this.reportService.getStockLevels().subscribe(s => this.stockLevels = s);
  }

  loadMonthly() {
    this.reportService.getMonthly(this.selectedYear, this.selectedMonth)
      .subscribe(m => this.monthlyData = m);
  }

  generateAiInsight() {
    this.isLoadingAI = true;
    this.aiInsight   = '';
    this.reportService.getAiInsight(this.activeTab).subscribe({
      next: res => { this.aiInsight = res.insight; this.isLoadingAI = false; },
      error: ()  => { this.aiInsight = 'AI insight unavailable.'; this.isLoadingAI = false; }
    });
  }

  getStatusBadge(s: string): string {
    return s === 'OUT_OF_STOCK' ? 'bg-danger'
         : s === 'LOW'          ? 'bg-warning text-dark'
         : 'bg-success';
  }

  getStockPercent(p: any): number {
    if (p.lowStockThreshold <= 0) return 100;
    return Math.min(100, (p.quantityInStock / (p.lowStockThreshold * 3)) * 100);
  }
}
