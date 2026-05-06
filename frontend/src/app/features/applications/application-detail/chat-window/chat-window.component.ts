import { DatePipe, NgClass } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import {
  afterNextRender,
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  OnInit,
  computed,
  inject,
  Injector,
  input,
  signal,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { Button } from 'primeng/button';
import { Textarea } from 'primeng/textarea';
import { catchError, of, switchMap, timer } from 'rxjs';

import { AuthService } from '../../../../core/auth/auth.service';
import type { ApplicationStatusCode, ChatMessageDto } from '../../../../core/models/application.models';
import type { ProblemDetails } from '../../../../core/models/common.models';
import { ChatApiService } from '../../chat-api.service';

const CHAT_ACTIVE_STATUSES: ApplicationStatusCode[] = [
  'OnEditing',
  'Pending',
  'ApprovedBySupervisor',
  'PendingDepartmentHead',
];

/** Порог в px: если пользователь ближе к низу — при polling подстраиваем скролл к новым сообщениям. */
const STICK_TO_BOTTOM_PX = 80;

@Component({
  selector: 'app-chat-window',
  imports: [ReactiveFormsModule, Textarea, Button, DatePipe, NgClass],
  templateUrl: './chat-window.component.html',
  styleUrl: './chat-window.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ChatWindowComponent implements OnInit {
  private readonly chatApi = inject(ChatApiService);
  private readonly auth = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);

  private readonly messagesScroll = viewChild<ElementRef<HTMLElement>>('messagesScroll');

  readonly applicationId = input.required<string>();
  readonly applicationStatus = input.required<ApplicationStatusCode>();

  readonly messages = signal<ChatMessageDto[]>([]);
  readonly isLoading = signal(true);
  readonly isSending = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly messageControl = new FormControl('', {
    nonNullable: true,
    validators: [Validators.required, Validators.maxLength(4000)],
  });

  /** Для OnPush: FormControl не сигнал — дергаем computed при вводе. */
  private readonly messageEditTick = signal(0);

  readonly currentUserId = computed(() => this.auth.currentUser()?.userId ?? null);
  readonly isChatActive = computed(() => CHAT_ACTIVE_STATUSES.includes(this.applicationStatus()));
  readonly canSend = computed(() => {
    this.messageEditTick();
    if (!this.isChatActive() || this.isSending()) {
      return false;
    }
    return this.messageControl.valid && this.messageControl.value.trim().length > 0;
  });
  readonly charsUsed = computed(() => {
    this.messageEditTick();
    return this.messageControl.value.length;
  });
  readonly charsLeft = computed(() => 4000 - this.charsUsed());

  constructor() {
    this.messageControl.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => {
      this.messageEditTick.update((n) => n + 1);
    });
  }

  ngOnInit(): void {
    this.loadInitialMessages();
    this.startPolling();
  }

  isOwnMessage(message: ChatMessageDto): boolean {
    return !!this.currentUserId() && message.senderId === this.currentUserId();
  }

  sendMessage(): void {
    if (!this.canSend()) {
      this.messageControl.markAsTouched();
      return;
    }

    const content = this.messageControl.value.trim();
    this.isSending.set(true);
    this.errorMessage.set(null);

    this.chatApi.sendMessage(this.applicationId(), content).subscribe({
      next: () => {
        this.messageControl.reset('');
        this.messageControl.markAsPristine();
        this.messageControl.markAsUntouched();
        this.isSending.set(false);
        this.chatApi.getMessages(this.applicationId()).subscribe({
          next: (fresh) => {
            this.messages.set(fresh);
            this.scheduleScrollToBottom();
          },
          error: () => {},
        });
      },
      error: (err: HttpErrorResponse) => {
        this.errorMessage.set(this.resolveError(err, 'Не удалось отправить сообщение.'));
        this.isSending.set(false);
      },
    });
  }

  onMessageKeyDown(event: KeyboardEvent): void {
    if (event.ctrlKey && (event.key === 'Enter' || event.key === ' ')) {
      event.preventDefault();
      this.sendMessage();
    }
  }

  private loadInitialMessages(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.chatApi.getMessages(this.applicationId()).subscribe({
      next: (rows) => {
        this.messages.set(rows);
        this.isLoading.set(false);
        this.markIncomingReadIfNeeded(rows);
        this.scheduleScrollToBottom();
      },
      error: (err: HttpErrorResponse) => {
        this.errorMessage.set(this.resolveError(err, 'Не удалось загрузить сообщения чата.'));
        this.isLoading.set(false);
      },
    });
  }

  private startPolling(): void {
    timer(5_000, 5_000)
      .pipe(
        switchMap(() =>
          this.chatApi.getMessages(this.applicationId()).pipe(
            // Сбой одного запроса не должен останавливать polling.
            catchError(() => of(null)),
          ),
        ),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((rows) => {
        if (rows) {
          const stickToBottom = this.shouldStickToBottom();
          this.messages.set(rows);
          this.markIncomingReadIfNeeded(rows);
          if (stickToBottom) {
            this.scheduleScrollToBottom();
          }
        }
      });
  }

  /**
   * readAt на сервере выставляется только через read-all. Сообщения, пришедшие после первого открытия чата,
   * иначе остаются «непрочитанными» у отправителя, пока получатель снова не вызовет read-all.
   */
  private markIncomingReadIfNeeded(rows: ChatMessageDto[]): void {
    if (!this.isChatActive()) {
      return;
    }
    const uid = this.currentUserId();
    if (!uid) {
      return;
    }
    const hasUnreadFromOthers = rows.some((m) => m.senderId !== uid && !m.readAt);
    if (!hasUnreadFromOthers) {
      return;
    }
    this.chatApi
      .markAllAsRead(this.applicationId())
      .pipe(
        switchMap(() => this.chatApi.getMessages(this.applicationId())),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (fresh) => this.messages.set(fresh),
        error: () => {},
      });
  }

  private resolveError(err: HttpErrorResponse, fallback: string): string {
    const detail = (err.error as ProblemDetails | null)?.detail?.trim();
    return detail || fallback;
  }

  private shouldStickToBottom(): boolean {
    const el = this.messagesScroll()?.nativeElement;
    if (!el) {
      return true;
    }
    return el.scrollHeight - el.scrollTop - el.clientHeight <= STICK_TO_BOTTOM_PX;
  }

  private scheduleScrollToBottom(): void {
    afterNextRender(
      () => {
        const el = this.messagesScroll()?.nativeElement;
        if (el) {
          el.scrollTop = el.scrollHeight;
        }
      },
      { injector: this.injector },
    );
  }
}
