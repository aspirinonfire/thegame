import { Button, Modal, ModalBody, ModalHeader } from "flowbite-react";
import { useShallow } from "zustand/shallow";
import GameMap from "~/common-components/gamemap";
import { useAppStore } from "~/useAppStore";
import PlatePicker from "./platepicker";
import { ChevronRightIcon } from "@heroicons/react/24/outline";
import { useState } from "react";
import { useNavigate } from "react-router";

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

  const navigate = useNavigate();

  const [showPicker, setShowPicker] = useState(false);
  const [showEndGame, setShowEndGame] = useState(false);

  const dateStartedFriendly = activeGame?.dateCreated.toLocaleString();

  async function tryEndGame() {
    setShowEndGame(false);
    const finishResult = await finishCurrentGame();

    if (typeof finishResult === 'string') {
      console.error("Could not finish the game:", finishResult);
    } else {
      navigate("/history");
    }
  }

  function renderStartNewGameContents() {
    return (<div className="flex flex-col justify-center items-center grow gap-5 text-center">
      <h1 className="text-3xl">Get ready for a roadtip!</h1>
      <Button className="bg-amber-700 animate-pulse" size="xl" 
        onClick={() => startNewGame(new Date().toISOString())}>
          <ChevronRightIcon className="mr-2 h-5 w-5 pt-1" />
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
          
          <GameMap argType="activeGame" plateSpots={activeGame?.licensePlates ?? []} onMapClick={() => setShowPicker(true)} />
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
            plateData={activeGame?.licensePlates ?? []}
            saveNewPlateData={spotNewPlates} />) : null }

        <Modal show={showEndGame} size="sm" onClose={() => setShowEndGame(false)} popup>
          <ModalHeader />
          <ModalBody>
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
          </ModalBody>
        </Modal>
      </>);
  }

  return (activeGame == null ? renderStartNewGameContents() : renderCurrentGameContents());
}

export default GamePage;