import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import {
  CreateTransactionRequest,
  TransactionDto,
  TransactionQueryParams,
  TransactionSummaryDto,
  UpdateTransactionRequest
} from '../../models/transaction.models';
import { PagedResult } from '../../models/paged-result.models';
import { API_BASE_URL } from '../http/api-config';

@Injectable({ providedIn: 'root' })
export class TransactionsService {
  private readonly http = inject(HttpClient);
  private readonly apiBase = inject(API_BASE_URL);

  readonly transactions = signal<TransactionDto[]>([]);
  readonly summary = signal<TransactionSummaryDto | null>(null);
  readonly loading = signal(false);

  async getPaged(query: TransactionQueryParams): Promise<PagedResult<TransactionDto>> {
    this.loading.set(true);
    try {
      let params = new HttpParams();
      if (query.page != null)     params = params.set('page', String(query.page));
      if (query.pageSize != null) params = params.set('pageSize', String(query.pageSize));
      if (query.sortBy)           params = params.set('sortBy', query.sortBy);
      if (query.sortOrder)        params = params.set('sortOrder', query.sortOrder);
      if (query.category)         params = params.set('category', query.category);
      if (query.dateFrom)         params = params.set('dateFrom', query.dateFrom);
      if (query.dateTo)           params = params.set('dateTo', query.dateTo);

      const result = await firstValueFrom(
        this.http.get<PagedResult<TransactionDto>>(`${this.apiBase}/transactions`, {
          params,
          withCredentials: true
        })
      );
      this.transactions.set(result.items);
      return result;
    } finally {
      this.loading.set(false);
    }
  }

  async getSummary(): Promise<TransactionSummaryDto> {
    const result = await firstValueFrom(
      this.http.get<TransactionSummaryDto>(`${this.apiBase}/transactions/summary`, {
        withCredentials: true
      })
    );
    this.summary.set(result);
    return result;
  }

  async create(req: CreateTransactionRequest): Promise<TransactionDto> {
    this.loading.set(true);
    try {
      const created = await firstValueFrom(
        this.http.post<TransactionDto>(`${this.apiBase}/transactions`, req, {
          withCredentials: true
        })
      );
      await this.getPaged({ page: 1, pageSize: 10 });
      return created;
    } finally {
      this.loading.set(false);
    }
  }

  async update(req: UpdateTransactionRequest): Promise<TransactionDto> {
    this.loading.set(true);
    try {
      const updated = await firstValueFrom(
        this.http.put<TransactionDto>(
          `${this.apiBase}/transactions/${req.transactionId}`,
          req,
          { withCredentials: true }
        )
      );
      await this.getPaged({ page: 1, pageSize: 10 });
      return updated;
    } finally {
      this.loading.set(false);
    }
  }

  async delete(id: number): Promise<void> {
    this.loading.set(true);
    try {
      await firstValueFrom(
        this.http.delete<void>(`${this.apiBase}/transactions/${id}`, {
          withCredentials: true
        })
      );
      await this.getPaged({ page: 1, pageSize: 10 });
    } finally {
      this.loading.set(false);
    }
  }
}
