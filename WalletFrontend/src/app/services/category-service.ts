import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment';
import { HttpClient } from '@angular/common/http';
import { CategoryReadDto } from '../models/category-read-dto';
import { CategoryCreateDto } from '../models/category-create-dto';
import { CategoryUpdateDto } from '../models/category-update-dto';

@Injectable({
  providedIn: 'root'
})
export class CategoryService {
  private api = `${environment.apiUrl}/categories`;
  constructor(private http: HttpClient) { }

  list() {
    return this.http.get<CategoryReadDto[]>(this.api);
  }
  create(body: CategoryCreateDto) {
    return this.http.post<CategoryReadDto>(this.api, body);
  }
  update(id: number, body: CategoryUpdateDto) {
    return this.http.put(`${this.api}/${id}`, body);
  }
  delete(id: number) {
    return this.http.delete(`${this.api}/${id}`);
  }
}
