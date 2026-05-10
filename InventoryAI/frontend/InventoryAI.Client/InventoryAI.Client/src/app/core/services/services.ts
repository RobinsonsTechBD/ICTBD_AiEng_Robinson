// ============================================================
// Core Services
// ============================================================
import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Router } from '@angular/router';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';

const API = environment.apiUrl;

// ── Auth Service ─────────────────────────────────────────────
@Injectable({ providedIn: 'root' })
export class AuthService {
  private currentUserSubject = new BehaviorSubject<any>(
    JSON.parse(localStorage.getItem('currentUser') || 'null')
  );
  currentUser$ = this.currentUserSubject.asObservable();

  constructor(private http: HttpClient, private router: Router) {}

  get currentUser() { return this.currentUserSubject.value; }
  get token()       { return this.currentUser?.token; }
  get isLoggedIn()  { return !!this.token; }
  get userRole()    { return this.currentUser?.user?.Role; }
  get menu()        { return this.currentUser?.menu || []; }

  login(username: string, password: string): Observable<any> {
    return this.http.post(`${API}/auth/login`, { username, password }).pipe(
      tap((res: any) => {
        localStorage.setItem('currentUser', JSON.stringify(res));
        this.currentUserSubject.next(res);
      })
    );
  }

  logout() {
    localStorage.removeItem('currentUser');
    this.currentUserSubject.next(null);
    this.router.navigate(['/login']);
  }

  hasRole(role: string) { return this.userRole === role; }
}

// ── Product Service ──────────────────────────────────────────
@Injectable({ providedIn: 'root' })
export class ProductService {
  constructor(private http: HttpClient) {}

  getAll(includeInactive = false)             { return this.http.get<any[]>(`${API}/products?includeInactive=${includeInactive}`); }
  getById(id: number)                         { return this.http.get<any>(`${API}/products/${id}`); }
  getLowStock()                               { return this.http.get<any[]>(`${API}/products/low-stock`); }
  create(product: any)                        { return this.http.post<any>(`${API}/products`, product); }
  update(id: number, product: any)            { return this.http.put<any>(`${API}/products/${id}`, product); }
  delete(id: number)                          { return this.http.delete(`${API}/products/${id}`); }
  updateStock(id: number, data: any)          { return this.http.post<any>(`${API}/products/${id}/stock`, data); }
  uploadImage(id: number, file: File)         {
    const fd = new FormData();
    fd.append('file', file);
    return this.http.post<any>(`${API}/products/${id}/upload-image`, fd);
  }
}

// ── Category Service ─────────────────────────────────────────
@Injectable({ providedIn: 'root' })
export class CategoryService {
  constructor(private http: HttpClient) {}

  getAll()                     { return this.http.get<any[]>(`${API}/categories`); }
  getById(id: number)          { return this.http.get<any>(`${API}/categories/${id}`); }
  create(cat: any)             { return this.http.post<any>(`${API}/categories`, cat); }
  update(id: number, cat: any) { return this.http.put<any>(`${API}/categories/${id}`, cat); }
  delete(id: number)           { return this.http.delete(`${API}/categories/${id}`); }
}

// ── Order Service ────────────────────────────────────────────
@Injectable({ providedIn: 'root' })
export class OrderService {
  constructor(private http: HttpClient) {}

  getAll()                          { return this.http.get<any[]>(`${API}/orders`); }
  getById(id: number)               { return this.http.get<any>(`${API}/orders/${id}`); }
  create(order: any)                { return this.http.post<any>(`${API}/orders`, order); }
  updateStatus(id: number, s: any)  { return this.http.patch<any>(`${API}/orders/${id}/status`, s); }
  delete(id: number)                { return this.http.delete(`${API}/orders/${id}`); }
}

// ── Report Service ───────────────────────────────────────────
@Injectable({ providedIn: 'root' })
export class ReportService {
  constructor(private http: HttpClient) {}

  getDaily(date?: string)                  { return this.http.get<any>(`${API}/reports/daily${date ? '?date='+date : ''}`); }
  getWeekly(from?: string, to?: string)    { return this.http.get<any>(`${API}/reports/weekly${from ? '?from='+from+'&to='+to : ''}`); }
  getMonthly(year?: number, month?: number){ return this.http.get<any>(`${API}/reports/monthly${year ? '?year='+year+'&month='+month : ''}`); }
  getTopProducts(count = 10)               { return this.http.get<any[]>(`${API}/reports/top-products?count=${count}`); }
  getStockLevels()                         { return this.http.get<any[]>(`${API}/reports/stock-levels`); }
  getAiInsight(type = 'monthly')           { return this.http.get<any>(`${API}/reports/ai-insight?type=${type}`); }
}

// ── AI Chat Service ──────────────────────────────────────────
@Injectable({ providedIn: 'root' })
export class AiChatService {
  constructor(private http: HttpClient) {}

  startSession()                              { return this.http.post<any>(`${API}/aichat/session`, {}); }
  sendMessage(message: string, sessionId: string) {
    return this.http.post<any>(`${API}/aichat/message`, { message, sessionId });
  }
  getHistory(sessionId: string)               { return this.http.get<any[]>(`${API}/aichat/${sessionId}/history`); }
}

// ── Alert Service ────────────────────────────────────────────
@Injectable({ providedIn: 'root' })
export class AlertService {
  constructor(private http: HttpClient) {}

  getLowStockAlerts()                    { return this.http.get<any[]>(`${API}/alerts/low-stock`); }
  getAiSuggestion(productId: number)     { return this.http.get<any>(`${API}/alerts/${productId}/suggestion`); }
}

// ── User Service ─────────────────────────────────────────────
@Injectable({ providedIn: 'root' })
export class UserService {
  constructor(private http: HttpClient) {}

  getAll()                     { return this.http.get<any[]>(`${API}/auth/users`); }
  register(user: any)          { return this.http.post<any>(`${API}/auth/register`, user); }
  update(id: number, u: any)   { return this.http.put<any>(`${API}/users/${id}`, u); }
  delete(id: number)           { return this.http.delete(`${API}/users/${id}`); }
}
