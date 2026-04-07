import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { OperatingModeService } from '../core/services/operating-mode.service';

@Component({
  standalone: true,
  selector: 'app-pos-entry-redirect',
  template: ''
})
export class PosEntryRedirectComponent {
  constructor(router: Router, operatingMode: OperatingModeService) {
    void router.navigateByUrl(operatingMode.getPreferredPosRoute());
  }
}
