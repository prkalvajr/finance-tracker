export type TransactionCategory = 'income' | 'expense';

// Mirrors TransactionDto on the backend.
// Date/CreatedAt/UpdatedAt are ISO 8601 strings (server returns DateOnly / DateTime).
// Deleted is server-side only and intentionally excluded.
export interface TransactionDto {
  transactionId: number;
  userId: number;
  title: string;
  amount: number;
  category: TransactionCategory;
  date: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateTransactionRequest {
  title: string;
  amount: number;
  category: TransactionCategory;
  date?: string | null;
}

export interface UpdateTransactionRequest {
  transactionId: number;
  title: string;
  amount: number;
  category: TransactionCategory;
  date?: string | null;
}

export interface TransactionQueryParams {
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortOrder?: 'asc' | 'desc';
  category?: TransactionCategory | null;
  dateFrom?: string | null;
  dateTo?: string | null;
}

export interface TransactionSummaryDto {
  netBalance: number;
  totalIncome: number;
  totalExpense: number;
}
