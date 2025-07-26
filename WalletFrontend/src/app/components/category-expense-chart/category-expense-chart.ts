import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { NgChartsModule } from 'ng2-charts';
import { CategoryReportService } from '../../services/category-report-service';
import { ChartConfiguration } from 'chart.js';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-category-expense-chart',
  imports: [CommonModule, NgChartsModule, FormsModule],
  templateUrl: './category-expense-chart.html',
  styleUrl: './category-expense-chart.css'
})
export class CategoryExpenseChart {
  private svc = inject(CategoryReportService);

  /** two reactive signals so the chart updates if month changes */
  month = signal<string>(new Date().toISOString().slice(0, 7)); // yyyy-MM
  chartData = signal<ChartConfiguration<'pie'>['data']>({ labels: [], datasets: [] });

  ngOnInit() { this.load(); }

  load() {
    this.svc.getCategoryExpenses(this.month()).subscribe(data => {
      // const labels = data.map(d => d.category); // for manual color opts
      // const values = data.map(d => d.total);

      this.chartData.set({
        labels: data.map(d => d.category),
        datasets: [
          {
            data: data.map(d => d.total),
            // Chart.js will auto-pick colours
            // labels,
            // datasets: [{
            //   data: values,
            //   backgroundColor: labels.map(colourFor)   // manual colours
            // }]
          }
        ]
      });
    });
  }

  /* when user picks another month */
  onMonthChange() { this.load(); }
}
