import GameMap from "~/common-components/gamemap";
import { useAppStore } from "~/useAppStore";

const HistoryPage = () => {
  const pastGames = useAppStore(state => state.pastGames);

  const numberOfSpotsByPlateLkp = pastGames
    .reduce((lkp, game) => {
      game.licensePlates
        .filter(plate => !!plate.dateSpotted)
        .forEach(plate => {
          lkp[plate.key] = (lkp[plate.key] || 0) + 1;
        });

      return lkp;
    }, {} as {[key: string]: number});

  return (
    <div className="flex flex-col gap-5">
      <h1 className="text-xl sm:text-2xl">Total Games Played: {pastGames.length}</h1>
      <GameMap argType="historicData"
        totalNumberOfGames={ pastGames.length}
        spotsByStateLookup={ numberOfSpotsByPlateLkp } />
    </div>
  )
}

export default HistoryPage;