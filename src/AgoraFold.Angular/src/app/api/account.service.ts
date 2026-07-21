import { Injectable, inject } from '@angular/core';
import { ApiClient, ApiError } from './client';
import type { AuthUser } from './types';

@Injectable({ providedIn: 'root' })
export class AccountApi {
  private readonly api = inject(ApiClient);

  register(email: string, displayName: string, password: string): Promise<AuthUser> {
    return this.api.request<AuthUser>('/api/account/register', { method: 'POST', body: { email, displayName, password } });
  }

  login(email: string, password: string, rememberMe: boolean): Promise<AuthUser> {
    return this.api.request<AuthUser>('/api/account/login', { method: 'POST', body: { email, password, rememberMe } });
  }

  logout(): Promise<null> {
    return this.api.request<null>('/api/account/logout', { method: 'POST' });
  }

  async me(): Promise<AuthUser | null> {
    try {
      return await this.api.request<AuthUser>('/api/account/me');
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) return null;
      throw error;
    }
  }
}
