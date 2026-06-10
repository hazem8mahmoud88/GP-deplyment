import { inject, OnDestroy, OnInit } from '@angular/core';
import { Component } from '@angular/core';
import { RouterModule } from "@angular/router";
import { CommonModule } from '@angular/common';
import { ApiAuthService } from 'src/app/services/authAPI.service';
import { Organizer } from 'src/app/models/organizers.model';
import { Subscription, take } from 'rxjs';
import { Admin } from 'src/app/models/Admin.model';

@Component({
  standalone: true,
  selector: 'app-header',
  templateUrl: './header.component.html',
  styleUrls: ['./header.component.css'],
  imports: [RouterModule, CommonModule]
})
export class HeaderComponent implements OnInit, OnDestroy {
  authService:ApiAuthService = inject(ApiAuthService);
  isAdmin: boolean;
  isLogged: boolean = false;
  userName: string = '';
  unsubscribe: Subscription;

  ngOnInit() {
    this.unsubscribe = this.authService.user.subscribe((user: Admin | null) => {
      this.isLogged = !!user;
      this.isAdmin = user?.role === 'Admin';
      this.userName = user?.username || '';
    });
  }

  ngOnDestroy() {
    this.unsubscribe.unsubscribe();
  }

  logoutUser()  {
    this.authService.logout();
  }
}
