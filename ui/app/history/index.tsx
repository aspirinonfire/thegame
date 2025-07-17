import GameMap from "~/common-components/gamemap";
import { useAppState } from "~/appState/useAppState";
import { useEffect, useState } from "react";
import { type GameHistory } from "~/appState/GameSlice";
import { Spinner } from "flowbite-react";

const HistoryPage = () => {
  const getGameHistory = useAppState(state => state.retrieveGameHistory);
  const [isRetrievingHistory, setIsRetrievingHistory] = useState(true);
  const [gameHistory, setGameHistory] = useState<GameHistory>({
    numberOfGames: 0,
    spotStats: {}
  });

  useEffect(() => {
    getGameHistory()
      .then(history => {
        if (!!history) {
          setGameHistory(history);
        }
      })
      .finally(() => {
        setIsRetrievingHistory(false);
      });
  }, []);

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
      { isRetrievingHistory ? <Spinner size="xl" /> : renderHistory() }
    </div>
  )
}

export default HistoryPage;