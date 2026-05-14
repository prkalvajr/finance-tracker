import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { describe, expect, it, vi } from 'vitest';

import { GridComponent } from './grid.component';
import { GridColumn, GridQuery } from './grid.types';

interface Row { id: number; name: string; amount: number }

function makeRows(n = 3): Row[] {
  return Array.from({ length: n }, (_, i) => ({ id: i + 1, name: `Item ${i + 1}`, amount: (i + 1) * 10 }));
}

function makeLoader() {
  const fn = vi.fn().mockResolvedValue(undefined);
  return fn as typeof fn & { mock: typeof fn['mock'] };
}

const defaultColumns: GridColumn<Row>[] = [
  { key: 'name', label: 'Name', sortable: true },
  { key: 'amount', label: 'Amount', sortable: true }
];

async function createComponent(
  columns = defaultColumns,
  load = makeLoader(),
  pageSize = 10,
  rows: Row[] = makeRows(),
  totalCount: number = rows.length
) {
  await TestBed.configureTestingModule({
    imports: [GridComponent, NoopAnimationsModule]
  }).compileComponents();

  const fixture: ComponentFixture<GridComponent<Row>> = TestBed.createComponent(GridComponent<Row>);
  fixture.componentRef.setInput('columns', columns);
  fixture.componentRef.setInput('load', load);
  fixture.componentRef.setInput('rows', rows);
  fixture.componentRef.setInput('totalCount', totalCount);
  fixture.componentRef.setInput('pageSize', pageSize);
  fixture.detectChanges();
  // Wait for the initial async load callback
  await fixture.whenStable();
  fixture.detectChanges();
  return { fixture, component: fixture.componentInstance, load };
}

describe('GridComponent', () => {
  it('1: Initial render calls load with default query', async () => {
    const load = makeLoader();
    await createComponent(defaultColumns, load);

    expect(load).toHaveBeenCalledTimes(1);
    const query: GridQuery = load.mock.calls[0][0];
    expect(query.page).toBe(1);
    expect(query.pageSize).toBe(10);
    expect(query.sortBy).toBeUndefined();
    expect(query.sortOrder).toBeUndefined();
    expect(query.filters).toEqual({});
  });

  it('2: Page change calls load with new page', async () => {
    const load = makeLoader();
    const { component } = await createComponent(defaultColumns, load, 10, makeRows(10), 30);

    load.mockClear();
    component.onPageChange({ pageIndex: 1, pageSize: 10, length: 30 });
    await new Promise(r => setTimeout(r, 0));

    expect(load).toHaveBeenCalledTimes(1);
    expect(load.mock.calls[0][0].page).toBe(2);
    expect(load.mock.calls[0][0].pageSize).toBe(10);
  });

  it('3: Page size change calls load and resets to page 1', async () => {
    const load = makeLoader();
    const { component } = await createComponent(defaultColumns, load, 10, makeRows(10), 30);

    // Simulate navigating to page 2 first
    load.mockClear();
    component.onPageChange({ pageIndex: 1, pageSize: 10, length: 30 });
    await new Promise(r => setTimeout(r, 0));

    load.mockClear();
    // Now change page size
    component.onPageChange({ pageIndex: 0, pageSize: 25, length: 30 });
    await new Promise(r => setTimeout(r, 0));

    expect(load).toHaveBeenCalledTimes(1);
    const query: GridQuery = load.mock.calls[0][0];
    expect(query.page).toBe(1);
    expect(query.pageSize).toBe(25);
  });

  it('4: Sort change calls load with sortBy and sortOrder', async () => {
    const load = makeLoader();
    const { component } = await createComponent(defaultColumns, load);

    load.mockClear();
    component.onSortChange({ active: 'amount', direction: 'desc' });
    await new Promise(r => setTimeout(r, 0));

    expect(load).toHaveBeenCalledTimes(1);
    const query: GridQuery = load.mock.calls[0][0];
    expect(query.sortBy).toBe('amount');
    expect(query.sortOrder).toBe('desc');
  });

  it('5: Filter change calls load with filters and resets to page 1', async () => {
    const load = makeLoader();
    const { component } = await createComponent(defaultColumns, load, 10, makeRows(10), 30);

    // Go to page 2
    component.onPageChange({ pageIndex: 1, pageSize: 10, length: 30 });
    await new Promise(r => setTimeout(r, 0));

    load.mockClear();
    component.applyFilters({ category: 'income' });
    await new Promise(r => setTimeout(r, 0));

    expect(load).toHaveBeenCalledTimes(1);
    const query: GridQuery = load.mock.calls[0][0];
    expect(query.page).toBe(1);
    expect(query.filters).toEqual({ category: 'income' });
  });

  it('6: refresh() re-invokes load with the current query', async () => {
    const load = makeLoader();
    const { component } = await createComponent(defaultColumns, load);

    component.onSortChange({ active: 'name', direction: 'asc' });
    await new Promise(r => setTimeout(r, 0));

    load.mockClear();
    component.refresh();
    await new Promise(r => setTimeout(r, 0));

    expect(load).toHaveBeenCalledTimes(1);
    const query: GridQuery = load.mock.calls[0][0];
    expect(query.sortBy).toBe('name');
    expect(query.sortOrder).toBe('asc');
  });

  it('7: Custom cell function renders returned string', async () => {
    const columns: GridColumn<Row>[] = [
      { key: 'amount', label: 'Amount', sortable: false, cell: (row) => `$${row.amount.toFixed(2)}` }
    ];
    const { component } = await createComponent(columns, makeLoader(), 10, [{ id: 1, name: 'X', amount: 5 }]);

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
