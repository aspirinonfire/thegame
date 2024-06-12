"use client"
import { useState } from 'react';
import PlatePicker from './platepicker';
import UsMap from '../common/usmap';
import { mockGameData } from '../common/data';
import { LicensePlate } from '../common/gameCore/gameModels';
import CalculateScore from '../common/gameCore/GameScoreCalculator';

const emptySpots: { [key: string]: LicensePlate } = mockGameData
  .map(territory => {
    return {
      stateOrProvince: territory.shortName,
      country: territory.country,
      dateSpotted: null,
      spottedBy: null
    } as LicensePlate
  })
  .reduce((allSpots, plate) => {
    // TODO add coutnry
    allSpots[plate.stateOrProvince] = plate; 
    return allSpots;
  }, {} as { [key: string]: LicensePlate });


export default function Game() {
  const [showPicker, setShowPicker] = useState(false);
  const [spottedPlates, setSpottedPlates] = useState(emptySpots);

  const plateData = Object.keys(spottedPlates)
    .map(key => spottedPlates[key]);

  const score = CalculateScore(plateData);

  function saveNewPlateData(upldatedPlates: LicensePlate[]) {
    var updatedSpottedPlates = upldatedPlates
      .reduce((platesLkp, plate) => {
        platesLkp[plate.stateOrProvince] = plate;
        return platesLkp;
      }, {} as { [key: string]: LicensePlate });

    setSpottedPlates(updatedSpottedPlates);
  }

  return (
    <div className="flex flex-col gap-6 rounded-lg bg-gray-100">
      <div className="px-4 py-4">
        <div>
          <h1 className="text-3xl text-black">::Game:: Score: { score.totalScore } </h1>
        </div>
        <div>
          <UsMap plateSpots={spottedPlates} onMapClick={() => setShowPicker(true)}/>
        </div>
        <div className="py-5">
          <p className={`text-xl text-gray-800 md:text-3xl md:leading-normal`}>
            ...game
          </p>
        </div>
      </div>

      { showPicker ? (
        <PlatePicker 
          setShowPicker={ (isShown: boolean) => setShowPicker(isShown) }
          plateData={ plateData }
          saveNewPlateData = { saveNewPlateData } />
        ) : null }
    </div>
  )
}