import { TestBed } from '@angular/core/testing';

import { CategoryReportService } from './category-report-service';

describe('CategoryReportService', () => {
  let service: CategoryReportService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(CategoryReportService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
