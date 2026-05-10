// login.component.ts
import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { AuthService } from '../../../core/services/services';

@Component({
  standalone: true,
  selector: 'app-login',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './login.component.html'
})
export class LoginComponent {
  form: FormGroup;
  isLoading = false;
  error = '';
  showPassword = false;
  returnUrl: string;

  constructor(
    private fb: FormBuilder,
    private auth: AuthService,
    private router: Router,
    private route: ActivatedRoute
  ) {
    this.form = this.fb.group({
      username: ['admin', Validators.required],
      password: ['Admin@123', Validators.required]
    });
    this.returnUrl = this.route.snapshot.queryParams['returnUrl'] || '/dashboard';
    if (this.auth.isLoggedIn) this.router.navigate([this.returnUrl]);
  }

  submit() {
    if (this.form.invalid || this.isLoading) return;
    this.isLoading = true;
    this.error     = '';
    const { username, password } = this.form.value;
    this.auth.login(username, password).subscribe({
      next: () => this.router.navigate([this.returnUrl]),
      error: (err) => {
        this.error     = err.error?.message || 'Invalid username or password';
        this.isLoading = false;
      }
    });
  }
}
