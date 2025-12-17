import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Footer } from './shared/footer/footer';
import { ToastComponent } from './shared/toast/toast';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Footer, ToastComponent],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  protected readonly title = signal('EntryPointApp.Client');
}
