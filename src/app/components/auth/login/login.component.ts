import { Component, inject } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ApiAuthService } from 'src/app/services/authAPI.service';
import { MessageBoxComponent } from 'src/app/utilities/message-box/message-box.component';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';

@Component({
  standalone: true,
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css'],
  imports: [ReactiveFormsModule, MessageBoxComponent, CommonModule],
})
export class LoginComponent {

  loginAuthService: ApiAuthService = inject(ApiAuthService);
  loginError: string = '';
  loginSuccess: string = '';
  router: Router = inject(Router);

  loginForm = new FormGroup({
    email: new FormControl('', [Validators.required, Validators.email]),
    password: new FormControl('', Validators.required)
  })

  onSubmit() {
    this.loginAuthService.login(this.loginForm.get('email').value, this.loginForm.get('password').value).subscribe({
      next: (res) => {
        this.loginSuccess = 'LoggedIn Successfully'
        this.router.navigate(['/dashboard']);
      },
      error: (err) => {
        this.handleError(err);
      },
    });
  }

  handleError(err) {
    switch (true) {
      case err.error.errors.includes('Auth.InvalidCredentials'):
        this.loginError = 'تأكد من البريد الاكتروني او كلمه السر و حاول مجددا.'
    }
    setTimeout(() => {
      this.loginError = '';
    }, 3000)
  }

}
