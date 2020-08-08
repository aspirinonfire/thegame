import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { AppRoutes } from './constants';

const routes: Routes = [
  {
    path: AppRoutes.home,
    loadChildren: () => import('../home/home.module').then(m => m.HomeModule),

  },
  {
    path: '',
    redirectTo: `/${AppRoutes.home}`,
    pathMatch: 'full'
  },
  {
    path: '**',
    redirectTo: `/${AppRoutes.home}`,
  }

];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class CoreRoutingModule { }
