import { Button } from "flowbite-react";
import { useShallow } from "zustand/shallow";
import { useAppStore } from "~/useAppStore";

const HistoryPage = () => {
  const [activeGame, pastGames, startNewGame] = useAppStore(useShallow(state => 
    [state.activeGame, state.pastGames, state.startNewGame]
  ));

  return <>
    <h1>History Page</h1>
    <pre>
      {JSON.stringify(pastGames, null, 2)}
    </pre>
    <p>Active Game: {activeGame ? 'yes': 'no'}</p>
    <Button disabled={!!activeGame} onClick={() => startNewGame(new Date().toISOString())}>
      Start new Journey
    </Button>
  </>
}

export default HistoryPage;