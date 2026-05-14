import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  OnInit,
  computed,
  inject,
  input,
  signal
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatSortModule, Sort } from '@angular/material/sort';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

import { GridColumn, GridFetcher, GridQuery } from './grid.types';

@Component({
  selector: 'app-grid',
  standalone: true,
  imports: [
    CommonModule,
    MatTableModule,
    MatSortModule,
    MatPaginatorModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './grid.component.html',
  styleUrl: './grid.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class GridComponent<T> implements OnInit {
  readonly columns = input.required<GridColumn<T>[]>();
  readonly fetch = input.required<GridFetcher<T>>();
  readonly pageSize = input(10);

  private readonly cdr = inject(ChangeDetectorRef);

  readonly rows = signal<T[]>([]);
  readonly totalCount = signal(0);
  readonly loading = signal(false);
  readonly errorOccurred = signal(false);

  readonly columnKeys = computed(() => this.columns().map(c => c.key));

  private _page = 1;
  _pageSize = 10;
  private _sortBy: string | undefined;
  private _sortOrder: 'asc' | 'desc' | undefined;
  private _filters: Record<string, unknown> = {};

  ngOnInit(): void {
    this._pageSize = this.pageSize();
    this.loadData();
  }

  onSortChange(sort: Sort): void {
    if (sort.active && sort.direction) {
      this._sortBy = sort.active;
      this._sortOrder = sort.direction as 'asc' | 'desc';
    } else {
      this._sortBy = undefined;
      this._sortOrder = undefined;
    }
    this._page = 1;
    this.loadData();
  }

  onPageChange(event: PageEvent): void {
    if (event.pageSize !== this._pageSize) {
      this._pageSize = event.pageSize;
      this._page = 1;
    } else {
      this._page = event.pageIndex + 1;
    }
    this.loadData();
  }

  applyFilters(filters: Record<string, unknown>): void {
    this._filters = filters;
    this._page = 1;
    this.loadData();
  }

  refresh(): void {
    this.loadData();
  }

  getCell(col: GridColumn<T>, row: T): string {
    return col.cell ? col.cell(row) : String((row as Record<string, unknown>)[col.key] ?? '');
  }

  private async loadData(): Promise<void> {
    this.loading.set(true);
    this.errorOccurred.set(false);

    const query: GridQuery = {
      page: this._page,
      pageSize: this._pageSize,
      sortBy: this._sortBy,
      sortOrder: this._sortOrder,
      filters: this._filters
    };

    try {
      const result = await this.fetch()(query);
      this.rows.set(result.items);
      this.totalCount.set(result.totalCount);
    } catch {
      this.rows.set([]);
      this.errorOccurred.set(true);
    } finally {
      this.loading.set(false);
      this.cdr.markForCheck();
    }
  }
}
