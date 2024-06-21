"use client"
import { useContext, useState } from 'react';
import PlatePicker from './platepicker';
import GameMap from '../common/gamemap';
import { CurrentGameContext, CurrentUserAccountContext } from '../common/gameCore/gameContext';
import { CreateNewGame, FinishActiveGame, UpdateCurrentGameWithNewSpots } from '../common/gameCore/gameRepository';
import { LicensePlateSpot } from '../common/gameCore/gameModels';

export default function Game() {
  const { currentGame, setNewCurrentGame } = useContext(CurrentGameContext);
  const userAccount = useContext(CurrentUserAccountContext);

  const [currentPlateSpots, setCurrentPlateSpots] = useState(currentGame?.licensePlates ?? {});
  const [showPicker, setShowPicker] = useState(false);

  async function tryStartNewGame() {
    const newGameResult = await CreateNewGame(new Date().toISOString(),
      userAccount?.name ?? "N/A");

    if (typeof newGameResult === 'string') {
      console.error("Bad new game:", newGameResult);
    } else {
      console.info("game started...");
      setNewCurrentGame(newGameResult);
      setCurrentPlateSpots(newGameResult.licensePlates);
    }
  }

  async function tryEndGame() {
    const finishResult = await FinishActiveGame();

    if (typeof finishResult === 'string') {
      console.error("Could not finish the game:", finishResult);
    } else {
      console.info("game started...");
      setNewCurrentGame(null);
    }
  }

  async function tryUpdateGame(licensePlatesLkp: { [key: string]: LicensePlateSpot }) {
    var updatedGameResult = await UpdateCurrentGameWithNewSpots(licensePlatesLkp);
    if (typeof updatedGameResult === 'string') {
      console.error("Failed to save game:", updatedGameResult)
    } else {
      setCurrentPlateSpots(updatedGameResult.licensePlates);
    }
  }

  function renderStartNewGameContents() {
    return (<div className="flex flex-col gap-5">
      <h1 className="text-3xl">Get ready for a roadtip!</h1>
      <button type="button" className="text-white bg-amber-900 hover:bg-amber-950 focus:outline-none focus:ring-4 focus:ring-gray-700 font-medium rounded-lg text-sm px-5 py-2.5 me-2 m-4"
        onClick={tryStartNewGame}>
        Let&apos;s Go!
      </button>
    </div>);
  }

  function renderCurrentGameContents() {
    return (
      <>
        <div className={`flex flex-col gap-5 transition-all ${showPicker ? "blur-sm": ""}`}>
          <div className="flex flex-row items-center justify-between sm:justify-start gap-5">
            <h1 className="text-xl sm:text-2xl">Score: {currentGame?.score.totalScore} </h1>
            <div className="flex flex-row justify-center gap-3 animate-pulse text-sm text-amber-500" style={{ fontSize: ".7rem"}}>
              { (currentGame?.score.milestones ?? []).map(ms =>(<div key={ms}>{ms}</div>)) }
            </div>
          </div>
          
          <div className="flex py-5">
            <GameMap argType="activeGame" plateSpots={currentPlateSpots} onMapClick={() => setShowPicker(true)} />
          </div>
        </div>

        <div className={`flex justify-end items-end fixed bottom-0 right-5 ${showPicker ? 'hidden' : ''}`}>
          <button type="button" className="text-white bg-amber-950 hover:bg-gray-900 focus:outline-none focus:ring-4 focus:ring-gray-300 font-medium rounded-lg text-sm px-5 py-2.5 me-2 m-4 dark:bg-gray-800 dark:hover:bg-gray-700"
            onClick={tryEndGame}>
            We have arrived!
          </button>
        </div>

        <PlatePicker
          isShowPicker={showPicker}
          setShowPicker={(isShown: boolean) => setShowPicker(isShown)}
          plateData={currentPlateSpots}
          saveNewPlateData={tryUpdateGame} />
      </>);
  }

  return (currentGame == null ? renderStartNewGameContents() : renderCurrentGameContents());
}