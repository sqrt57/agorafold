import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ApiError } from '../../api/client';
import { AuthService } from '../../auth/auth.service';

@Component({
  selector: 'app-login',
  imports: [FormsModule],
  templateUrl: './login.html',
})
export class Login {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  protected readonly email = signal('');
  protected readonly password = signal('');
  protected readonly rememberMe = signal(false);
  protected readonly error = signal('');
  protected readonly submitting = signal(false);

  async submit(event: SubmitEvent): Promise<void> {
    event.preventDefault();
    this.submitting.set(true);
    this.error.set('');
    try {
      await this.auth.login(this.email(), this.password(), this.rememberMe());
      const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') ?? '/';
      await this.router.navigateByUrl(returnUrl);
    } catch (exception) {
      this.error.set(exception instanceof ApiError ? (exception.errors[0] ?? exception.message) : (exception as Error).message);
    } finally {
      this.submitting.set(false);
    }
  }
}
