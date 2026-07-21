import { Injectable, inject, signal } from '@angular/core';
import { AccountApi } from '../api/account.service';
import type { AuthUser } from '../api/types';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly accountApi = inject(AccountApi);

  readonly user = signal<AuthUser | null>(null);
  readonly hydrated = signal(false);

  private hydratePromise: Promise<void> | null = null;

  // Memoized so repeated calls (e.g. from both the app shell and a route guard
  // racing on first navigation) only ever issue one `/api/account/me` request.
  hydrate(): Promise<void> {
    if (!this.hydratePromise) {
      this.hydratePromise = this.accountApi.me().then((loadedUser) => {
        this.user.set(loadedUser);
        this.hydrated.set(true);
      });
    }
    return this.hydratePromise;
  }

  async register(email: string, displayName: string, password: string): Promise<void> {
    this.user.set(await this.accountApi.register(email, displayName, password));
  }

  async login(email: string, password: string, rememberMe: boolean): Promise<void> {
    this.user.set(await this.accountApi.login(email, password, rememberMe));
  }

  async logout(): Promise<void> {
    await this.accountApi.logout();
    this.user.set(null);
  }
}
