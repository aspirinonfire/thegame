import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { HomeComponent } from './home/home.component';
import { AppRoutes } from '../core/constants';
import { HistoryComponent } from './history/history.component';
import { GameRedirectGuard } from './game-redirect.guard';

const routes: Routes = [{
  path: '',
  component: HomeComponent,
  canActivate: [GameRedirectGuard]
}, {
  path: AppRoutes.history,
  component: HistoryComponent
}];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class HomeRoutingModule { }
