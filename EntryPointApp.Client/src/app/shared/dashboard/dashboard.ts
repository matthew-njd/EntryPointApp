import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Nav } from '../nav/nav';
import { TimesheetService } from '../../core/services/timesheet.service';

@Component({
  selector: 'app-dashboard',
  imports: [CommonModule, Nav],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css',
})
export class Dashboard implements OnInit {
  private timesheetService = inject(TimesheetService);

  ngOnInit(): void {
    this.timesheetService.getTimesheets().subscribe({
      next: (timesheets) => {
        console.log('Timesheets fetched:', timesheets);
      },
      error: (error) => {
        console.error('Timesheets fetched failed:', error);
      },
    });
  }
}
