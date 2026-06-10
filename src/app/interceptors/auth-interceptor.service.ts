import { HttpInterceptorFn, HttpParams } from "@angular/common/http";
import { ApiAuthService } from "../services/authAPI.service";
import { inject } from "@angular/core";
import { switchMap, take } from "rxjs";

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService: ApiAuthService = inject(ApiAuthService);

  return authService.user.pipe(
    take(1),
    switchMap(user => {
      if (!user) {
        return next(req)
      }

      const modifiedReq = req.clone({
        headers: req.headers.set('Authorization', `Bearer ${user.token}`)
      });

      return next(modifiedReq);
    })

  )
}
