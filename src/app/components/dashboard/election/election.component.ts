import { CommonModule } from '@angular/common';
import { Component, inject, Input, OnInit } from '@angular/core';
import { ElectionApi } from 'src/app/services/electionAPI.service';
import { ActivatedRoute, Router, RouterModule } from "@angular/router";
import { OrganizerPanel } from 'src/app/services/organizerPanelAPI.service';

@Component({
  standalone: true,
  selector: 'app-election',
  templateUrl: './election.component.html',
  styleUrls: ['./election.component.css'],
  imports: [CommonModule, RouterModule]
})
export class ElectionComponent implements OnInit {
  electionApi: ElectionApi = inject(ElectionApi);
  organizerPanelApi: OrganizerPanel = inject(OrganizerPanel);
  router: Router = inject(Router);
  activatedRoute: ActivatedRoute = inject(ActivatedRoute);
  electionsArray: any = [];
  user: any;
  userID = JSON.parse(localStorage.getItem('userID'));
  organizerID = JSON.parse(localStorage.getItem('user'))?.organizerId;
  currentDate = new Date();
  currentFormatedDate = this.currentDate.toISOString().split('T')[0];

  @Input() home: boolean = false;

  ngOnInit() {

    // If Admin Or User

    this.getAllElections();

    // If Organizer

    if (JSON.parse(localStorage.getItem('user'))?.role === 'Organizer') {
      this.organizerPanelApi.getOrganizerElections(this.organizerID).subscribe({
        next: (res) => {
          this.electionsArray = res;
          // this.checkElectionStatus()
        },
        error: err => console.log(err)
      })
    }
  }


  getAllElections() {
    const userRole = JSON.parse(localStorage.getItem('user'))?.role;
    if (userRole === 'Admin' || this.home) {
      this.electionApi.getElections().subscribe({
        next: (res) => {
          this.electionsArray = res;
          console.log(this.electionsArray);
        },
        error: (err) => console.log(err.error.errors)
      });
    }
  }

  vote(id: string) {
    this.router.navigate(['vote', id])
  }

  addOrganizer(id: string) {
    this.router.navigate(['election-options', id])
  }
  goToOrganizerOptions(id: string) {
    this.router.navigate(['election-organizer-options', id])
  }

  // checkElectionStatus() {
  //   this.electionsArray.forEach(election => {
  //     if (this.currentFormatedDate < election.endDate && election.status == 'draft' || 'Closed') {
  //       this.activateElection(election.id);
  //     } else if (this.currentFormatedDate >= election.endDate && election.status == 'Active') {
  //       this.closeElection(election.id);
  //     }
  //   });
  // }

  activateElection(electionID: string) {
    this.electionApi.activateElection(electionID).subscribe({
      next: (res) => {
        console.log(res, "worked");
      },
      error: (err) => {
        console.log(err, "Didn't Work");
      }
    })
  }

  closeElection(electionID: string) {
    this.electionApi.closeElection(electionID).subscribe({
      next: (res) => {
        console.log(res, "worked");
      },
      error: (err) => {
        console.log(err, "Didn't Work");
      }
    })
  }

  showStats(id: string) {
    this.router.navigate(['election-stats', id])
  }

}
