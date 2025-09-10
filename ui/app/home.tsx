import { useEffect } from "react";
import { useNavigate } from "react-router";
import { useAppState } from "./appState/useAppState";
import { AppPaths } from "./routes";

const HomePage = () => {
  const navigate = useNavigate();
  const currentGame = useAppState(state => state.activeGame);

  useEffect(() => {
    if (currentGame) {
      navigate(AppPaths.game);
    } else {
      navigate(AppPaths.history);
    }
  }, [currentGame]);
}

export default HomePage;