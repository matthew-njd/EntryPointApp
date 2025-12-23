import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import {
  AbstractControl,
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { ResetPasswordRequest } from '../../../core/models/auth.model';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-reset-password',
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './reset-password.html',
  styleUrl: './reset-password.css',
})
export class ResetPassword implements OnInit {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private toastService = inject(ToastService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private cdr = inject(ChangeDetectorRef);

  resetPasswordForm: FormGroup;
  isSubmitting = false;
  showPassword = false;
  showConfirmPassword = false;

  email = '';
  token = '';

  constructor() {
    this.resetPasswordForm = this.fb.group(
      {
        newPassword: [
          '',
          [
            Validators.required,
            Validators.minLength(8),
            this.passwordValidator,
          ],
        ],
        confirmPassword: ['', [Validators.required]],
      },
      { validators: this.passwordMatchValidator }
    );
  }

  ngOnInit(): void {
    this.route.queryParams.subscribe((params) => {
      this.email = params['email'] || '';
      this.token = params['token'] || '';

      if (!this.email || !this.token) {
        this.toastService.error(
          'Invalid reset link. Please request a new password reset.'
        );
      }
    });
  }

  get newPassword() {
    return this.resetPasswordForm.get('newPassword');
  }

  get confirmPassword() {
    return this.resetPasswordForm.get('confirmPassword');
  }

  private passwordValidator(control: AbstractControl): ValidationErrors | null {
    const value = control.value;
    if (!value) return null;

    const hasUpperCase = /[A-Z]/.test(value);
    const hasLowerCase = /[a-z]/.test(value);
    const hasNumber = /\d/.test(value);
    const hasSpecialChar = /[\W_]/.test(value);

    const valid = hasUpperCase && hasLowerCase && hasNumber && hasSpecialChar;

    return valid ? null : { passwordStrength: true };
  }

  private passwordMatchValidator(
    group: AbstractControl
  ): ValidationErrors | null {
    const password = group.get('newPassword')?.value;
    const confirmPassword = group.get('confirmPassword')?.value;

    return password === confirmPassword ? null : { passwordMismatch: true };
  }

  onSubmit(): void {
    if (this.resetPasswordForm.invalid || !this.email || !this.token) {
      this.resetPasswordForm.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;
    this.cdr.detectChanges();

    const resetRequest: ResetPasswordRequest = {
      email: this.email,
      token: this.token,
      newPassword: this.resetPasswordForm.value.newPassword,
      confirmPassword: this.resetPasswordForm.value.confirmPassword,
    };

    this.authService.resetPassword(resetRequest).subscribe({
      next: (message) => {
        this.isSubmitting = false;
        this.toastService.success(message);

        setTimeout(() => {
          this.router.navigate(['/login']);
        }, 3000);
      },
      error: (error) => {
        this.isSubmitting = false;
        this.cdr.detectChanges();
        this.toastService.error(error);
      },
    });
  }

  togglePasswordVisibility() {
    this.showPassword = !this.showPassword;
  }

  toggleConfirmPasswordVisibility() {
    this.showConfirmPassword = !this.showConfirmPassword;
  }
}
