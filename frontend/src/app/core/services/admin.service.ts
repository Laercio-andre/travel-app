import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AdminUser } from '../models/travel.models';
import { unwrapList } from './api-utils';

@Injectable({ providedIn: 'root' })
export class AdminService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/api/admin`;

  users() {
    return this.http.get<AdminUser[] | { items?: AdminUser[]; data?: AdminUser[] }>(`${this.base}/users`).pipe(map(unwrapList));
  }

  deactivate(id: string) {
    return this.http.patch<AdminUser>(`${this.base}/users/${id}/deactivate`, {});
  }

  activate(id: string) {
    return this.http.patch<AdminUser>(`${this.base}/users/${id}/activate`, {});
  }

  setRole(id: string, role: 'Traveler' | 'Admin') {
    return this.http.patch<AdminUser>(`${this.base}/users/${id}/role`, { role });
  }

  sendPasswordReset(id: string) {
    return this.http.post<{ message: string }>(`${this.base}/users/${id}/password-reset`, {});
  }
}
