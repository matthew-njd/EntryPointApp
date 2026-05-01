import { TranslateLoader, TranslationObject } from '@ngx-translate/core';
import { Observable, of } from 'rxjs';
import { EN } from './en';
import { ES } from './es';

const TRANSLATIONS: Record<string, TranslationObject> = { en: EN as TranslationObject, es: ES as TranslationObject };

export class StaticTranslationLoader implements TranslateLoader {
  getTranslation(lang: string): Observable<TranslationObject> {
    return of(TRANSLATIONS[lang] ?? TRANSLATIONS['en']);
  }
}
