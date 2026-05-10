// product-form.component.ts
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { ProductService, CategoryService } from '../../../core/services/services';

@Component({
  standalone: true,
  selector: 'app-product-form',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './product-form.component.html'
})
export class ProductFormComponent implements OnInit {
  form: FormGroup;
  categories: any[] = [];
  isLoading  = false;
  isSaving   = false;
  error      = '';
  isEditMode = false;
  productId: number | null = null;
  imagePreview: string | null = null;
  selectedFile: File | null = null;

  constructor(
    private fb: FormBuilder,
    private productService: ProductService,
    private categoryService: CategoryService,
    private route: ActivatedRoute,
    private router: Router
  ) {
    this.form = this.fb.group({
      sku:               ['', Validators.required],
      productName:       ['', Validators.required],
      description:       [''],
      categoryId:        ['', Validators.required],
      unitPrice:         [0, [Validators.required, Validators.min(0)]],
      costPrice:         [0, [Validators.required, Validators.min(0)]],
      quantityInStock:   [0, [Validators.required, Validators.min(0)]],
      lowStockThreshold: [10, [Validators.required, Validators.min(1)]]
    });
  }

  ngOnInit() {
    this.categoryService.getAll().subscribe(c => this.categories = c);

    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEditMode = true;
      this.productId  = +id;
      this.isLoading  = true;
      this.productService.getById(this.productId).subscribe({
        next: p => {
          this.form.patchValue(p);
          this.imagePreview = p.imagePath;
          this.isLoading    = false;
        },
        error: () => { this.isLoading = false; this.router.navigate(['/products']); }
      });
    }
  }

  onFileChange(event: Event) {
    const input = event.target as HTMLInputElement;
    if (input.files?.[0]) {
      this.selectedFile = input.files[0];
      const reader = new FileReader();
      reader.onload = (e) => this.imagePreview = e.target?.result as string;
      reader.readAsDataURL(this.selectedFile);
    }
  }

  submit() {
    if (this.form.invalid || this.isSaving) return;
    this.isSaving = true;
    this.error    = '';

    const action = this.isEditMode
      ? this.productService.update(this.productId!, this.form.value)
      : this.productService.create(this.form.value);

    action.subscribe({
      next: (saved) => {
        if (this.selectedFile) {
          this.productService.uploadImage(saved.productId, this.selectedFile).subscribe({
            next: () => this.router.navigate(['/products']),
            error: () => this.router.navigate(['/products'])
          });
        } else {
          this.router.navigate(['/products']);
        }
      },
      error: (err) => {
        this.error   = err.error?.message || 'Failed to save product';
        this.isSaving = false;
      }
    });
  }
}
