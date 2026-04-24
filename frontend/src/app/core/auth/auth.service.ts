import { HttpBackend, HttpClient } from '@angular/common/http';
import { Injectable, computed, signal } from '@angular/core';
import { Observable, catchError, finalize, firstValueFrom, map, of, share, tap } from 'rxjs';

import { environment } from '../../../environments/environment';
import type { AccessTokenDto, UserInfo } from '../models/auth.models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  /** HTTP без interceptors — избегает цикла AuthService ↔ HttpClient. */
  private readonly httpRaw: HttpClient;

  private readonly _currentUser = signal<UserInfo | null>(null);
  readonly currentUser = this._currentUser.asReadonly();

  readonly isLoggedIn = computed(() => this._currentUser() !== null);
  readonly role = computed(() => this._currentUser()?.role ?? null);

  private readonly _accessToken = signal<string | null>(null);

  private refreshInFlight: Observable<string> | null = null;

  constructor(httpBackend: HttpBackend) {
    this.httpRaw = new HttpClient(httpBackend);
  }

  getAccessToken(): string | null {
    return this._accessToken();
  }

  login(email: string, password: string): Observable<void> {
    return this.httpRaw
      .post<AccessTokenDto>(`${environment.apiUrl}/auth/login`, { email, password }, { withCredentials: true })
      .pipe(tap((dto) => this.applyAccessResponse(dto)), map(() => void 0));
  }

  logout(): Observable<void> {
    return this.httpRaw
      .post<void>(`${environment.apiUrl}/auth/logout`, {}, { withCredentials: true })
      .pipe(
        catchError(() => of(void 0)),
        tap(() => this.clearSession()),
        map(() => void 0),
      );
  }

  /** Ротация refresh-токена (cookie); возвращает новый access-токен. */
  refresh(): Observable<string> {
    if (this.refreshInFlight) {
      return this.refreshInFlight;
    }

    this.refreshInFlight = this.httpRaw
      .post<AccessTokenDto>(`${environment.apiUrl}/auth/refresh`, {}, { withCredentials: true })
      .pipe(
        tap((dto) => this.applyAccessResponse(dto)),
        map((dto) => dto.accessToken),
        finalize(() => {
          this.refreshInFlight = null;
        }),
        share(),
      );

    return this.refreshInFlight;
  }

  /** Вызывается при инициализации приложения: пытаемся восстановить сессию через refresh cookie. */
  async restoreSession(): Promise<void> {
    try {
      const dto = await firstValueFrom(
        this.httpRaw.post<AccessTokenDto>(
          `${environment.apiUrl}/auth/refresh`,
          {},
          { withCredentials: true },
        ),
      );
      this.applyAccessResponse(dto);
    } catch {
      this.clearSession();
    }
  }

  clearSession(): void {
    this._accessToken.set(null);
    this._currentUser.set(null);
  }

  private applyAccessResponse(dto: AccessTokenDto): void {
    this._accessToken.set(dto.accessToken);
    this._currentUser.set({
      fullName: dto.fullName,
      userId: dto.userId,
      email: dto.email,
      role: dto.role,
    });
  }
}
