import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';

@Component({
  standalone: true,
  selector: 'app-message-box',
  templateUrl: './message-box.component.html',
  styleUrls: ['./message-box.component.css'],
  imports: [CommonModule],
})
export class MessageBoxComponent {
  @Input() showSuccess: string;
  @Input() showError: string;
}
