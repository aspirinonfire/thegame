"use client"
import { useEffect, useState } from "react";
import { Game } from "../common/gameCore/gameModels";
import { GetPastGames } from "../common/gameCore/gameRepository";
import GameMap from "../common/gamemap";
import { useRouter } from "next/navigation";

export default function History() {
  const [pastGames, setPastGames] = useState<Game[]>([]);
  const [isFetchingPastGames, setIsFetchingPastGames] = useState(true);

  const router = useRouter();
  router.refresh();

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
          const plateKey = `${plate.country}-${plate.stateOrProvince}`;
          lkp[plateKey] = (lkp[plateKey] || 0) + 1;
        });

      return lkp;
    }, {} as {[key: string]: number});

  return (
    <div className="flex flex-col gap-5">
      <h1 className="text-3xl">Total Games Played: {pastGames.length}</h1>
      <GameMap argType="historicData"
        totalNumberOfGames={ isFetchingPastGames ? 0 : pastGames.length}
        spotsByStateLookup={ isFetchingPastGames ? {} : numberOfSpotsByPlateLkp} />
    </div>
  )
}