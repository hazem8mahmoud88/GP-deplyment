import { CommonModule } from '@angular/common';
import { AfterViewChecked, AfterViewInit, Component, ElementRef, inject, OnChanges, OnInit, ViewChild } from '@angular/core';
import { FormGroup, FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { OrganizerPanel } from 'src/app/services/organizerPanelAPI.service';
import { votingAPI } from 'src/app/services/votingApi.service';
import { MessageBoxComponent } from "src/app/utilities/message-box/message-box.component";

@Component({
  standalone: true,
  selector: 'app-voting-process',
  templateUrl: './voting-process.component.html',
  styleUrls: ['./voting-process.component.css'],
  imports: [ReactiveFormsModule, CommonModule, MessageBoxComponent],
})
export class VotingProcessComponent implements OnInit, AfterViewChecked {
  route: ActivatedRoute = inject(ActivatedRoute);
  router: Router = inject(Router);
  electionID: string;
  nationalId: string;
  votingApi: votingAPI = inject(votingAPI);
  organizersPanel: OrganizerPanel = inject(OrganizerPanel);
  idVerifySection: boolean = true;
  pictureVerificationSection: boolean = false;
  candidateSelectionSection: boolean = false;
  p2Active: boolean = false;
  p3Active: boolean = false;
  cameraError: boolean = false;
  isChecking: boolean = false;
  successMessage: string = '';
  errorMessage: string = '';
  candidates: any = [];
  voterName: string = '';
  voterLocation: string = '';
  voterToken: string = '';

  ngOnInit() {

    this.electionID = this.route.snapshot.paramMap.get('id');

    // Timer

    const targetTime: any = new Date();
    targetTime.setMinutes(targetTime.getMinutes() + 5);

    setInterval(() => {
      const now = new Date();
      const diffMs = targetTime.getTime() - now.getTime();

      if (diffMs <= 0) {
        this.countdown = '0:00';
        this.p3Active = false;
        this.p2Active = true;
      };

      const totalSeconds = Math.floor(diffMs / 1000);
      const minuts = Math.floor(totalSeconds / 60);
      const seconds = totalSeconds % 60;


      this.countdown = `${minuts}:${seconds < 10 ? '0' : ''}${seconds}`;

    }, 1000);
  }

  showErrorMessage(message: string) {
    this.errorMessage = message
    setTimeout(() => {
      this.errorMessage = null
    }, 2000)
  }

  showSuccessMessage(message: string) {
    this.successMessage = message
    setTimeout(() => {
      this.successMessage = null
    }, 2000)
  }




  idForm = new FormGroup({
    id: new FormControl('', [Validators.required, Validators.minLength(14), Validators.pattern(/^\d+$/)])
  })

  onSubmit() {
    console.log(this.idForm.controls['id'].value);
    this.votingApi.verifyNationalNumber(this.electionID, this.idForm.controls['id'].value).subscribe({
      next: (res: any) => {
        this.voterName = res.voterName;
        this.voterLocation = res.location || '';
        this.nationalId = this.idForm.controls['id'].value;
        this.idVerifySection = false;
        this.pictureVerificationSection = true;
        this.p2Active = true;
        this.showSuccessMessage('تم التحقق من هويتك بنجاح');
        console.log(res);
      },
      error: (err) => {
        if (err.error.errors[0] === 'Voting.VoterNotFound') {
          this.showErrorMessage('ليس لديك صلاحيه للانتخاب بهذي الانتخابات');
          console.log(err)
        } else if (err.error.errors[0] == 'Voting.AlreadyVoted') {
          this.showErrorMessage('لقد قمت بالادلاء بصوتك بالفعل');
          console.log(err)
        } else if (err.error.errors[0] == 'Voting.ElectionNotActive') {
          this.showErrorMessage('لم يتم تفعيل الانتخابات بعد')
        } else {
          this.showErrorMessage('حدث خطأ');
          console.log(err)
        }
        console.log(err)
      }
    })
  }

  // Process 2 -- Picture Verification

  @ViewChild('video') video!: ElementRef<HTMLVideoElement>
  @ViewChild('canvas') canvas!: ElementRef<HTMLCanvasElement>

  capturedImage: string | null = null;

  cameraInitialized: boolean = false;

  ngAfterViewChecked() {
    if (!this.cameraInitialized && this.video) {
      this.openCamera();
      this.cameraInitialized = true;
      console.log('test');
    }
  }

  stream: MediaStream | null = null;

  async openCamera() {
    try {
      this.stream = await navigator.mediaDevices.getUserMedia({ video: true });
      this.video.nativeElement.srcObject = this.stream;
    } catch (err: any) {
      console.log(err);
      if (err == 'NotFoundError: Requested device not found') {
        this.cameraError = true;
      }
    }
  }

  closeCamera() {
    if (this.stream) {
      this.stream.getTracks().forEach((track) => track.stop());
      this.stream = null;
    }
  }
  capture() {
    const VideoEl = this.video.nativeElement;
    const canvasEl = this.canvas.nativeElement;

    canvasEl.width = VideoEl.videoWidth;
    canvasEl.height = VideoEl.videoHeight;

    const ctx = canvasEl.getContext('2d');
    ctx.drawImage(VideoEl, 0, 0);

    this.capturedImage = canvasEl.toDataURL('image/jpeg');
    this.closeCamera();
  }

  checkImgIdentity() {
    this.isChecking = true;

    // Get All Candidates Data For This Election For The Next Step


    this.organizersPanel.getCandidates(this.electionID).subscribe({
      next: (res) => {
        this.candidates = res;
        this.candidates = this.candidates.map(candidate => ({
          ...candidate,
          photoPath: candidate.photoPath || null
        }));
        console.log(this.candidates)

      },
      error: (err) => {
        console.log(err)
      }
    })


    this.votingApi.verifyPhoto(this.electionID, this.nationalId, this.capturedImage).subscribe({
      next: (res: any) => {
        console.log(res);
        this.isChecking = false;
        this.pictureVerificationSection = false;
        this.candidateSelectionSection = true;
        this.p3Active = true;
        this.voterToken = res.voterToken;
        this.showSuccessMessage('تم التحقق من هويتك بنجاح');
        console.log(this.voterToken);
      },
      error: (err) => {
        console.log(err);
        this.isChecking = false;
        this.capturedImage = null;
        this.cameraInitialized = false;
        if (err.error?.errors?.[0] === 'Voting.FaceVerificationFailed') {
          this.showErrorMessage('فشل التحقق من الوجه. يرجى المحاولة مرة أخرى بصورة أوضح');
        } else if (err.error?.errors?.[0] === 'Voting.VoterPhotoNotFound') {
          this.showErrorMessage('لم يتم العثور على صورتك. يرجى التواصل مع منظم الانتخابات');
        } else {
          this.showErrorMessage('حدث خطأ أثناء التحقق. يرجى المحاولة مرة أخرى');
        }
      }
    })
  }


  // Process 3
  countdown: string = ''


  candidatesForm = new FormGroup({
    candidate: new FormControl('null', Validators.required),
  })

  saveCandidateInfo() {

    let candidate: any = this.candidatesForm.get('candidate').value;

    let voteObj = {
      votedCandidate: candidate.id,
      votedCandidateElectionId: candidate.electionId,
    }
    console.log(voteObj, candidate);
  }

  castVote() {
    let candidate: any = this.candidatesForm.get('candidate').value;
    this.votingApi.castVote(this.electionID, candidate.id, this.voterToken).subscribe({
      next: (res) => {
        console.log(res);
        this.showSuccessMessage('تم الادلاء بصوتك بنجاح');
        setTimeout(() => {
          this.router.navigate(['/home']);
        }, 2000)
      },
      error: (err) => {
        console.log(err);
        if (err.error.errors[0] === 'Voting.AlreadyVoted') {
          this.showErrorMessage('لقد قمت بالادلاء بصوتك بالفعل');
        } else {
          this.showErrorMessage('حدث خطأ');
        }
      }
    })
  }

}
