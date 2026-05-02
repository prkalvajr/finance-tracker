import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import { TransactionsService } from './transactions.service';
import { TransactionDto, TransactionSummaryDto } from '../../models/transaction.models';
import { PagedResult } from '../../models/paged-result.models';

const API = 'http://localhost:5283';

function makeTransaction(overrides: Partial<TransactionDto> = {}): TransactionDto {
  return {
    transactionId: 1,
    userId: 1,
    title: 'Groceries',
    amount: 50,
    category: 'expense',
    date: '2025-01-15',
    createdAt: '2025-01-15T10:00:00Z',
    updatedAt: '2025-01-15T10:00:00Z',
    ...overrides
  };
}

function makePagedResult(items: TransactionDto[] = [makeTransaction()]): PagedResult<TransactionDto> {
  return { items, totalCount: items.length, page: 1, pageSize: 10, totalPages: 1 };
}

describe('TransactionsService', () => {
  let service: TransactionsService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([])),
        provideHttpClientTesting()
      ]
    });
    service = TestBed.inject(TransactionsService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  it('1: getPaged() calls GET /transactions with query params and updates signal', async () => {
    const paged = makePagedResult();
    const promise = service.getPaged({
      page: 2,
      pageSize: 5,
      sortBy: 'amount',
      sortOrder: 'desc',
      category: 'expense',
      dateFrom: '2025-01-01',
      dateTo: '2025-01-31'
    });

    const req = httpMock.expectOne(r => r.url === `${API}/transactions`);
    expect(req.request.method).toBe('GET');
    expect(req.request.withCredentials).toBe(true);
    expect(req.request.params.get('page')).toBe('2');
    expect(req.request.params.get('pageSize')).toBe('5');
    expect(req.request.params.get('sortBy')).toBe('amount');
    expect(req.request.params.get('sortOrder')).toBe('desc');
    expect(req.request.params.get('category')).toBe('expense');
    expect(req.request.params.get('dateFrom')).toBe('2025-01-01');
    expect(req.request.params.get('dateTo')).toBe('2025-01-31');
    req.flush(paged);

    const result = await promise;
    expect(result).toEqual(paged);
    expect(service.transactions()).toEqual(paged.items);
  });

  it('2: getSummary() calls GET /transactions/summary and updates summary signal', async () => {
    const summary: TransactionSummaryDto = { netBalance: 500, totalIncome: 1000, totalExpense: 500 };
    const promise = service.getSummary();

    const req = httpMock.expectOne(`${API}/transactions/summary`);
    expect(req.request.method).toBe('GET');
    expect(req.request.withCredentials).toBe(true);
    req.flush(summary);

    const result = await promise;
    expect(result).toEqual(summary);
    expect(service.summary()).toEqual(summary);
  });

  it('3: create() POSTs payload and refreshes list', async () => {
    const created = makeTransaction({ transactionId: 42, title: 'Salary', category: 'income', amount: 3000 });
    const refreshed = makePagedResult([created]);

    const promise = service.create({ title: 'Salary', amount: 3000, category: 'income' });

    const postReq = httpMock.expectOne(`${API}/transactions`);
    expect(postReq.request.method).toBe('POST');
    expect(postReq.request.body).toEqual({ title: 'Salary', amount: 3000, category: 'income' });
    postReq.flush(created);

    await Promise.resolve();
    await Promise.resolve();

    const getReq = httpMock.expectOne(r => r.url === `${API}/transactions`);
    expect(getReq.request.method).toBe('GET');
    getReq.flush(refreshed);

    const result = await promise;
    expect(result).toEqual(created);
    expect(service.transactions()).toEqual(refreshed.items);
  });

  it('4: update() PUTs payload and refreshes list', async () => {
    const updated = makeTransaction({ title: 'Updated', amount: 99 });
    const refreshed = makePagedResult([updated]);

    const promise = service.update({ transactionId: 1, title: 'Updated', amount: 99, category: 'expense' });

    const putReq = httpMock.expectOne(`${API}/transactions/1`);
    expect(putReq.request.method).toBe('PUT');
    putReq.flush(updated);

    await Promise.resolve();
    await Promise.resolve();

    const getReq = httpMock.expectOne(r => r.url === `${API}/transactions`);
    getReq.flush(refreshed);

    const result = await promise;
    expect(result).toEqual(updated);
    expect(service.transactions()).toEqual(refreshed.items);
  });

  it('5: delete() DELETEs id and refreshes list', async () => {
    const refreshed = makePagedResult([]);

    const promise = service.delete(7);

    const deleteReq = httpMock.expectOne(`${API}/transactions/7`);
    expect(deleteReq.request.method).toBe('DELETE');
    deleteReq.flush(null);

    await Promise.resolve();
    await Promise.resolve();

    const getReq = httpMock.expectOne(r => r.url === `${API}/transactions`);
    getReq.flush(refreshed);

    await promise;
    expect(service.transactions()).toEqual([]);
  });

  it('6: loading signal toggles around requests', async () => {
    expect(service.loading()).toBe(false);

    const promise = service.getPaged({ page: 1, pageSize: 10 });
    expect(service.loading()).toBe(true);

    const req = httpMock.expectOne(r => r.url === `${API}/transactions`);
    req.flush(makePagedResult());

    await promise;
    expect(service.loading()).toBe(false);
  });
});
