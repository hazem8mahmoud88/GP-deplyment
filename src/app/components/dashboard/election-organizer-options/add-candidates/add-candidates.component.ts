import { CommonModule } from '@angular/common';
import { Component, inject, OnInit } from '@angular/core';
import { FormControl, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { OrganizerPanel } from 'src/app/services/organizerPanelAPI.service';
import { MessageBoxComponent } from "src/app/utilities/message-box/message-box.component";

@Component({
  standalone: true,
  selector: 'app-add-candidates',
  templateUrl: './add-candidates.component.html',
  styleUrls: ['./add-candidates.component.css'],
  imports: [ReactiveFormsModule, CommonModule, MessageBoxComponent]
})
export class AddCandidatesComponent implements OnInit {
  formActive: boolean = false;
  activatedRoute: ActivatedRoute = inject(ActivatedRoute);
  organizerAPI: OrganizerPanel = inject(OrganizerPanel);
  candidates: any = [];
  electionID: string;
  selectedPhoto: File;
  successMessage = '';
  errorMessage = '';

  ngOnInit() {
    this.electionID = this.activatedRoute.snapshot.paramMap.get('id');
    this.getCandidates();
  }


  avatarImageError(event: any) {
    event.target.src = '../../../../../assets/avatar.jpg';
  }

  getCandidates() {
    this.organizerAPI.getCandidates(this.electionID).subscribe({
      next: (res) => {
        this.candidates = res;
        // Convert relative paths to absolute backend URLs
        this.candidates = this.candidates.map(candidate => ({
          ...candidate,
          photoPath: candidate.photoPath || null
        }));
        console.log(this.candidates);
      },
      error: (err) => {
        this.showErrorMessage('حدث خطأ في ايجاد المرشحين. حاول مره أخري')
      }
    })
  }

  imageSelected(event: any, candidateID: number) {
    const photo = event.target.files[0];
    console.log("Image Changed");
    if (photo) {
      this.selectedPhoto = photo;
      const formData = new FormData();
      formData.append('photo', this.selectedPhoto);

      this.organizerAPI.uploadCandidatePicture(formData, this.electionID, candidateID).subscribe({
        next: (res) => {
          window.location.reload();
        },
        error: (err) => {
          this.showErrorMessage('عذرا. حدث خطأ في اضافه الصوره. حاول مره اخري');
          console.log(err)
        }
      });
    }
  }

  deleteCandidate(candidateID: number) {
    this.organizerAPI.deleteCandidate(this.electionID, candidateID).subscribe({
      next: (res) => {
        this.getCandidates();
        this.showSuccessMessage('تم حذف المرشح بنجاح')
      },
      error: (err) => {
        this.showErrorMessage('عذرا. حدث خطأ في حزف المرشح. حاول مره أخري')
      }
    })
  }

  candidatesForm = new FormGroup({
    fullName: new FormControl('', Validators.required),
    symbol: new FormControl('', Validators.required),
    partyName: new FormControl('', Validators.required),
    candidateNum: new FormControl('', Validators.required)
  })


  onSubmit() {
    this.organizerAPI.addCandidates(
      this.electionID,
      this.candidatesForm.value.fullName,
      this.candidatesForm.value.symbol,
      this.candidatesForm.value.partyName,
      this.candidatesForm.value.candidateNum,
    ).subscribe({
      next: (res) => {
        this.candidatesForm.reset();
        this.getCandidates();
        this.showSuccessMessage('تم اضافه المرشح بنجاح')
      },
      error: (err) => {
        this.showErrorMessage('عذرا. حدث خطأ في اضافه المرشح. حاول مره أخري');
        console.log(err)
      },
    })
  }

  showSuccessMessage(message: string) {
    this.successMessage = message;

    setTimeout(() => {
      this.successMessage = '';
    }, 2000)
  }

  showErrorMessage(message: string) {
    this.errorMessage = message;
    setTimeout(() => {
      this.errorMessage = '';
    }, 2000)
  }

}
