import { HttpClient } from '@angular/common/http';
import { ActivatedRoute } from '@angular/router';
import { Component, inject, OnInit } from '@angular/core';
import { ElectionApi } from 'src/app/services/electionAPI.service';
import { Election } from 'src/app/models/election.model';
import { CommonModule } from '@angular/common';
import { FormControl, FormGroup, Validators, ɵInternalFormsSharedModule, ReactiveFormsModule } from '@angular/forms';
import { MessageBoxComponent } from "src/app/utilities/message-box/message-box.component";

@Component({
  standalone: true,
  selector: 'app-edit-election',
  templateUrl: './edit-election.component.html',
  styleUrls: ['./edit-election.component.css'],
  imports: [CommonModule, MessageBoxComponent, ɵInternalFormsSharedModule, ReactiveFormsModule]
})
export class EditElectionComponent implements OnInit {
  electionAPI: ElectionApi = inject(ElectionApi);
  activatedRoute: ActivatedRoute = inject(ActivatedRoute);
  id: any;
  httpClient: HttpClient = inject(HttpClient);
  election: Election;
  electionType: string = '';
  successMessage: string = '';
  errorMessage: string = '';



  ngOnInit() {
    this.id = this.activatedRoute.snapshot.paramMap.get('id');

    this.electionAPI.getElectionById(this.id).subscribe({
      next: (res: Election) => {
        this.election = res;
        this.electionType = this.election.type;

        this.editElectionForm.patchValue({
          title: this.election.title,
          type: this.election.type,
          description: this.election.description,
          startDate: this.election.startDate.split('T')[0],
          endDate: this.election.endDate.split('T')[0]
        })

      },
      error: err => console.log(err)
    })
  }

  editElectionForm = new FormGroup({
    title: new FormControl(this.electionType, Validators.required),
    type: new FormControl('اختر النوع', Validators.required),
    description: new FormControl('', Validators.required),
    startDate: new FormControl('', Validators.required),
    endDate: new FormControl('', Validators.required),
  })

  onSubmit() {
    let data: Election = {
      title: this.editElectionForm.get('title').value,
      type: this.editElectionForm.get('type').value,
      description: this.editElectionForm.get('description').value,
      startDate: new Date(this.editElectionForm.get('startDate').value).toISOString(),
      endDate: new Date(this.editElectionForm.get('endDate').value).toISOString(),
    }

    this.electionAPI.updateElection(this.id, data).subscribe({
      next: (res) => {
        this.showSuccessMessage('تم تعديل الانتخابات بنجاح');
      },
      error: (err) => {
        this.showErrorMessage('عذرا حدث خطأ. حاول مره اخري')
      }
    });

  }


  showSuccessMessage(message:string) {
    this.successMessage = message;
    setTimeout(() => {
      this.successMessage = '';
    }, 2000)

  }

    showErrorMessage(message:string) {
    this.errorMessage = message;
    setTimeout(() => {
      this.errorMessage = '';
    }, 2000)
  }

}
