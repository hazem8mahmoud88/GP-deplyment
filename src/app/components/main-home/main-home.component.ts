import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { ElectionComponent } from "../dashboard/election/election.component";

@Component({
  standalone: true,
  selector: 'app-main-home',
  imports: [CommonModule, ElectionComponent],
  templateUrl: './main-home.component.html',
  styleUrls: ['./main-home.component.css']
})
export class MainHomeComponent {
}
