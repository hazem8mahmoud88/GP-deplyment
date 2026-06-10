import { Component, inject, OnInit } from '@angular/core';
import { CreateElectionComponent } from "./create-election/create-election.component";
import { CommonModule } from '@angular/common';
import { ElectionApi } from 'src/app/services/electionAPI.service';
import { ElectionComponent } from './election/election.component';
import { ApiAuthService } from 'src/app/services/authAPI.service';
import { BehaviorSubject } from 'rxjs';
import { Organizer } from 'src/app/models/organizers.model';
import { Admin } from 'src/app/models/Admin.model';

@Component({
  standalone: true,
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css'],
  imports: [CreateElectionComponent, CommonModule, ElectionComponent]
})
export class DashboardComponent implements OnInit{
  closeElectionScreen: boolean = false;
  electionApi: ElectionApi = inject(ElectionApi);
  authAPI: ApiAuthService = inject(ApiAuthService);
  user: any;


  ngOnInit() {
    this.authAPI.user.subscribe({
      next: (res) => this.user = res,
      error: err => console.log(err)
    });
    console.log(this.user);
  }

  closeElectionScreenFn() {
    this.closeElectionScreen = false;
  }

}
