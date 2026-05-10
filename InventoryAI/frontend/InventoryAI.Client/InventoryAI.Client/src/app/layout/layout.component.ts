import { Component, OnInit } from '@angular/core';
import { Router, NavigationEnd, RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { filter } from 'rxjs/operators';
import { AuthService } from '../core/services/services';
import { AlertService } from '../core/services/services';

@Component({
  standalone: true,
  selector: 'app-layout',
  imports: [CommonModule, RouterModule],
  templateUrl: './layout.component.html'
})
export class LayoutComponent implements OnInit {
  menuItems: any[] = [];
  alertCount = 0;
  today = new Date();
  pageTitle = 'Dashboard';

  constructor(
    private auth: AuthService,
    private alertService: AlertService,
    private router: Router
  ) {}

  get fullName() { return this.auth.currentUser?.user?.FullName || ''; }
  get role()     { return this.auth.currentUser?.user?.Role || ''; }
  get initials() {
    return this.fullName.split(' ').map((n: string) => n[0]).join('').toUpperCase().substring(0, 2);
  }

  ngOnInit() {
    this.menuItems = this.auth.menu;
    this.loadAlertCount();
    this.router.events.pipe(
      filter(e => e instanceof NavigationEnd)
    ).subscribe(() => {
      this.updatePageTitle();
    });
  }

  loadAlertCount() {
    this.alertService.getLowStockAlerts().subscribe({
      next: alerts => this.alertCount = alerts.length,
      error: () => {}
    });
  }

  updatePageTitle() {
    const url = this.router.url;
    const titles: Record<string, string> = {
      '/dashboard':       'Dashboard',
      '/products':        'Products',
      '/categories':      'Categories',
      '/orders':          'Orders',
      '/stock-movements': 'Stock Movements',
      '/alerts':          'Stock Alerts',
      '/reports':         'Reports',
      '/ai-chat':         'AI Assistant',
      '/users':           'User Management'
    };
    this.pageTitle = titles[url] || 'InventoryAI';
  }

  logout() { this.auth.logout(); }
}
