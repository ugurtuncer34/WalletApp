import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { CategoryExpenseDto } from '../models/category-expense-dto';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class CategoryReportService {
  private http = inject(HttpClient);

  getCategoryExpenses(month: string): Observable<CategoryExpenseDto[]> {
    return this.http.get<CategoryExpenseDto[]>(
      `${environment.apiUrl}/reports/category-expenses`,
      { params: { month } }
    )
    .pipe(map(r => r ?? []));
  }
}
