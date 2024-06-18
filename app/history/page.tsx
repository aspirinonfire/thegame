"use client"
import { useEffect, useState } from "react";
import { Game } from "../common/gameCore/gameModels";
import { GetPastGames } from "../common/gameCore/gameRepository";
import UsMap from "../common/usmap";

export default function History() {
  const [pastGames, setPastGames] = useState<Game[]>([]);
  const [isFetchingPastGames, setIsFetchingPastGames] = useState(true);

  useEffect(() => {
    async function FetchPastGames() {
      const pastGames = await GetPastGames();
      setIsFetchingPastGames(false);
      setPastGames(pastGames);
    }
    if (isFetchingPastGames){
      FetchPastGames();
    }
  });

  const numberOfSpotsByPlateLkp = pastGames
    .reduce((lkp, game) => {
      Object.keys(game.licensePlates)
        .map(key => game.licensePlates[key])
        .filter(plate => !!plate.dateSpotted)
        .forEach(plate => {
          lkp[plate.plateKey] = (lkp[plate.plateKey] || 0) + 1;
        });

      return lkp;
    }, {} as {[key: string]: number});

  return (
    <>
      <p className={`text-xl text-gray-800 md:text-3xl md:leading-normal`}>
        {isFetchingPastGames ? "Fetching Past Games" : `Total Games Played: ${pastGames.length}`}
      </p>
      {!isFetchingPastGames ? (<UsMap argType="historicData" totalNumberOfGames={pastGames.length} spotsByStateLookup={numberOfSpotsByPlateLkp} />) : null}
    </>
    
  )
}