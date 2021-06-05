import { HttpClient, HttpHeaders } from '@angular/common/http';
import { BaseService } from './base.service';
import { Injectable } from '@angular/core';
import { catchError } from 'rxjs/operators';
import { User } from '../models/user.model';
import { environment } from '@environments/environment';

@Injectable({ providedIn: 'root' })
export class UserService extends BaseService {
    constructor(private http: HttpClient) {
        super();
    }
    getAll() {
        const httpOptions = {
            headers: new HttpHeaders({
                'Content-Type': 'application/json'
            })
        };
        return this.http.get<User[]>(`${environment.apiUrl}/api/users`, httpOptions)
            .pipe(catchError(this.handleError));
    }
}