import {
  ApplicationConfig,
  provideBrowserGlobalErrorListeners,
  provideZonelessChangeDetection,
} from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { authInterceptor } from './core/interceptors/auth.interceptor';
import { routes } from './app.routes';
import { provideTranslateService, TranslateLoader } from '@ngx-translate/core';
import { StaticTranslationLoader } from './core/i18n/translation-loader';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZonelessChangeDetection(),
    provideRouter(routes),
    provideHttpClient(withFetch(), withInterceptors([authInterceptor])),
    provideTranslateService({
      loader: { provide: TranslateLoader, useClass: StaticTranslationLoader },
      fallbackLang: 'en',
    }),
  ],
};
