import { Component } from '@angular/core';
import { FileUploadComponent } from "./file-upload/file-upload.component";
import { CommonModule } from '@angular/common';
import { AddCandidatesComponent } from "./add-candidates/add-candidates.component";
import { inject } from '@angular/core';
import { ElectionStatsAPI } from 'src/app/services/electionStatsAPI.service';
import { ActivatedRoute } from '@angular/router';
import { MessageBoxComponent } from 'src/app/utilities/message-box/message-box.component';

@Component({
  standalone: true,
  selector: 'app-election-organizer-options',
  templateUrl: './election-organizer-options.component.html',
  styleUrls: ['./election-organizer-options.component.css'],
  imports: [FileUploadComponent, CommonModule, AddCandidatesComponent, MessageBoxComponent]
})
export class ElectionOrganizerOptionsComponent {
  electionStatsAPI = inject(ElectionStatsAPI);
  route = inject(ActivatedRoute);
  electionID = this.route.snapshot.params['id'];
  successMessage: string = '';
  errorMessage: string = '';

  activeSection: 'upload-data' | 'add-candidates' = 'upload-data';
  setSection(section: 'upload-data' | 'add-candidates') {
    this.activeSection = section;
  }

  countVotes() {
    this.electionStatsAPI.countVotes(Number(this.electionID)).subscribe({
      next: (res) => {
        this.showSuccessMessage('تم حساب الاصوات بنجاح');
        console.log(res);
      },
      error: (err) => {
        if (err.error.errors[0] == 'Results.ElectionNotClosed') {
          this.showErrorMessage('عذرا. لا يمكن حساب الاصوات لان الانتخابات لم يتم اغلاقها');
        } else {
          this.showErrorMessage('عذرا. حدث خطأ في حساب الاصوات');
        }
        console.log(err);
      }
    });
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
