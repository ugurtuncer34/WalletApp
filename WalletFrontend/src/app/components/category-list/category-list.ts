import { CommonModule } from '@angular/common';
import { Component, EventEmitter, inject, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CategoryItem } from '../category-item/category-item';
import { Observable } from 'rxjs';
import { CategoryCreateDto } from '../../models/category-create-dto';
import { CategoryReadDto } from '../../models/category-read-dto';
import { CategoryService } from '../../services/category-service';

@Component({
  selector: 'app-category-list',
  imports: [CommonModule, FormsModule, CategoryItem],
  templateUrl: './category-list.html',
  styleUrl: './category-list.css'
})
export class CategoryList {
  @Output() updated = new EventEmitter<void>();

  private ctService = inject(CategoryService);
  categories$: Observable<CategoryReadDto[]> = this.ctService.list();

  newCategory: CategoryCreateDto = { name: '' };

  createCategory() {
    if (!this.newCategory.name.trim()) return alert('Name required');

    this.ctService.create(this.newCategory).subscribe(() => {
      this.categories$ = this.ctService.list();
      this.newCategory = { name: '' };
      this.updated.emit();
    });
  }

  reload() {
    this.categories$ = this.ctService.list();
    this.updated.emit();
  }

  trackById = (_: number, item: CategoryReadDto) => item.id;
}
