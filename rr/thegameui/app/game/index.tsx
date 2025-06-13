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
    var testSpots = ["US-CA", "US-WA", "CA-AB"]
      .map<LicensePlateSpot>(key => ({
        key: key,
        dateSpotted: new Date(),
        spottedBy: user?.name ?? "n/a"
      }));

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