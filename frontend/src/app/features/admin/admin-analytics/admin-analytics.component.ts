import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Button } from 'primeng/button';
import { UIChart } from 'primeng/chart';
import { Select } from 'primeng/select';

import type { AdminAnalyticsDto } from '../../../core/models/admin.models';
import { AdminApiService } from '../admin-api.service';

const MONTHS_RU = ['Янв', 'Фев', 'Мар', 'Апр', 'Май', 'Июн', 'Июл', 'Авг', 'Сен', 'Окт', 'Ноя', 'Дек'];

const PALETTE = [
  '#6366f1', '#22c55e', '#f59e0b', '#ef4444', '#8b5cf6',
  '#06b6d4', '#ec4899', '#84cc16',
];

const CHART_DEFAULTS = {
  responsive: true,
  maintainAspectRatio: false,
};

interface YearOption {
  label: string;
  value: number | null;
}

@Component({
  selector: 'app-admin-analytics',
  imports: [Button, UIChart, Select, FormsModule],
  templateUrl: './admin-analytics.component.html',
  styleUrl: './admin-analytics.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminAnalyticsComponent {
  private readonly adminApi = inject(AdminApiService);

  readonly data = signal<AdminAnalyticsDto | null>(null);
  readonly isLoading = signal(true);
  readonly errorMessage = signal<string | null>(null);
  readonly currentYear = new Date().getFullYear();

  /** Выбранный год; null = «Все время». Обычное свойство для [(ngModel)]. */
  selectedYear: number | null = null;

  readonly yearOptions: YearOption[] = (() => {
    const opts: YearOption[] = [{ label: 'Все время', value: null }];
    for (let y = this.currentYear; y >= 2024; y--) {
      opts.push({ label: String(y), value: y });
    }
    return opts;
  })();

  readonly statusChartData = computed(() => {
    const d = this.data();
    if (!d || d.applicationsByStatus.length === 0) return null;
    return {
      labels: d.applicationsByStatus.map(s => s.statusDisplayName),
      datasets: [{
        data: d.applicationsByStatus.map(s => s.count),
        backgroundColor: PALETTE.slice(0, d.applicationsByStatus.length),
        hoverBackgroundColor: PALETTE.slice(0, d.applicationsByStatus.length),
      }],
    };
  });

  readonly gwYearChartData = computed(() => {
    const d = this.data();
    if (!d || d.gwByYear.length === 0) return null;
    const sorted = [...d.gwByYear].sort((a, b) => a.year - b.year);
    return {
      labels: sorted.map(y => String(y.year)),
      datasets: [{
        label: 'ВКР',
        data: sorted.map(y => y.count),
        backgroundColor: '#6366f1cc',
        borderColor: '#6366f1',
        borderWidth: 1,
      }],
    };
  });

  readonly monthChartData = computed(() => {
    const d = this.data();
    const byMonth = new Map(d?.applicationsByMonth.map(m => [m.month, m.count]) ?? []);
    return {
      labels: MONTHS_RU,
      datasets: [{
        label: 'Заявки',
        data: Array.from({ length: 12 }, (_, i) => byMonth.get(i + 1) ?? 0),
        borderColor: '#6366f1',
        backgroundColor: 'rgba(99,102,241,0.12)',
        fill: true,
        pointBackgroundColor: '#6366f1',
        pointRadius: 4,
      }],
    };
  });

  readonly topTeachersChartData = computed(() => {
    const d = this.data();
    if (!d || d.topTeachersByApplications.length === 0) return null;
    return {
      labels: d.topTeachersByApplications.map(t => t.teacherFullName),
      datasets: [{
        label: 'Заявок',
        data: d.topTeachersByApplications.map(t => t.count),
        backgroundColor: '#6366f1cc',
        borderColor: '#6366f1',
        borderWidth: 1,
      }],
    };
  });

  readonly doughnutOptions = {
    ...CHART_DEFAULTS,
    cutout: '58%',
    plugins: {
      legend: {
        position: 'right',
        labels: { padding: 12, boxWidth: 12, font: { size: 11 } },
      },
    },
  };

  readonly barOptions = {
    ...CHART_DEFAULTS,
    plugins: { legend: { display: false } },
    scales: {
      y: { beginAtZero: true, ticks: { precision: 0 } },
    },
  };

  readonly lineOptions = {
    ...CHART_DEFAULTS,
    plugins: { legend: { display: false } },
    scales: {
      y: { beginAtZero: true, ticks: { precision: 0 } },
    },
    elements: { line: { tension: 0.4 } },
  };

  readonly horizontalBarOptions = {
    ...CHART_DEFAULTS,
    indexAxis: 'y' as const,
    plugins: { legend: { display: false } },
    scales: {
      x: { beginAtZero: true, ticks: { precision: 0 } },
    },
  };

  constructor() {
    this.load();
  }

  reload(): void {
    this.load();
  }

  /** Вызывается после изменения года в выпадающем списке. */
  onYearChange(): void {
    this.load();
  }

  private load(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.adminApi.getAnalytics(this.selectedYear).subscribe({
      next: (result) => {
        this.data.set(result);
        this.isLoading.set(false);
      },
      error: () => {
        this.errorMessage.set('Не удалось загрузить аналитику.');
        this.isLoading.set(false);
      },
    });
  }
}
