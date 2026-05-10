import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CategoryService } from '../../core/services/services';

@Component({
  standalone: true,
  selector: 'app-category-list',
  imports: [CommonModule, FormsModule],
  templateUrl: './catagory-list.component.html'
})
export class CategoryListComponent implements OnInit {
  categories: any[] = [];
  isLoading  = false;
  showForm   = false;
  isSaving   = false;
  editItem: any = null;
  name        = '';
  description = '';
  error       = '';
  deleteTarget: any = null;
  showConfirm = false;

  constructor(private svc: CategoryService) {}

  ngOnInit() { this.load(); }

  load() {
    this.isLoading = true;
    this.svc.getAll().subscribe({
      next: c => { this.categories = c; this.isLoading = false; },
      error: () => this.isLoading = false
    });
  }

  openForm(item?: any) {
    this.editItem    = item || null;
    this.name        = item?.categoryName || '';
    this.description = item?.description  || '';
    this.error       = '';
    this.showForm    = true;
  }

  closeForm() { this.showForm = false; this.editItem = null; this.error = ''; }

  save() {
    if (!this.name.trim()) { this.error = 'Category name is required'; return; }
    this.isSaving = true;
    const payload = { categoryName: this.name.trim(), description: this.description };
    const action  = this.editItem
      ? this.svc.update(this.editItem.categoryId, payload)
      : this.svc.create(payload);

    action.subscribe({
      next: () => { this.load(); this.closeForm(); this.isSaving = false; },
      error: (err) => {
        this.error   = err.error?.message || 'Failed to save category';
        this.isSaving = false;
      }
    });
  }

  confirmDelete(item: any) { this.deleteTarget = item; this.showConfirm = true; }

  doDelete() {
    if (!this.deleteTarget) return;
    this.svc.delete(this.deleteTarget.categoryId).subscribe({
      next: () => { this.load(); this.showConfirm = false; this.deleteTarget = null; },
      error: () => this.showConfirm = false
    });
  }
}
