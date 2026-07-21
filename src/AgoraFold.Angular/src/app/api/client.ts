import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../environments/environment';

const API_BASE_URL = environment.apiBaseUrl;

const MUTATING_METHODS = new Set(['POST', 'PUT', 'DELETE', 'PATCH']);

export class ApiError extends Error {
  status: number;
  errors: string[];

  constructor(status: number, errors?: string[]) {
    super(errors?.[0] ?? `Request failed with status ${status}`);
    this.status = status;
    this.errors = errors ?? [];
  }
}

export function imageUrl(path: string | null | undefined): string | undefined {
  return path ? `${API_BASE_URL}${path}` : undefined;
}

interface ApiRequestOptions {
  method?: string;
  body?: unknown;
}

@Injectable({ providedIn: 'root' })
export class ApiClient {
  constructor(private readonly http: HttpClient) {}

  // ASP.NET antiforgery tokens are bound to the current identity, so fetch a fresh
  // token for every mutation instead of caching one across login/logout boundaries.
  private async getCsrfToken(): Promise<string> {
    const response = await firstValueFrom(
      this.http.get<{ token: string }>(`${API_BASE_URL}/api/antiforgery/token`, { withCredentials: true }),
    );
    return response.token;
  }

  async request<T>(path: string, { method = 'GET', body }: ApiRequestOptions = {}): Promise<T> {
    const headers: Record<string, string> = {};

    if (MUTATING_METHODS.has(method)) {
      headers['X-CSRF-TOKEN'] = await this.getCsrfToken();
    }

    try {
      const response = await firstValueFrom(
        this.http.request<T>(method, `${API_BASE_URL}${path}`, {
          headers,
          body,
          withCredentials: true,
        }),
      );
      return response as T;
    } catch (error) {
      if (error instanceof HttpErrorResponse) {
        const data = typeof error.error === 'object' && error.error !== null ? (error.error as { errors?: string[] }) : undefined;
        throw new ApiError(error.status, data?.errors);
      }
      throw error;
    }
  }
}
