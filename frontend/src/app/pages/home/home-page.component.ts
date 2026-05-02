import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  TemplateRef,
  ViewChild,
  afterNextRender,
  inject,
  signal
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog } from '@angular/material/dialog';

import { GridComponent } from '../../shared/components/grid/grid.component';
import { GridColumn, GridFetcher, GridQuery } from '../../shared/components/grid/grid.types';
import { TransactionsService } from '../../core/services/transactions.service';
import { ConfirmationService } from '../../shared/services/confirmation.service';
import { NotificationService } from '../../core/services/notification.service';
import { TransactionCategory, TransactionDto } from '../../models/transaction.models';
import { formatCurrency, formatIsoDate } from '../../core/utils/format';
import { TransactionModalComponent } from './transaction-modal/transaction-modal.component';

@Component({
  selector: 'app-home-page',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    GridComponent,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatSelectModule,
    MatInputModule,
    MatIconModule
  ],
  templateUrl: './home-page.component.html',
  styleUrl: './home-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class HomePageComponent implements OnInit {
  private readonly txService = inject(TransactionsService);
  private readonly confirmation = inject(ConfirmationService);
  private readonly notification = inject(NotificationService);
  private readonly fb = inject(FormBuilder);
  private readonly dialog = inject(MatDialog);

  readonly summary = this.txService.summary;
  readonly formatCurrency = formatCurrency;

  readonly filterForm = this.fb.nonNullable.group({
    category: ['' as TransactionCategory | ''],
    dateFrom: [''],
    dateTo: ['']
  });

  readonly columns = signal<GridColumn<TransactionDto>[]>([
    { key: 'title',    label: 'Title',    sortable: true },
    { key: 'amount',   label: 'Amount',   sortable: true, cell: (r) => formatCurrency(r.amount) },
    { key: 'category', label: 'Category', sortable: true, cell: (r) => r.category === 'income' ? 'Income' : 'Expense' },
    { key: 'date',     label: 'Date',     sortable: true, cell: (r) => formatIsoDate(r.date) },
    { key: 'actions',  label: '',         sortable: false }
  ]);

  @ViewChild('actionsTpl') private actionsTpl!: TemplateRef<{ $implicit: TransactionDto }>;
  @ViewChild(GridComponent) grid!: GridComponent<TransactionDto>;

  readonly fetch: GridFetcher<TransactionDto> = async (query: GridQuery) => {
    const result = await this.txService.getPaged({
      page: query.page,
      pageSize: query.pageSize,
      sortBy: query.sortBy,
      sortOrder: query.sortOrder,
      category: (query.filters['category'] as TransactionCategory) || null,
      dateFrom: (query.filters['dateFrom'] as string) || null,
      dateTo: (query.filters['dateTo'] as string) || null
    });
    await this.txService.getSummary();
    return { items: result.items, totalCount: result.totalCount };
  };

  constructor() {
    afterNextRender(() => {
      this.columns.update(cols =>
        cols.map(c => c.key === 'actions' ? { ...c, template: this.actionsTpl } : c)
      );
    });
  }

  ngOnInit(): void {
    this.txService.getSummary();
  }

  applyFilters(): void {
    const { category, dateFrom, dateTo } = this.filterForm.getRawValue();
    this.grid.applyFilters({
      category: category || null,
      dateFrom: dateFrom || null,
      dateTo: dateTo || null
    });
  }

  clearFilters(): void {
    this.filterForm.reset();
    this.grid.applyFilters({});
  }

  async deleteTransaction(row: TransactionDto): Promise<void> {
    const confirmed = await this.confirmation.confirm({
      title: 'Delete transaction',
      message: `Delete "${row.title}"? This cannot be undone.`,
      confirmText: 'Delete',
      variant: 'danger'
    });
    if (!confirmed) return;

    await this.txService.delete(row.transactionId);
    this.grid.refresh();
    this.notification.success('Transaction deleted.');
  }

  openCreateModal(): void {
    this.openTransactionModal();
  }

  openEditModal(row: TransactionDto): void {
    this.openTransactionModal(row);
  }

  private openTransactionModal(transaction?: TransactionDto): void {
    const ref = this.dialog.open<TransactionModalComponent, TransactionDto | null, TransactionDto>(
      TransactionModalComponent,
      {
        data: transaction ?? null,
        width: '520px',
        maxWidth: 'calc(100vw - 32px)',
        autoFocus: 'first-tabbable'
      }
    );

    ref.afterClosed().subscribe(async saved => {
      if (!saved) return;
      this.grid.refresh();
      await this.txService.getSummary();
    });
  }
}
