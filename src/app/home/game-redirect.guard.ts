import { Injectable } from '@angular/core';
import { CanActivate, ActivatedRouteSnapshot, RouterStateSnapshot, UrlTree, Router } from '@angular/router';
import { Observable } from 'rxjs';
import { GameService } from '../core/services/game.service';
import { AppRoutes } from '../core/constants';

@Injectable({
  providedIn: 'root'
})
export class GameRedirectGuard implements CanActivate {

  constructor(private readonly gameSvc: GameService, private router: Router) {}

  canActivate(
    next: ActivatedRouteSnapshot,
    state: RouterStateSnapshot): Observable<boolean | UrlTree> | Promise<boolean | UrlTree> | boolean | UrlTree {

    const hasPastGames = !!this.gameSvc.getPastGames().length;

    if (!!this.gameSvc.getCurrentGame() || !hasPastGames) {
      return this.router.parseUrl(`/${AppRoutes.game}`);
    }
    
    return this.router.parseUrl(`/${AppRoutes.home}/${AppRoutes.history}`);
  }
}
