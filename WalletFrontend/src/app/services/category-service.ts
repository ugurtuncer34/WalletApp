import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment';
import { HttpClient } from '@angular/common/http';
import { CategoryReadDto } from '../models/category-read-dto';

@Injectable({
  providedIn: 'root'
})
export class CategoryService {
  private api = `${environment.apiUrl}/categories`;
  constructor(private http: HttpClient){}

  list(){
    return this.http.get<CategoryReadDto[]>(this.api);
  }
  create(body: Partial<CategoryReadDto>){
    return this.http.post(this.api, body);
  }
  delete(id: number){
    return this.http.delete(`${this.api}/${id}`);
  }
}
