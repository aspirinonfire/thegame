import { AppInitDataService } from './services/app-init-data.service';
import { mockAccount, mockGameData } from './mockData';
import { of, forkJoin } from 'rxjs';
import { delay, map } from 'rxjs/operators';

export function appInitFactory(appInitService: AppInitDataService) {
    return (): Promise<void> => {
        // TODO run real data init call
        const accountObs = of(mockAccount).pipe(delay(500));
        const gameDataObs = of(mockGameData).pipe(delay(500));

        return forkJoin(accountObs, gameDataObs)
            .pipe(map(([account, gameData]) => {
                appInitService.loadInitData(account, gameData);
                return;
            }))
            .toPromise();
    }
}