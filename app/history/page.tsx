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
      <div>
          <h1 className="text-3xl text-black">
            {isFetchingPastGames ? "Fetching Past Games" : `Total Games Played: ${pastGames.length}`}
          </h1>
        </div>
        <div className="py-5">
          {!isFetchingPastGames ? (<UsMap argType="historicData" totalNumberOfGames={pastGames.length} spotsByStateLookup={numberOfSpotsByPlateLkp} />) : null}
        </div>
    </>
  )
}