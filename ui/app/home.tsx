import { useEffect } from "react";
import { useNavigate } from "react-router";
import { useAppStore } from "./useAppStore";

const HomePage = () => {
  let navigate = useNavigate();
  let currentGame = useAppStore(state => state.activeGame);

  useEffect(() => {
    if (currentGame) {
      navigate("/game");
    } else {
      navigate("/history");
    }
  }, [currentGame]);
}

export default HomePage;