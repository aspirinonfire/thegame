import GameMap from "~/common-components/gamemap";
import { useAppState } from "~/appState/useAppState";
import { useEffect, useState } from "react";
import { useShallow } from "zustand/shallow";
import LoadingWidget from "~/common-components/loading";
import ResolveWithMinimumDelay from "~/common-components/debouncedResolver";

const HistoryPage = () => {
  const [retrieveGameHistory, gameHistory, isAuthenticated] = useAppState(useShallow(state =>
    [
      state.retrieveGameHistory,
      state.gameHistory,
      state.activeUser.isAuthenticated
    ]));
  
  const [isRetrievingHistory, setIsRetrievingHistory] = useState(true);

  useEffect(() => {
    setIsRetrievingHistory(true);

    ResolveWithMinimumDelay(retrieveGameHistory())
      .finally(() => {
        setIsRetrievingHistory(false);
      });
  }, [isAuthenticated]);

  const renderHistory = () => {
    return <>
      <h1 className="text-xl sm:text-2xl" data-testid="total-games-played">Total Games Played: {gameHistory.numberOfGames}</h1>
      <GameMap argType="historicData"
        totalNumberOfGames={ gameHistory.numberOfGames }
        spotsByStateLookup={ gameHistory.spotStats } />
    </>
  }

  return (
    <div className="flex flex-col gap-5">
      { isRetrievingHistory ? <LoadingWidget/> : renderHistory() }
    </div>
  )
}

export default HistoryPage;