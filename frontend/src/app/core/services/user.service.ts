import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { UpdateUserRequest, UserDto } from '../../models/user.models';
import { API_BASE_URL } from '../http/api-config';

@Injectable({ providedIn: 'root' })
export class UserService {
  private readonly http = inject(HttpClient);
  private readonly apiBase = inject(API_BASE_URL);

  getCurrentUser(): Observable<UserDto> {
    return this.http.get<UserDto>(`${this.apiBase}/user`, { withCredentials: true });
  }

  updateUser(req: UpdateUserRequest): Observable<UserDto> {
    return this.http.put<UserDto>(`${this.apiBase}/update`, req, { withCredentials: true });
  }
}
