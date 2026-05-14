import { TemplateRef } from '@angular/core';

export interface GridColumn<T> {
  key: string;
  label: string;
  sortable: boolean;
  cell?: (row: T) => string;
  template?: TemplateRef<unknown>;
}

export interface GridQuery {
  page: number;       // 1-based
  pageSize: number;
  sortBy?: string;
  sortOrder?: 'asc' | 'desc';
  filters: Record<string, unknown>;
}

export type GridFetcher<T> = (query: GridQuery) => Promise<{ items: T[]; totalCount: number }>;

export type GridLoader = (query: GridQuery) => void | Promise<void>;
