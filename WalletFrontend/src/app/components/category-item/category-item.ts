import { CommonModule } from '@angular/common';
import { Component, EventEmitter, inject, Input, Output } from '@angular/core';
import { CategoryReadDto } from '../../models/category-read-dto';
import { CategoryUpdateDto } from '../../models/category-update-dto';
import { CategoryService } from '../../services/category-service';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-category-item',
  imports: [CommonModule, FormsModule],
  templateUrl: './category-item.html',
  styleUrl: './category-item.css'
})
export class CategoryItem {
  @Input() category!: CategoryReadDto;
  @Output() updated = new EventEmitter<void>();
  @Output() removed = new EventEmitter<void>();

  editing = false;
  editCategory: CategoryUpdateDto = { name: '' };

  constructor(private ctService: CategoryService) { }

  startEdit() {
    this.editing = true;
    this.editCategory = { name: this.category.name };
  }

  cancelEdit() {
    this.editing = false;
  }

  saveEdit() {
    this.ctService.update(this.category.id, this.editCategory).subscribe({
      next: () => {
        this.editing = false;
        this.updated.emit(); // tell parent to reload
      }
    });
  }

  deleteCategory() {
    if (!confirm('Delete this category?')) return;
    this.ctService.delete(this.category.id).subscribe({
      next: () => this.removed.emit() // tell parent to reload
    });
  }
}
