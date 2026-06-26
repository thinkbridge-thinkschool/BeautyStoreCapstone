import { Component, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { rxResource } from '@angular/core/rxjs-interop';
import { AdminService } from '../../services/admin.service';
import { AuthService } from '../../../auth/services/auth.service';

@Component({
  selector: 'app-settings',
  imports: [DatePipe],
  templateUrl: './settings.component.html',
  styleUrl: './settings.component.scss',
})
export class SettingsComponent {
  #admin = inject(AdminService);
  #auth  = inject(AuthService);

  readonly currentUser = this.#auth.currentUser;

  settingsResource = rxResource({
    stream: () => this.#admin.getSettings()
  });

  statusBadge(status: string): string {
    return status === 'Configured' ? 'badge-green' : 'badge-inactive';
  }

  boolBadge(value: boolean): string {
    return value ? 'badge-green' : 'badge-red';
  }

  envBadge(env: string): string {
    return env === 'Production' ? 'badge-green' : 'badge-yellow';
  }
}
