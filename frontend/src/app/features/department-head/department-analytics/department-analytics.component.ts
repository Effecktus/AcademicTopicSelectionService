import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { Button } from 'primeng/button';
import { UIChart } from 'primeng/chart';

import type { DepartmentHeadAnalyticsDto } from '../../../core/models/department-head.models';
import { DepartmentHeadApiService } from '../department-head-api.service';

const PALETTE = [
  '#6366f1', '#22c55e', '#f59e0b', '#ef4444', '#8b5cf6',
  '#06b6d4', '#ec4899', '#84cc16',
];

const CHART_DEFAULTS = {
  responsive: true,
  maintainAspectRatio: false,
};

@Component({
  selector: 'app-department-analytics',
  imports: [Button, UIChart],
  templateUrl: './department-analytics.component.html',
  styleUrl: './department-analytics.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DepartmentAnalyticsComponent {
  private readonly api = inject(DepartmentHeadApiService);

  readonly data = signal<DepartmentHeadAnalyticsDto | null>(null);
  readonly isLoading = signal(true);
  readonly errorMessage = signal<string | null>(null);

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

  readonly workloadChartData = computed(() => {
    const d = this.data();
    if (!d || d.teacherWorkload.length === 0) return null;
    return {
      labels: d.teacherWorkload.map(t => t.teacherFullName),
      datasets: [{
        label: 'Активных студентов',
        data: d.teacherWorkload.map(t => t.activeStudentsCount),
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

  private load(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.api.getAnalytics().subscribe({
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
