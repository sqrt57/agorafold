import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ApiError } from '../../api/client';
import { AuthService } from '../../auth/auth.service';

@Component({
  selector: 'app-register',
  imports: [FormsModule],
  templateUrl: './register.html',
})
export class Register {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly email = signal('');
  protected readonly displayName = signal('');
  protected readonly password = signal('');
  protected readonly errors = signal<string[]>([]);
  protected readonly submitting = signal(false);

  async submit(event: SubmitEvent): Promise<void> {
    event.preventDefault();
    this.submitting.set(true);
    this.errors.set([]);
    try {
      await this.auth.register(this.email(), this.displayName(), this.password());
      await this.router.navigateByUrl('/');
    } catch (exception) {
      this.errors.set(exception instanceof ApiError && exception.errors.length ? exception.errors : [(exception as Error).message]);
    } finally {
      this.submitting.set(false);
    }
  }
}
