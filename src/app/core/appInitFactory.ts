import { AppInitService } from './services/app-init.service';
import { GameService } from './services/game.service';
import { of } from 'rxjs';
import { delay, map } from 'rxjs/operators';

export function appInitFactory(appInitService: AppInitService) {
    return (): Promise<void> => {
        // TODO run real data init call
        return of("some_data")
            .pipe(delay(500))
            .pipe(map(x => {
                appInitService.loadInitData(x);
                return;
            }))
            .toPromise();
    }
}