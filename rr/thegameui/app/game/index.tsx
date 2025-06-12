import { Button } from "flowbite-react";
import { useShallow } from "zustand/shallow";
import { territories } from "~/game-core/gameConfiguration";
import type { LicensePlateSpot } from "~/game-core/models/LicensePlateSpot";
import { useAppStore } from "~/useAppStore";

const GamePage = () => {
  const [user, activeGame, finishCurrentGame, spotNewPlates, startNewGame] =
    useAppStore(useShallow(state => [
      state.activeUser,
      state.activeGame,
      state.finishCurrentGame,
      state.spotNewPlates,
      state.startNewGame
    ]
  ));

  const handleNewSpots = () => {
    // TODO read new spots from modal/picker
    var testSpots = territories
      .filter(ter => (ter.shortName == "CA" && ter.country == "US") || (ter.shortName == "WA" && ter.country == "US"))
      .reduce((lkp, ter) => {
        lkp[`${ter.country}-${ter.shortName}`] = {
          country: ter.country,
          stateOrProvince: ter.shortName,
          spottedBy: user?.name ?? "n/a",
          dateSpotted: new Date()
        }

        return lkp;
      }, {} as { [key: string]: LicensePlateSpot });

    spotNewPlates(testSpots);
  };

  return <>
    <h1>Game Page</h1>
    <pre>
      {JSON.stringify(activeGame ?? "no game", null, 2)}
    </pre>
    <Button size="xs" color="default" disabled={!activeGame} onClick={handleNewSpots}>
      New Spot
    </Button>
    <Button size="xs" color="dark" disabled={!activeGame} onClick={finishCurrentGame}>
      Are we there yet?
    </Button>
  </>
}

export default GamePage;