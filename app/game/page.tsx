"use client"
import { useState } from 'react';
import PlatePicker from './platepicker';
import UsMap, { SpottedLicensePlates } from '../common/usmap';

export default function Game() {
  const [showPicker, setShowPicker] = useState(false);

  // todo read from state
  const plates: SpottedLicensePlates = {
    "CA": true,
    "AL": 1.0
  };

  return (
    <div className="flex flex-col gap-6 rounded-lg bg-gray-100">
      <div className="px-4">
        <div>
          <UsMap plateSpots={plates} onMapClick={() => setShowPicker(true)}/>
        </div>
        <div className="py-5">
          <p className={`text-xl text-gray-800 md:text-3xl md:leading-normal`}>
            ...game
          </p>
        </div>
      </div>

      { showPicker ? (<PlatePicker setShowPicker={(isShown: boolean) => setShowPicker(isShown)} /> ) : null }
    </div>
  )
}