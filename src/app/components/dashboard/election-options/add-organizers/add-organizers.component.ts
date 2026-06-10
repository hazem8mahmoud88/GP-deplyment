import { CommonModule } from '@angular/common';
import { Component, inject, OnInit } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Route } from '@angular/router';
import { AdminPanelAPI } from 'src/app/services/adminPanelAPI.service';
import { ElectionApi } from 'src/app/services/electionAPI.service';
import { MessageBoxComponent } from "src/app/utilities/message-box/message-box.component";

@Component({
  standalone: true,
  selector: 'app-add-organizers',
  templateUrl: './add-organizers.component.html',
  styleUrls: ['./add-organizers.component.css'],
  imports: [CommonModule, ReactiveFormsModule, MessageBoxComponent]
})
export class AddOrganizersComponent {
  formActive:boolean = false;
  adminPanelAPI: AdminPanelAPI = inject(AdminPanelAPI);
  route: ActivatedRoute = inject(ActivatedRoute);
  electionAPI: ElectionApi = inject(ElectionApi);
  id: any;
  organizers: any = [];
  allOrganizers: any = [];
  electionDetails: any = '';
  successMessage: string = '';
  errorMessage: string = '';
  isDesktop: boolean = window.innerWidth > 768;

  ngOnInit() {
    this.id = this.route.snapshot.paramMap.get('id');
    this.getElectionDetails();
    this.getOrganizers();
    this.getAllOrganizers();
  }

  getElectionDetails() {
    this.electionAPI.getElectionById(this.id).subscribe({
      next: res => {this.electionDetails = res},
    })
  }

  getOrganizers() {
    this.adminPanelAPI.getOrganizers(this.id).subscribe({
      next: (res) => {
        this.organizers = res;
        console.log(this.organizers)
      },
      error: err => console.log(err)
    });
  }

  getAllOrganizers() {
    this.adminPanelAPI.getAllOrganizers().subscribe({
      next: (res) => {
        this.allOrganizers = res;
        console.log(this.allOrganizers)
      },
      error: (err) => {
        console.log(err);
      }
    })
  }

  deleteOrganizer(id: number) {
    this.adminPanelAPI.deleteOrganizer(this.id, id).subscribe({
      next: () => {
        if (this.organizers.length === 1) {
          this.organizers = []
        }
        this.getOrganizers();
        this.showSuccessMessage("تم حذف المنظم بنجاح");
      },
      error: () => {
        this.showErrorMessage("حدث خطأ. حاول مره اخري")
      }
    })
  }

  activateForm() {
    this.formActive = !this.formActive;
  }

  organizerForm = new FormGroup({
    id: new FormControl(null, [Validators.required]),
  })

  onSubmit() {
    this.adminPanelAPI.addOrganizer(this.id, this.organizerForm.value.id)
    .subscribe({
      next: (res) => {
        this.showSuccessMessage('تم حفظ المنظم بنجاح')
        this.getOrganizers();
      },
      error: (err) => {
        if (err.error.errors[0] == 'ElectionOrganizer.AlreadyAssigned') {
          this.showErrorMessage('المنظم موجود بالفعل')
        } else {
          this.showErrorMessage('حدث خطأ. حاول مره اخري');
          console.log(err)
        }
      }
    })
    this.organizerForm.reset();
  }

  showSuccessMessage(message: string) {
    this.successMessage = message;
    setTimeout(() => {
      this.successMessage = '';
    }, 2000)
  }

  showErrorMessage(message: string) {
    this.errorMessage = message
    setTimeout(() => {
      this.errorMessage = ''
    }, 2000)
  }
}
