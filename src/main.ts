
import { bootstrapApplication } from '@angular/platform-browser';
import { AppComponent } from './app/app.component';
import { provideRouter, withInMemoryScrolling } from '@angular/router';
import { AppRoutes } from './app/AppRouting';
import { importProvidersFrom } from '@angular/core';
import { HttpClientModule, provideHttpClient, withInterceptors } from '@angular/common/http';
import { authInterceptor } from './app/interceptors/auth-interceptor.service';

bootstrapApplication(AppComponent, {
  providers: [
    provideRouter(AppRoutes,
      withInMemoryScrolling({
        scrollPositionRestoration: 'enabled'
      })
    ),
    importProvidersFrom(HttpClientModule),
    provideHttpClient(withInterceptors([authInterceptor]))
  ]
})
