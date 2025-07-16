import { useEffect } from "react";
import { useNavigate } from "react-router";
import { useAppState } from "./appState/useAppState";

const HomePage = () => {
  let navigate = useNavigate();
  let currentGame = useAppState(state => state.activeGame);

  useEffect(() => {
    if (currentGame) {
      navigate("/game");
    } else {
      navigate("/history");
    }
  }, [currentGame]);
}

export default HomePage;