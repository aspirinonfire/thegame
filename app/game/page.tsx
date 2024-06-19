"use client"
import { useContext, useState } from 'react';
import PlatePicker from './platepicker';
import UsMap from '../common/usmap';
import { CurrentGameContext, CurrentUserAccountContext } from '../common/gameCore/gameContext';
import { CreateNewGame, FinishActiveGame, UpdateCurrentGameWithNewSpots } from '../common/gameCore/gameRepository';
import { LicensePlateSpot } from '../common/gameCore/gameModels';

export default function Game() {
  const { currentGame, setNewCurrentGame } = useContext(CurrentGameContext);
  const userAccount = useContext(CurrentUserAccountContext);

  const [currentPlateSpots, setCurrentPlateSpots] = useState(currentGame?.licensePlates ?? {});
  const [showPicker, setShowPicker] = useState(false);

  console.log("rendering game page");

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

  const renderStartNewGameContents = () =>
    <div className="text-black">
      <h1 className="text-3xl">Get ready for a roadtip!</h1>
      <button type="button" className="text-white bg-gray-800 hover:bg-gray-900 focus:outline-none focus:ring-4 focus:ring-gray-300 font-medium rounded-lg text-sm px-5 py-2.5 me-2 m-4 dark:bg-gray-800 dark:hover:bg-gray-700 dark:focus:ring-gray-700 dark:border-gray-700"
        onClick={tryStartNewGame}>
        Let&apos;s Go!
      </button>
    </div>

  async function tryUpdateGame(licensePlatesLkp: { [key: string]: LicensePlateSpot }) {
    var updatedGameResult = await UpdateCurrentGameWithNewSpots(licensePlatesLkp);
    if (typeof updatedGameResult === 'string') {
      console.error("Failed to save game:", updatedGameResult)
    } else {
      setCurrentPlateSpots(updatedGameResult.licensePlates);
    }
  }

  function renderCurrentGameContents() {
    return (
      <>
        <div>
          <h1 className="text-3xl text-black">::Game:: Score: {currentGame?.score.totalScore} </h1>
        </div>
        <div className="py-5">
          <UsMap argType="activeGame" plateSpots={currentPlateSpots} onMapClick={() => setShowPicker(true)} />
        </div>
        <div className="py-5">
          <p className="text-xl text-gray-800 md:text-3xl md:leading-normal">
            ...game
          </p>
        </div>

        <div className="flex flex-row justify-end">
          <button type="button" className="text-white bg-gray-800 hover:bg-gray-900 focus:outline-none focus:ring-4 focus:ring-gray-300 font-medium rounded-lg text-sm px-5 py-2.5 me-2 m-4 dark:bg-gray-800 dark:hover:bg-gray-700"
            onClick={tryEndGame}>
            We have arrived!
          </button>
        </div>

        {showPicker ? (
          <PlatePicker
            setShowPicker={(isShown: boolean) => setShowPicker(isShown)}
            plateData={currentPlateSpots}
            saveNewPlateData={tryUpdateGame} />
        ) : null}
      </>);
  }

  return (currentGame == null ? renderStartNewGameContents() : renderCurrentGameContents());
}