import { inject, Injectable } from "@angular/core";
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, tap } from "rxjs";
import { Router } from "@angular/router";
import { Admin } from "../models/Admin.model";
import { Organizer } from "../models/organizers.model";
import { environment } from "../../environments/environment";

@Injectable({
  providedIn: 'root'
})
export class ApiAuthService {
  http: HttpClient = inject(HttpClient);
  baseUrl: string = `${environment.apiUrl}/api`;
  baseLoginURL: string = `${environment.apiUrl}/api/auth/login`;
  user = new BehaviorSubject<Admin | null>(null);
  router: Router = inject(Router);
  userID: any;

  login(email, password) {
    const data = { email: email, password: password };
    return this.http.post<Admin>(this.baseLoginURL, data)
      .pipe(tap((res) => {
        this.handleCreateUser(res);
        localStorage.setItem('userID', JSON.stringify(res.adminId));
      }))
  }

  autoLogin() {
    const loggedUser = JSON.parse(localStorage.getItem('user'));

    if (!loggedUser) {
      return
    }

    let user = new Admin(loggedUser.email, loggedUser.username, loggedUser.role, loggedUser.adminId, loggedUser.organizerId, loggedUser.expiresAt, loggedUser._token);

    if (user.token) {
      this.user.next(user);
    }
  }

  logout() {
    this.user.next(null);
    this.router.navigate(['/login']);
    localStorage.removeItem('user');
    localStorage.removeItem('userID');
  }

  handleCreateUser(res: Admin) {
    const expiresIn = new Date(res.expiresAt);
    const user = new Admin(res.email, res.username, res.role, res.adminId, res.organizerId, expiresIn, res.token);
    localStorage.setItem('user', JSON.stringify(user));
    this.user.next(user);
  }

  createOrganizer(data: Organizer) {
    return this.http.post(`${this.baseUrl}/auth/register/organizer`, data);
  }

}
