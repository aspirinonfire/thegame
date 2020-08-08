import { Component, OnInit } from '@angular/core';
import { AppInitService } from 'src/app/core/services/app-init.service';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.scss']
})
export class HomeComponent implements OnInit {
  public homeData: any;

  constructor(private readonly appInitSvc: AppInitService) { }

  ngOnInit(): void {
    this.homeData = this.appInitSvc.initData;
  }
}
