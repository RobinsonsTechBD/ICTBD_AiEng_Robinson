// alerts.component.ts
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AlertService } from '../../core/services/services';

@Component({
  standalone: true,
  selector: 'app-alerts',
  imports: [CommonModule],
  templateUrl: './alerts.component.html'
})
export class AlertsComponent implements OnInit {
  alerts: any[] = [];
  isLoading = false;
  loadingAI: { [key: number]: boolean } = {};
  suggestions: { [key: number]: string } = {};

  constructor(private alertService: AlertService) {}

  ngOnInit() { this.load(); }

  load() {
    this.isLoading = true;
    this.alertService.getLowStockAlerts().subscribe({
      next: a => { this.alerts = a; this.isLoading = false; },
      error: () => this.isLoading = false
    });
  }

  getAiSuggestion(productId: number) {
    this.loadingAI[productId] = true;
    this.alertService.getAiSuggestion(productId).subscribe({
      next: res => {
        this.suggestions[productId] = res.suggestion;
        this.loadingAI[productId]   = false;
      },
      error: () => { this.loadingAI[productId] = false; }
    });
  }

  getSeverityBadge(s: string): string {
    return s === 'Critical' ? 'bg-danger'
         : s === 'High'     ? 'bg-warning text-dark'
         : 'bg-info text-dark';
  }

  getSeverityIcon(s: string): string {
    return s === 'Critical' ? 'bi-exclamation-octagon-fill text-danger'
         : s === 'High'     ? 'bi-exclamation-triangle-fill text-warning'
         : 'bi-info-circle-fill text-info';
  }
}
