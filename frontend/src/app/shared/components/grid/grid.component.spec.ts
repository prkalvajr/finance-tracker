import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { describe, expect, it, vi } from 'vitest';

import { GridComponent } from './grid.component';
import { GridColumn, GridQuery } from './grid.types';

interface Row { id: number; name: string; amount: number }

function makeRows(n = 3): Row[] {
  return Array.from({ length: n }, (_, i) => ({ id: i + 1, name: `Item ${i + 1}`, amount: (i + 1) * 10 }));
}

function makeFetcher(rows = makeRows(), total?: number) {
  const fn = vi.fn().mockResolvedValue({ items: rows, totalCount: total ?? rows.length });
  return fn as typeof fn & { mock: typeof fn['mock'] };
}

const defaultColumns: GridColumn<Row>[] = [
  { key: 'name', label: 'Name', sortable: true },
  { key: 'amount', label: 'Amount', sortable: true }
];

async function createComponent(
  columns = defaultColumns,
  fetch = makeFetcher(),
  pageSize = 10
) {
  await TestBed.configureTestingModule({
    imports: [GridComponent, NoopAnimationsModule]
  }).compileComponents();

  const fixture: ComponentFixture<GridComponent<Row>> = TestBed.createComponent(GridComponent<Row>);
  fixture.componentRef.setInput('columns', columns);
  fixture.componentRef.setInput('fetch', fetch);
  fixture.componentRef.setInput('pageSize', pageSize);
  fixture.detectChanges();
  // Wait for the initial async fetch
  await fixture.whenStable();
  fixture.detectChanges();
  return { fixture, component: fixture.componentInstance, fetch };
}

describe('GridComponent', () => {
  it('1: Initial render calls fetch with default query', async () => {
    const fetch = makeFetcher();
    await createComponent(defaultColumns, fetch);

    expect(fetch).toHaveBeenCalledTimes(1);
    const query: GridQuery = fetch.mock.calls[0][0];
    expect(query.page).toBe(1);
    expect(query.pageSize).toBe(10);
    expect(query.sortBy).toBeUndefined();
    expect(query.sortOrder).toBeUndefined();
    expect(query.filters).toEqual({});
  });

  it('2: Page change calls fetch with new page', async () => {
    const fetch = makeFetcher(makeRows(10), 30);
    const { component } = await createComponent(defaultColumns, fetch);

    fetch.mockClear();
    component.onPageChange({ pageIndex: 1, pageSize: 10, length: 30 });
    await new Promise(r => setTimeout(r, 0));

    expect(fetch).toHaveBeenCalledTimes(1);
    expect(fetch.mock.calls[0][0].page).toBe(2);
    expect(fetch.mock.calls[0][0].pageSize).toBe(10);
  });

  it('3: Page size change calls fetch and resets to page 1', async () => {
    const fetch = makeFetcher(makeRows(10), 30);
    const { component } = await createComponent(defaultColumns, fetch);

    // Simulate navigating to page 2 first
    fetch.mockClear();
    component.onPageChange({ pageIndex: 1, pageSize: 10, length: 30 });
    await new Promise(r => setTimeout(r, 0));

    fetch.mockClear();
    // Now change page size
    component.onPageChange({ pageIndex: 0, pageSize: 25, length: 30 });
    await new Promise(r => setTimeout(r, 0));

    expect(fetch).toHaveBeenCalledTimes(1);
    const query: GridQuery = fetch.mock.calls[0][0];
    expect(query.page).toBe(1);
    expect(query.pageSize).toBe(25);
  });

  it('4: Sort change calls fetch with sortBy and sortOrder', async () => {
    const fetch = makeFetcher();
    const { component } = await createComponent(defaultColumns, fetch);

    fetch.mockClear();
    component.onSortChange({ active: 'amount', direction: 'desc' });
    await new Promise(r => setTimeout(r, 0));

    expect(fetch).toHaveBeenCalledTimes(1);
    const query: GridQuery = fetch.mock.calls[0][0];
    expect(query.sortBy).toBe('amount');
    expect(query.sortOrder).toBe('desc');
  });

  it('5: Filter change calls fetch with filters and resets to page 1', async () => {
    const fetch = makeFetcher(makeRows(10), 30);
    const { component } = await createComponent(defaultColumns, fetch);

    // Go to page 2
    component.onPageChange({ pageIndex: 1, pageSize: 10, length: 30 });
    await new Promise(r => setTimeout(r, 0));

    fetch.mockClear();
    component.applyFilters({ category: 'income' });
    await new Promise(r => setTimeout(r, 0));

    expect(fetch).toHaveBeenCalledTimes(1);
    const query: GridQuery = fetch.mock.calls[0][0];
    expect(query.page).toBe(1);
    expect(query.filters).toEqual({ category: 'income' });
  });

  it('6: Fetcher rejection surfaces empty rows and does not crash', async () => {
    const fetch = vi.fn().mockRejectedValue(new Error('network error'));
    const { component } = await createComponent(defaultColumns, fetch as never);

    expect(component.rows()).toEqual([]);
    expect(component.errorOccurred()).toBe(true);
    expect(component.loading()).toBe(false);
  });

  it('7: Custom cell function renders returned string', async () => {
    const columns: GridColumn<Row>[] = [
      { key: 'amount', label: 'Amount', sortable: false, cell: (row) => `$${row.amount.toFixed(2)}` }
    ];
    const { component } = await createComponent(columns, makeFetcher([{ id: 1, name: 'X', amount: 5 }]));

    expect(component.getCell(columns[0], { id: 1, name: 'X', amount: 5 })).toBe('$5.00');
  });

  it('8: Non-sortable column is configured as not sortable', async () => {
    const columns: GridColumn<Row>[] = [
      { key: 'name', label: 'Name', sortable: false }
    ];
    const { component } = await createComponent(columns);

    expect(component.columns()[0].sortable).toBe(false);
  });
});
