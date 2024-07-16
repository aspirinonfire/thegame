"use client"
import { useContext, useState } from 'react';
import PlatePicker from './platepicker';
import GameMap from '../common/gamemap';
import { CurrentGameContext, CurrentUserAccountContext } from '../common/gameCore/gameContext';
import { CreateNewGame, FinishActiveGame, UpdateCurrentGameWithNewSpots } from '../common/gameCore/gameRepository';
import { LicensePlateSpot } from '../common/gameCore/gameModels';
import { Button, Modal } from "flowbite-react";
import { useRouter } from "next/navigation";
import { HiOutlineChevronRight } from "react-icons/hi";

export default function GamePage() {
  const userAccount = useContext(CurrentUserAccountContext);
  const {activeGame, setActiveGame} = useContext(CurrentGameContext);

  const [showPicker, setShowPicker] = useState(false);
  const [showEndGame, setShowEndGame] = useState(false);

  const router = useRouter();

  const dateStartedFriendly = activeGame?.dateCreated.toLocaleString();
    
  async function tryStartNewGame() {
    const newGameResult = await CreateNewGame(new Date().toISOString(),
      userAccount.userDetails.Player.playerName ?? "N/A");

    if (typeof newGameResult === 'string') {
      console.error("Bad new game:", newGameResult);
    } else {
      setActiveGame(newGameResult);
    }
  }

  async function tryEndGame() {
    setShowEndGame(false);
    const finishResult = await FinishActiveGame();

    if (typeof finishResult === 'string') {
      console.error("Could not finish the game:", finishResult);
    } else {
      router.push("/history");
      setActiveGame(null);
    }
  }

  async function tryUpdateGame(licensePlatesLkp: { [key: string]: LicensePlateSpot }) {
    var updatedGameResult = await UpdateCurrentGameWithNewSpots(licensePlatesLkp);
    if (typeof updatedGameResult === 'string') {
      console.error("Failed to save game:", updatedGameResult)
    } else {
      setActiveGame(updatedGameResult);
    }
  }

  function renderStartNewGameContents() {
    return (<div className="flex flex-col justify-center items-center grow gap-5 text-center">
      <h1 className="text-3xl">Get ready for a roadtip!</h1>
      <Button className="bg-amber-700 animate-pulse" size="xl" 
        onClick={tryStartNewGame}>
        <HiOutlineChevronRight className="mr-2 h-5 w-5 pt-1" />
        Let&apos;s Go!
      </Button>
    </div>);
  }

  function renderCurrentGameContents() {
    return (
      <>
        <div className={`flex flex-col gap-5 transition-all ${showPicker ? "blur-sm": ""}`}>
          <div className="flex flex-row items-center justify-between sm:justify-start gap-5">
            <h1 className="text-xl sm:text-2xl">Score: {activeGame?.score.totalScore} </h1>
            <div className="flex flex-row justify-center gap-3 animate-pulse text-sm text-amber-500" style={{ fontSize: ".7rem"}}>
              { (activeGame?.score.milestones ?? []).map(ms =>(<div key={ms}>{ms}</div>)) }
            </div>
          </div>
          
          <GameMap argType="activeGame" plateSpots={activeGame?.licensePlates ?? {}} onMapClick={() => setShowPicker(true)} />
        </div>

        <div className={`flex flex-row grow justify-end items-center fixed bottom-0 right-5 ${showPicker ? 'hidden' : ''}`}>
          <small className="text-xs opacity-50 ml-5">Game started on <br /> {dateStartedFriendly}</small>
          <button type="button" className="text-white bg-amber-950 hover:bg-gray-900 focus:outline-none focus:ring-4 focus:ring-gray-300 font-medium rounded-lg text-sm px-3 sm:px-5 py-2.5 m-4"
            onClick={() => setShowEndGame(true)}>
            We have arrived!
          </button>
        </div>

        { !!activeGame ? (<PlatePicker
          isShowPicker={showPicker}
          setShowPicker={(isShown: boolean) => setShowPicker(isShown)}
          plateData={activeGame?.licensePlates ?? {}}
          saveNewPlateData={tryUpdateGame} />) : null }

        <Modal show={showEndGame} size="sm" onClose={() => setShowEndGame(false)} popup>
          <Modal.Header />
          <Modal.Body>
            <div className="text-center">
              <h3 className="mb-10 text-2xl font-normal text-gray-500">
                Are we there yet?
              </h3>
              <div className="flex flex-row justify-between grow">
                <Button color="gray" onClick={() => setShowEndGame(false)}>
                  Not yet
                </Button>
                <Button className="bg-amber-700" onClick={tryEndGame}>
                  End Game
                </Button>
              </div>
            </div>
          </Modal.Body>
        </Modal>
      </>);
  }

  return (activeGame == null ? renderStartNewGameContents() : renderCurrentGameContents());
}