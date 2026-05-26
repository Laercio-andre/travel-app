import { Component, OnInit, inject, signal } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { AdminUser } from '../../core/models/travel.models';
import { AdminService } from '../../core/services/admin.service';
import { AuthService } from '../../core/services/auth.service';
import { FeedbackComponent } from '../../shared/components/feedback.component';
import { IconComponent } from '../../shared/components/icon.component';

@Component({
  selector: 'app-admin-users',
  standalone: true,
  imports: [TranslateModule, FeedbackComponent, IconComponent],
  template: `
    <section class="page-heading compact"><p>Admin</p><h1><app-icon name="shield" />{{ 'ADMIN.TITLE' | translate }}</h1></section>
    <app-feedback [loading]="loading()" [error]="error()" [success]="success()" />
    <section class="panel table-panel">
      <table>
        <thead><tr><th>{{ 'COMMON.NAME' | translate }}</th><th>Email</th><th>Role</th><th>{{ 'ADMIN.STATUS' | translate }}</th><th>{{ 'ADMIN.ACTIONS' | translate }}</th></tr></thead>
        <tbody>
          @for (user of users(); track user.id) {
            <tr>
              <td><span class="user-cell"><app-icon name="user" />{{ user.firstName }} {{ user.lastName }}</span></td>
              <td>{{ user.email }}</td>
              <td><span class="status-pill">{{ user.role }}</span></td>
              <td><span class="status-pill" [class.danger]="!user.isActive">{{ (user.isActive ? 'COMMON.ACTIVE' : 'COMMON.INACTIVE') | translate }}</span></td>
              <td>
                <div class="row compact-actions">
                  @if (user.isActive) {
                    <button class="ghost icon-button" type="button" [title]="'ADMIN.DEACTIVATE' | translate" [disabled]="isCurrentUser(user)" (click)="deactivate(user)"><app-icon name="ban" /></button>
                  } @else {
                    <button class="ghost icon-button" type="button" [title]="'ADMIN.ACTIVATE' | translate" (click)="activate(user)"><app-icon name="check" /></button>
                  }
                  <button class="ghost icon-button" type="button" [title]="'ADMIN.MAKE_ADMIN' | translate" [disabled]="user.role === 'Admin' || isCurrentUser(user)" (click)="setRole(user, 'Admin')"><app-icon name="shield" /></button>
                  <button class="ghost icon-button" type="button" [title]="'ADMIN.MAKE_TRAVELER' | translate" [disabled]="user.role === 'Traveler' || isCurrentUser(user)" (click)="setRole(user, 'Traveler')"><app-icon name="user" /></button>
                  <button class="ghost icon-button" type="button" [title]="'ADMIN.SEND_RESET' | translate" (click)="sendReset(user)"><app-icon name="mail" /></button>
                </div>
              </td>
            </tr>
          }
        </tbody>
      </table>
    </section>
  `
})
export class AdminUsersComponent implements OnInit {
  private readonly service = inject(AdminService);
  private readonly auth = inject(AuthService);
  readonly users = signal<AdminUser[]>([]);
  readonly loading = signal(false);
  readonly error = signal('');
  readonly success = signal('');

  ngOnInit(): void {
    this.loading.set(true);
    this.service.users().subscribe({
      next: (users) => {
        this.users.set(users);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('ADMIN.LOAD_ERROR');
        this.loading.set(false);
      }
    });
  }

  deactivate(user: AdminUser): void {
    this.service.deactivate(user.id).subscribe({
      next: (updated) => this.replaceUser(updated),
      error: () => this.error.set('ADMIN.ACTION_ERROR')
    });
  }

  activate(user: AdminUser): void {
    this.service.activate(user.id).subscribe({
      next: (updated) => this.replaceUser(updated),
      error: () => this.error.set('ADMIN.ACTION_ERROR')
    });
  }

  setRole(user: AdminUser, role: 'Traveler' | 'Admin'): void {
    this.service.setRole(user.id, role).subscribe({
      next: (updated) => this.replaceUser(updated),
      error: () => this.error.set('ADMIN.ACTION_ERROR')
    });
  }

  sendReset(user: AdminUser): void {
    this.service.sendPasswordReset(user.id).subscribe({
      next: () => this.success.set('ADMIN.RESET_SENT'),
      error: () => this.error.set('ADMIN.ACTION_ERROR')
    });
  }

  isCurrentUser(user: AdminUser): boolean {
    return user.id === this.auth.currentUser()?.userId;
  }

  private replaceUser(updated: AdminUser): void {
    this.users.update((users) => users.map((item) => (item.id === updated.id ? updated : item)));
  }
}
