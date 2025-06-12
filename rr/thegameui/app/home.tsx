import { useEffect } from "react";
import { useNavigate } from "react-router";
import { useAppStore } from "./useAppStore";

const HomePage = () => {
  let navigate = useNavigate();
  let currentGame = useAppStore(state => state.currentGame);

  useEffect(() => {
    if (!currentGame) {
      navigate("/history");
    }
  }, [currentGame]);
}

export default HomePage;