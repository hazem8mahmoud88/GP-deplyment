import { Component, EventEmitter, inject, OnInit, Output } from '@angular/core';
import { FormArray, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { Election } from 'src/app/models/election.model';
import { ElectionApi } from 'src/app/services/electionAPI.service';
import { ApiAuthService } from 'src/app/services/authAPI.service';
import { MessageBoxComponent } from 'src/app/utilities/message-box/message-box.component';


@Component({
  standalone: true,
  selector: 'app-create-election',
  imports: [CommonModule, ReactiveFormsModule, MessageBoxComponent],
  templateUrl: './create-election.component.html',
  styleUrls: ['./create-election.component.css']
})
export class CreateElectionComponent implements OnInit {

  @Output() closeElectionScreen = new EventEmitter<boolean>()
  electionAPI: ElectionApi = inject(ElectionApi);
  apiAuth: ApiAuthService = inject(ApiAuthService);
  createdBy: any;
  successMessage: string = '';
  errorMessage: string = '';

  close() {
    this.closeElectionScreen.emit(false);
  }

  ngOnInit() {
    this.apiAuth.user.subscribe((user) => { this.createdBy = user.adminId });
    localStorage.setItem('createdBy', JSON.stringify(this.createdBy));
  }

  candidatesForm = new FormGroup({
    title: new FormControl('', Validators.required),
    type: new FormControl('', Validators.required),
    description: new FormControl('', Validators.required),
    startDate: new FormControl('', Validators.required),
    endDate: new FormControl('', Validators.required),
  })

  onSubmit() {
    let data: Election = {
      title: this.candidatesForm.get('title').value,
      type: this.candidatesForm.get('type').value,
      description: this.candidatesForm.get('description').value,
      startDate: new Date(this.candidatesForm.get('startDate').value).toISOString(),
      endDate: new Date(this.candidatesForm.get('endDate').value).toISOString(),
    }

    this.electionAPI.createElection(data).subscribe({
      next: (res) => {
        this.showSuccessMessage('تم انشاء الانتخابات بنجاح');
        console.log(res);
        window.location.reload();
        this.candidatesForm.reset();
        this.close();
      },
      error: (err) => {

        if (err.error.errors.StartDate?.[0] == 'Start date must be in the future') {
          this.showErrorMessage('وقت البدء يجب ان يكون في المستقبل');
        } else if (err.error.errors.EndDate?.[0] == 'End date must be after start date') {
          this.showErrorMessage('وقت الانتهاء يجب ان يكون بعد وقت البدء')
        } else {
          this.showErrorMessage('حدث خطأ');
        }

        console.log(err)
      }
    })

    console.log(data);
  }

  showSuccessMessage(message: string) {
    this.successMessage = message
    setTimeout(() => {
      this.successMessage = '';
    }, 2000);
  }

  showErrorMessage(message: string) {
    this.errorMessage = message;
    setTimeout(() => {
      this.errorMessage = '';
    }, 2000);
  }

}
