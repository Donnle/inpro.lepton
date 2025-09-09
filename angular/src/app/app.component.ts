import { Component, OnInit } from '@angular/core';
import { InternetConnectionStatusComponent, LoaderBarComponent } from '@abp/ng.theme.shared';
import { DynamicLayoutComponent } from '@abp/ng.core';
import { BaseGrpcApiService } from './shared/services/base-grpc-api.service';

@Component({
  selector: 'app-root',
  template: `
    <abp-loader-bar />
    <abp-dynamic-layout />
    <abp-internet-status />
  `,
  imports: [LoaderBarComponent, DynamicLayoutComponent, InternetConnectionStatusComponent],
})
export class AppComponent implements OnInit {
  // eslint-disable-next-line @angular-eslint/prefer-inject
  constructor(private baseGrpcApiService: BaseGrpcApiService) {
  }

  ngOnInit() {
    this.baseGrpcApiService.test();
  }
}
