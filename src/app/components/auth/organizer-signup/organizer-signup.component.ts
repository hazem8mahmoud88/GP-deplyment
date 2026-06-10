import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { ReactiveFormsModule, FormGroup, FormControl, Validators } from '@angular/forms';
import { MessageBoxComponent } from 'src/app/utilities/message-box/message-box.component';
import { Router } from '@angular/router';
import { ApiAuthService } from 'src/app/services/authAPI.service';
import { Organizer } from 'src/app/models/organizers.model';


@Component({
  standalone: true,
  selector: 'app-organizer-signup',
  imports: [CommonModule, MessageBoxComponent, ReactiveFormsModule],
  templateUrl: './organizer-signup.component.html',
  styleUrls: ['./organizer-signup.component.css']
})
export class OrganizerSignupComponent {
  authService: ApiAuthService = inject(ApiAuthService);
  signupError: string = '';
  signupSuccess: string = '';
  router: Router = inject(Router);

  OrgSignupForm = new FormGroup({
    email: new FormControl('', [Validators.required, Validators.email]),
    username: new FormControl('', Validators.required),
    password: new FormControl('', [Validators.required, Validators.pattern('^(?=.*[a-z])(?=.*[A-Z])(?=.*[^a-zA-Z0-9]).+$')]),
    fullname: new FormControl('', Validators.required),
    orgName: new FormControl('', Validators.required),
    phoneNumber: new FormControl('', [Validators.required, Validators.pattern('^[0-9]+$'), Validators.maxLength(15)])

  })


  onSubmit() {
    let data: Organizer = {
      email: this.OrgSignupForm.controls.email.value,
      username: this.OrgSignupForm.controls.username.value.replace(/\s+/g, ''),
      password: this.OrgSignupForm.controls.password.value,
      fullname: this.OrgSignupForm.controls.fullname.value,
      orgName: this.OrgSignupForm.controls.orgName.value,
      phoneNumber: this.OrgSignupForm.controls.phoneNumber.value,
    }

    this.authService.createOrganizer(data).subscribe({
      next: (res) => {
        this.signupSuccess = "تم انشاء المنظم بنجاح"
      },
      error: (err) => {
        this.signupError = "عذرا حدث خطأ. حاول مره اخري"
      }
    })

    this.OrgSignupForm.reset();

  }


}
