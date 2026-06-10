import { ActivatedRoute, Router } from '@angular/router';
import { Component, inject, OnInit } from '@angular/core';
import { AddOrganizersComponent } from "./add-organizers/add-organizers.component";
import { CommonModule } from '@angular/common';
import { EditElectionComponent } from "./edit-election/edit-election.component";
import { ElectionApi } from 'src/app/services/electionAPI.service';
import { MessageBoxComponent } from "src/app/utilities/message-box/message-box.component";
import { ElectionStatsAPI } from "src/app/services/electionStatsAPI.service";

@Component({
  standalone: true,
  selector: 'app-election-options',
  templateUrl: './election-options.component.html',
  styleUrls: ['./election-options.component.css'],
  imports: [AddOrganizersComponent, CommonModule, EditElectionComponent, MessageBoxComponent]
})
export class ElectionOptionsComponent implements OnInit {
  electionAPI: ElectionApi = inject(ElectionApi);
  electionStatsAPI: ElectionStatsAPI = inject(ElectionStatsAPI);
  activatedRoute: ActivatedRoute = inject(ActivatedRoute);
  router: Router = inject(Router);
  electionID: string = '';
  successMessage: string = '';
  errorMessage: string = '';

  ngOnInit() {
    this.electionID = this.activatedRoute.snapshot.paramMap.get('id');
    console.log(this.electionID)
  }


  activeSection: 'add-organizer' | 'edit-election' = 'add-organizer';


  setSection(section: 'add-organizer' | 'edit-election') {
    this.activeSection = section;
  }

  activateElection() {
    this.electionAPI.activateElection(this.electionID).subscribe({
      next: (res) => {
        this.showSuccessMessage('تم تفعيل الانتخابات بنجاح');
      },
      error: (err) => {

        if (err.error.errors[0] == 'Election.NoCandidates') {
          this.showErrorMessage('عذرا. لا يمكن تفعيل الانتخابات لعدم وجود مرشحين');
        } else {
          this.showErrorMessage('عذرا. حدث خطأ في تفعيل الانتخابات');
        }
        console.log(err);
      }
    });
  }

  closeElection() {
    this.electionAPI.closeElection(this.electionID).subscribe({
      next: (res) => {
        this.showSuccessMessage('تم اغلاق الانتخابات بنجاح');
      },
      error: (err) => {
        if (err.error.errors[0] === 'Election.AlreadyClosed') {
          this.showErrorMessage('عذرا. الانتخابات مغلقة بالفعل');
        } else {
          this.showErrorMessage('عذرا. حدث خطأ في اغلاق الانتخابات');
        }
        console.log(err);
      }
    });
  }


  deleteElection() {
    this.electionAPI.deleteElection(this.electionID).subscribe({
      next: (res) => {
        this.showSuccessMessage('تم حذف الانتخابات بنجاح');
        this.router.navigate(['/dashboard']);
      },
      error: (err) => {
        this.showErrorMessage('عذرا. حدث خطأ في حذف الانتخابات');
        console.log(err)
      }
    })
  }

  showSuccessMessage(message: string) {
    this.successMessage = message;
    setTimeout(() => {
      this.successMessage = ''
    }, 2000)
  }

  showErrorMessage(message: string) {
    this.errorMessage = message;
    setTimeout(() => {
      this.errorMessage = ''
    }, 2000)
  }

}
