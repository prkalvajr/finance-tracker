import { Routes } from '@angular/router';

import { authGuard } from './core/guards/auth.guard';
import { MainLayoutComponent } from './layouts/main-layout/main-layout.component';
import { AuthLayoutComponent } from './layouts/auth-layout/auth-layout.component';
import { HomePageComponent } from './pages/home/home-page.component';
import { ProfilePageComponent } from './pages/profile/profile-page.component';
import { LoginPageComponent } from './pages/login/login-page.component';
import { RegisterPageComponent } from './pages/register/register-page.component';

export const routes: Routes = [
  {
    path: '',
    component: MainLayoutComponent,
    canActivate: [authGuard],
    children: [
      { path: 'home', component: HomePageComponent },
      { path: 'profile', component: ProfilePageComponent },
      { path: '', pathMatch: 'full', redirectTo: 'home' }
    ]
  },
  {
    path: '',
    component: AuthLayoutComponent,
    children: [
      { path: 'login', component: LoginPageComponent },
      { path: 'register', component: RegisterPageComponent }
    ]
  },
  { path: '**', redirectTo: 'home' }
];
