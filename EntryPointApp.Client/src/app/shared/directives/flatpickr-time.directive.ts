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
import { Subscription } from 'rxjs';

@Directive({
  selector: 'input[fpTime]',
  standalone: true,
})
export class FlatpickrTimeDirective implements AfterViewInit, OnDestroy {
  @Input({ alias: 'fpTime', required: true }) control!: AbstractControl;

  private el = inject(ElementRef<HTMLInputElement>);
  private fp: Instance | null = null;
  private subs = new Subscription();
  private isSettingFromPicker = false;

  ngAfterViewInit(): void {
    this.fp = flatpickr(this.el.nativeElement, {
      enableTime: true,
      noCalendar: true,
      dateFormat: 'h:i K',
      onChange: (dates: Date[]) => {
        this.isSettingFromPicker = true;
        if (dates[0]) {
          const h = String(dates[0].getHours()).padStart(2, '0');
          const m = String(dates[0].getMinutes()).padStart(2, '0');
          this.control.setValue(`${h}:${m}`);
          this.control.markAsTouched();
        } else {
          this.control.setValue('');
        }
        this.isSettingFromPicker = false;
      },
    }) as Instance;

    // Pre-populate when the control already has a value (e.g. edit mode)
    if (this.control.value) {
      this.fp.setDate(this.parseTime(this.control.value), false);
    }

    // Keep flatpickr display in sync when the control value changes programmatically
    this.subs.add(
      this.control.valueChanges.subscribe((value: string) => {
        if (this.isSettingFromPicker || !this.fp) return;
        if (!value && this.fp.selectedDates.length > 0) {
          this.fp.clear();
        } else if (value) {
          this.fp.setDate(this.parseTime(value), false);
        }
      })
    );

    // Sync disabled state immediately and whenever status changes
    this.syncDisabled();
    this.subs.add(
      this.control.statusChanges.subscribe(() => this.syncDisabled())
    );
  }

  private parseTime(hhMm: string): Date {
    const [h, m] = hhMm.split(':').map(Number);
    const d = new Date();
    d.setHours(h, m, 0, 0);
    return d;
  }

  private syncDisabled(): void {
    this.el.nativeElement.disabled = this.control.disabled;
  }

  ngOnDestroy(): void {
    this.fp?.destroy();
    this.subs.unsubscribe();
  }
}
