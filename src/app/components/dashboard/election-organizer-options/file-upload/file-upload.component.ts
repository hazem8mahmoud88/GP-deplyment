import { CommonModule } from '@angular/common';
import { Component, inject, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { OrganizerPanel } from 'src/app/services/organizerPanelAPI.service';
import { MessageBoxComponent } from "src/app/utilities/message-box/message-box.component";

@Component({
  standalone: true,
  selector: 'app-file-upload',
  templateUrl: './file-upload.component.html',
  styleUrls: ['./file-upload.component.css'],
  imports: [MessageBoxComponent, CommonModule]
})
export class FileUploadComponent implements OnInit{
  organizerAPI: OrganizerPanel = inject(OrganizerPanel);
  activatedRoute: ActivatedRoute = inject(ActivatedRoute);
  electionID: string;
  successMessage: string = '';
  errorMessage: string = '';

  ngOnInit() {
    this.electionID = this.activatedRoute.snapshot.paramMap.get('id');
  }

  photosFile!: File;
  votersFile!: File;

  photosFileName: string | null = null;
  votersFileName: string | null = null;

  onFileSelected(event: Event, type: 'photosFile' | 'votersFile') {
    const input = event.target as HTMLInputElement;

    if (input.files?.length) {
      const file = input.files[0];

      if (type === 'votersFile') {
        this.votersFile = file;
        this.votersFileName = file.name
      }

      if (type === 'photosFile') {
        this.photosFile = file;
        this.photosFileName = file.name;
      }
    }
  }

  upload() {
    if (!this.votersFile || !this.photosFile) return;

    // Step 1: Upload voters CSV FIRST (voters must exist in DB before photos can match)
    this.organizerAPI.uploadVoters(this.electionID, this.votersFile).subscribe({
      next: (res) => {
        this.showSuccessMessage('تم رفع بيانات الناخبين بنجاح');

        // Step 2: Upload photos ZIP AFTER voters are created
        this.organizerAPI.uploadPhotos(this.electionID, this.photosFile).subscribe({
          next: () => {
            this.showSuccessMessage('تم رفع الصور بنجاح');
            this.votersFileName = null;
            this.photosFileName = null;
            this.votersFile = null;
            this.photosFile = null;
          },
          error: () => {
            this.showErrorMessage('حدث خطأ في رفع الصور. يرجي مراجعه صيغه الملف او المحاوله مره اخري.');
          }
        });
      },
      error: (err) => {
        this.showErrorMessage('حدث خطأ في رفع البيانات. يرجي مراجعه صيغه الملف او المحاوله مره اخري.');
      }
    });
  }

  showSuccessMessage(message) {
    this.successMessage = message;
    setTimeout(() => {
      this.successMessage = null;
    }, 2000)
  }

  showErrorMessage(message) {
    this.errorMessage = message;
    setTimeout(() => {
      this.errorMessage = null;
    }, 2000)
  }

}
