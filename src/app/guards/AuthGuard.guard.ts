import { inject, OnInit } from "@angular/core";
import { ApiAuthService } from "../services/authAPI.service";
import { ActivatedRoute, Router, RouterStateSnapshot } from "@angular/router";
import { take, map } from "rxjs";

export const canActivateAuth = (router: ActivatedRoute, status: RouterStateSnapshot) => {
  const authService: ApiAuthService = inject(ApiAuthService);
  const route: Router  = inject(Router);

  return authService.user.pipe(take(1), map((user) => {
    if (user.role === 'Admin' || 'Organizer') {
      return true;
    } else {
      authService.logout();
      return route.createUrlTree(['/login']);
    }
  }))

}
