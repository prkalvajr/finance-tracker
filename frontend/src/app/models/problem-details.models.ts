// RFC 7807 ProblemDetails plus the validation `errors` dictionary
// that ASP.NET Core attaches on 400 responses.
export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  errors?: Record<string, string[]>;
  [key: string]: unknown;
}
