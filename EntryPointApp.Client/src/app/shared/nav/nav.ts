import { Component, computed, inject } from '@angular/core';
import { AuthService } from '../../core/services/auth.service';
import { Router } from '@angular/router';
import { ToastService } from '../../core/services/toast.service';
import { LanguageService } from '../../core/services/language.service';
import { TranslateService, TranslatePipe } from '@ngx-translate/core';

@Component({
  selector: 'app-nav',
  imports: [TranslatePipe],
  templateUrl: './nav.html',
  styleUrl: './nav.css',
})
export class Nav {
  private authService = inject(AuthService);
  private toastService = inject(ToastService);
  private router = inject(Router);
  private languageService = inject(LanguageService);
  private translateService = inject(TranslateService);

  languageLabel = computed(() =>
    this.languageService.currentLang() === 'en' ? 'Español' : 'English'
  );

  toggleLanguage(): void {
    this.languageService.toggle();
  }

  onLogout(): void {
    this.authService.logout();
    this.toastService.success(this.translateService.instant('toast.loggedOut'));
    this.router.navigate(['/login']);
  }
}
