import type { NextJsGameData } from "./nextjs-models"

export function retrieveNextJsData () {
  const oldGameData: NextJsGameData = {
    currentGame: null,
    pastGames: []
  };

  try {
    const oldCurrentGameJson = localStorage.getItem("currentGame");
    if (!!oldCurrentGameJson) {
      oldGameData.currentGame = JSON.parse(oldCurrentGameJson)
    }

    const oldPastGamesJson = localStorage.getItem("pastGames");
    if (!!oldPastGamesJson) {
      oldGameData.pastGames = JSON.parse(oldPastGamesJson);
    }

  }
  catch (ex) {
    console.error("Failed to retrieve old game data:", ex);
  }

  return oldGameData
}

export function deleteNextJsGameData() {
  localStorage.removeItem("currentGame");
  localStorage.removeItem("pastGames");
}