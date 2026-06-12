import {
  AfterViewInit,
  Directive,
  ElementRef,
  Input,
  OnDestroy,
  inject,
} from '@angular/core';
import { AbstractControl } from '@angular/forms';
import flatpickr from 'flatpickr';
import { Instance } from 'flatpickr/dist/types/instance';

@Directive({
  selector: 'input[fpDate]',
  standalone: true,
})
export class FlatpickrDateDirective implements AfterViewInit, OnDestroy {
  @Input({ alias: 'fpDate', required: true }) control!: AbstractControl;
  @Input({ required: true }) minDate!: string;
  @Input({ required: true }) maxDate!: string;

  private el = inject(ElementRef<HTMLInputElement>);
  private fp: Instance | null = null;

  ngAfterViewInit(): void {
    this.fp = flatpickr(this.el.nativeElement, {
      enable: [(date: Date) => date.getDay() === 1],
      minDate: this.minDate,
      maxDate: this.maxDate,
      dateFormat: 'm / d / Y',
      onChange: (dates: Date[]) => {
        if (dates[0]) {
          const d = dates[0];
          const iso = `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
          this.control.setValue(iso);
          this.control.markAsTouched();
        } else {
          this.control.setValue('');
        }
      },
    }) as Instance;
  }

  ngOnDestroy(): void {
    this.fp?.destroy();
  }
}
