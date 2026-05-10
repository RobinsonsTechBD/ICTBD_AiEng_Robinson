// ─── category-list.component.ts ──────────────────────────────
import { Component, OnInit, Inject } from '@angular/core';
import { CategoryService } from '../core/services/services';

@Component({
  selector: 'app-category-list',
  template: ''
})
export class CategoryListComponent implements OnInit {
  categories: any[] = [];
  isLoading = false;
  showForm  = false;
  editItem: any = null;
  name = ''; description = '';

  constructor(private svc: CategoryService) {}
  ngOnInit() { this.load(); }

  load() {
    this.isLoading = true;
    this.svc.getAll().subscribe({ next: c => { this.categories = c; this.isLoading = false; }, error: () => this.isLoading = false });
  }

  openForm(item?: any) { this.editItem = item || null; this.name = item?.categoryName || ''; this.description = item?.description || ''; this.showForm = true; }
  closeForm() { this.showForm = false; this.editItem = null; }

  save() {
    const payload = { categoryName: this.name, description: this.description };
    const action  = this.editItem ? this.svc.update(this.editItem.categoryId, payload) : this.svc.create(payload);
    action.subscribe(() => { this.load(); this.closeForm(); });
  }

  delete(id: number) {
    if (confirm('Delete this category?')) this.svc.delete(id).subscribe(() => this.load());
  }
}

// ─── stock-movement.component.ts ─────────────────────────────
import { ProductService } from '../core/services/services';

@Component({
  selector: 'app-stock-movement',
  template: ''
})
export class StockMovementComponent implements OnInit {
  products: any[] = [];
  productId = ''; qty = 1; movType = 'IN'; notes = '';
  isSaving = false; success = false;

  constructor(private productSvc: ProductService) {}
  ngOnInit() { this.productSvc.getAll().subscribe(p => this.products = p); }

  submit() {
    if (!this.productId) return;
    this.isSaving = true;
    this.productSvc.updateStock(+this.productId, { quantity: this.qty, type: this.movType, notes: this.notes })
      .subscribe({ next: () => { this.success = true; this.isSaving = false; setTimeout(() => { this.success = false; this.productId = ''; this.qty = 1; this.notes = ''; }, 2000); },
                   error: () => this.isSaving = false });
  }
}

// ─── user-list.component.ts ───────────────────────────────────
import { UserService } from '../core/services/services';

@Component({
  selector: 'app-user-list',
  template: ''
})
export class UserListComponent implements OnInit {
  users: any[] = [];
  isLoading = false;
  showForm = false;
  fullName = ''; email = ''; username = ''; password = ''; roleId = 2;
  roles = [{ id: 1, name: 'Admin' }, { id: 2, name: 'Manager' }, { id: 3, name: 'Staff' }];

  constructor(private userSvc: UserService) {}
  ngOnInit() { this.load(); }
  load() { this.isLoading = true; this.userSvc.getAll().subscribe({ next: u => { this.users = u; this.isLoading = false; }, error: () => this.isLoading = false }); }
  openForm() { this.showForm = true; }
  closeForm() { this.showForm = false; }

  save() {
    this.userSvc.register({ fullName: this.fullName, email: this.email, username: this.username, password: this.password, roleId: this.roleId })
      .subscribe(() => { this.load(); this.closeForm(); });
  }

  delete(id: number) {
    if (confirm('Deactivate user?')) this.userSvc.delete(id).subscribe(() => this.load());
  }

  getRoleBadge(r: string) { return r === 'Admin' ? 'bg-danger' : r === 'Manager' ? 'bg-primary' : 'bg-secondary'; }
}
