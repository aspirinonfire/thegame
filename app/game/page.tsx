"use client"
import { useState } from 'react';
import PlatePicker from './platepicker';
import UsMap from '../common/usmap';
import { mockGameData } from '../common/data';
import { LicensePlate as LicensePlateSpot } from '../common/gameCore/gameModels';
import CalculateScore from '../common/gameCore/GameScoreCalculator';

const emptySpots: { [key: string]: LicensePlateSpot } = mockGameData
  .map(territory => {
    return {
      stateOrProvince: territory.shortName,
      country: territory.country,
      fullName: territory.longName,
      plateKey: `${territory.country}_${territory.shortName}`.toLowerCase(),
      dateSpotted: null,
      spottedBy: null,
      plateImageUrl: `./plates/${territory.country}-${territory.shortName}.jpg`.toLowerCase()
    } as LicensePlateSpot
  })
  .reduce((allSpots, plate) => {
    // TODO add coutnry
    allSpots[plate.plateKey] = plate;
    return allSpots;
  }, {} as { [key: string]: LicensePlateSpot });


export default function Game() {
  const [showPicker, setShowPicker] = useState(false);
  const [spottedPlates, setSpottedPlates] = useState(emptySpots);

  const plateData = Object.keys(spottedPlates)
    .map(key => spottedPlates[key]);

  const score = CalculateScore(plateData);

  function saveNewPlateData(upldatedPlates: LicensePlateSpot[]) {
    var updatedSpottedPlates = upldatedPlates
      .reduce((platesLkp, plate) => {
        platesLkp[plate.plateKey] = plate;
        return platesLkp;
      }, {} as { [key: string]: LicensePlateSpot });

    setSpottedPlates(updatedSpottedPlates);
  }

  return (
    <>
      <div>
        <h1 className="text-3xl text-black">::Game:: Score: {score.totalScore} </h1>
      </div>
      <div className="py-5">
        <UsMap plateSpots={spottedPlates} onMapClick={() => setShowPicker(true)} />
      </div>
      <div className="py-5">
        <p className={`text-xl text-gray-800 md:text-3xl md:leading-normal`}>
          ...game
        </p>
      </div>

      {showPicker ? (
        <PlatePicker
          setShowPicker={(isShown: boolean) => setShowPicker(isShown)}
          plateData={plateData}
          saveNewPlateData={saveNewPlateData} />
      ) : null}
    </>);
}