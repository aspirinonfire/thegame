"use client"
import { useEffect, useState } from "react";
import { Game } from "../common/gameCore/gameModels";
import { GetPastGames } from "../common/gameCore/gameRepository";
import GameMap from "../common/gamemap";

export default function History() {
  const [pastGames, setPastGames] = useState<Game[]>([]);
  const [isFetchingPastGames, setIsFetchingPastGames] = useState(true);

  useEffect(() => {
    async function FetchPastGames() {
      const pastGames = await GetPastGames();

      if (typeof pastGames === "string") {
        console.error(pastGames);
        return;
      }

      setIsFetchingPastGames(false);
      setPastGames(pastGames);
    }
    if (isFetchingPastGames){
      FetchPastGames();
    }
  });

  const numberOfSpotsByPlateLkp = pastGames
    .reduce((lkp, game) => {
      (game.spottedPlates ?? [])
        .filter(plate => !!plate.spottedOn)
        .forEach(plate => {
          const plateKey = `${plate.country}-${plate.stateOrProvince}`;
          lkp[plateKey] = (lkp[plateKey] || 0) + 1;
        });

      return lkp;
    }, {} as {[key: string]: number});

  return (
    <div className="flex flex-col gap-5">
      <h1 className="text-xl sm:text-2xl">Total Games Played: {pastGames.length}</h1>
      <GameMap argType="historicData"
        totalNumberOfGames={ isFetchingPastGames ? 0 : pastGames.length}
        spotsByStateLookup={ isFetchingPastGames ? {} : numberOfSpotsByPlateLkp} />
    </div>
  )
}