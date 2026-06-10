import { Component, inject, OnInit } from '@angular/core';
import { HeaderComponent } from './components/header/header.component';
import { MainHomeComponent } from './components/main-home/main-home.component';
import { RouterModule } from "@angular/router";
import { BrowserModule } from '@angular/platform-browser';
import { ApiAuthService } from './services/authAPI.service';
import { FooterComponent } from './components/footer/footer.component';

@Component({
  standalone: true,
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css'],
  imports: [HeaderComponent, RouterModule, FooterComponent],
})
export class AppComponent implements OnInit{
  title = 'secure-vote';

  authService: ApiAuthService = inject(ApiAuthService);

  ngOnInit() {
    this.authService.autoLogin();
  }


}
