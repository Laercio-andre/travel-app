import { Component, OnInit, inject, signal } from '@angular/core';
import { AdminUser } from '../../core/models/travel.models';
import { AdminService } from '../../core/services/admin.service';
import { FeedbackComponent } from '../../shared/components/feedback.component';

@Component({
  selector: 'app-admin-users',
  standalone: true,
  imports: [FeedbackComponent],
  template: `
    <section class="page-heading compact"><p>Admin</p><h1>Gestão de utilizadores</h1></section>
    <app-feedback [loading]="loading()" [error]="error()" />
    <section class="panel table-panel">
      <table>
        <thead><tr><th>Nome</th><th>Email</th><th>Role</th><th>Estado</th><th></th></tr></thead>
        <tbody>
          @for (user of users(); track user.id) {
            <tr>
              <td>{{ user.firstName }} {{ user.lastName }}</td>
              <td>{{ user.email }}</td>
              <td>{{ user.role }}</td>
              <td>{{ user.isActive ? 'Ativo' : 'Inativo' }}</td>
              <td><button class="ghost" type="button" [disabled]="!user.isActive" (click)="deactivate(user)">Desativar</button></td>
            </tr>
          }
        </tbody>
      </table>
    </section>
  `
})
export class AdminUsersComponent implements OnInit {
  private readonly service = inject(AdminService);
  readonly users = signal<AdminUser[]>([]);
  readonly loading = signal(false);
  readonly error = signal('');

  ngOnInit(): void {
    this.loading.set(true);
    this.service.users().subscribe({
      next: (users) => {
        this.users.set(users);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Não foi possível carregar utilizadores.');
        this.loading.set(false);
      }
    });
  }

  deactivate(user: AdminUser): void {
    this.service.deactivate(user.id).subscribe((updated) => this.users.update((users) => users.map((item) => (item.id === updated.id ? updated : item))));
  }
}
