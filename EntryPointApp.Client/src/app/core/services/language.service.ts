import { inject, Injectable, PLATFORM_ID, signal } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { TranslateService } from '@ngx-translate/core';

const LANG_KEY = 'app_lang';
type Lang = 'en' | 'es';

@Injectable({ providedIn: 'root' })
export class LanguageService {
  private translateService = inject(TranslateService);
  private platformId = inject(PLATFORM_ID);

  readonly currentLang = signal<Lang>('en');

  constructor() {
    const saved = isPlatformBrowser(this.platformId)
      ? (localStorage.getItem(LANG_KEY) as Lang | null)
      : null;
    const lang: Lang = saved === 'es' ? 'es' : 'en';
    this.currentLang.set(lang);
    this.translateService.use(lang);
  }

  toggle(): void {
    const next: Lang = this.currentLang() === 'en' ? 'es' : 'en';
    this.currentLang.set(next);
    this.translateService.use(next);
    if (isPlatformBrowser(this.platformId)) {
      localStorage.setItem(LANG_KEY, next);
    }
  }
}
