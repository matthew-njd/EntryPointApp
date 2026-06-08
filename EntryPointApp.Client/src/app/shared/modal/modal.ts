import {
  Component,
  ElementRef,
  EventEmitter,
  Input,
  Output,
  ViewChild,
} from '@angular/core';

@Component({
  selector: 'app-modal',
  imports: [],
  templateUrl: './modal.html',
  styleUrl: './modal.css',
})
export class Modal {
  @Input() title: string = 'Confirm';
  @Input() titleClass: string = 'text-lg font-bold';
  @Input() body: string = '';
  @Input() bodyClass: string = 'py-4';
  @Input() confirmLabel: string = 'Confirm';
  @Input() confirmClass: string = 'btn btn-success';
  @Input() cancelLabel: string = 'Cancel';
  @Input() cancelClass: string = 'btn btn-outline btn-error';

  @Output() confirmed = new EventEmitter<void>();
  @Output() cancelled = new EventEmitter<void>();

  @ViewChild('dialog') private dialogEl!: ElementRef<HTMLDialogElement>;

  private confirmedByButton = false;

  open(): void {
    this.confirmedByButton = false;
    this.dialogEl.nativeElement.showModal();
  }

  onConfirm(): void {
    this.confirmedByButton = true;
    this.dialogEl.nativeElement.close();
    this.confirmed.emit();
  }

  onCancel(): void {
    this.dialogEl.nativeElement.close();
  }

  onDialogClose(): void {
    if (!this.confirmedByButton) {
      this.cancelled.emit();
    }
    this.confirmedByButton = false;
  }
}
