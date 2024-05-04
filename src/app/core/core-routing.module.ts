import { NgModule } from '@angular/core';
import { Routes, RouterModule, PreloadAllModules } from '@angular/router';
import { AppRoutes } from './constants';

const routes: Routes = [
  {
    path: AppRoutes.home,
    loadChildren: () => import('../home/home.module').then(m => m.HomeModule),
    
  },
  {
    path: AppRoutes.game,
    loadChildren: () => import('../game/game.module').then(m => m.GameModule)
  },
  {
    path: AppRoutes.about,
    loadChildren: () => import('../about/about.module').then(m => m.AboutModule)
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
  imports: [RouterModule.forRoot(
    routes, { preloadingStrategy: PreloadAllModules, scrollPositionRestoration: 'top', relativeLinkResolution: 'legacy' })],
  exports: [RouterModule]
})
export class CoreRoutingModule { }
