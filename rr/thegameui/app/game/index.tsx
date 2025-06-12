import { Button } from "flowbite-react";
import { useShallow } from "zustand/shallow";
import { useAppStore } from "~/useAppStore";

const GamePage = () => {
  const [activeGame, finishCurrentGame, startNewGame] = useAppStore(useShallow(state => 
    [state.activeGame, state.finishCurrentGame, state.startNewGame]
  ));

  return <>
    <h1>Game Page</h1>
    <pre>
      {JSON.stringify(activeGame ?? "no game", null, 2)}
    </pre>
    <Button disabled={!activeGame} onClick={() => finishCurrentGame()}>
      Are we there yet?
    </Button>
  </>
}

export default GamePage;