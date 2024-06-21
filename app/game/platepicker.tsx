"use client"
import { mockAccount } from "../common/data";
import { LicensePlateSpot } from "../common/gameCore/gameModels";
import React, { useRef, useState } from 'react';
import { Button, Modal } from "flowbite-react";
import Image from 'next/image'

interface PickerControls {
  isShowPicker: boolean;
  setShowPicker: (isShown: boolean) => void;
  saveNewPlateData: (plates: { [key: string]: LicensePlateSpot }) => void;
  plateData: { [key: string]: LicensePlateSpot };
}

export default function PlatePicker({isShowPicker, setShowPicker, saveNewPlateData, plateData }: PickerControls) {
  const [formData, setFormData] = useState(plateData);
  const [searchTerm, setSearchTerm] = useState<string | null>();

  const searchInputRef = useRef<HTMLInputElement>(null);

  function handleCheckboxChange(plateKey: string, clickEvent: React.MouseEvent) {
    clickEvent.stopPropagation();

    const matchingPlate = formData[plateKey];
    if (!matchingPlate) {
      console.error(`${plateKey} was not found! eh?!`);
      return;
    }

    const updatedForm = {
      ...formData
    };

    updatedForm[plateKey] = {
      ...matchingPlate,
      dateSpotted: matchingPlate.dateSpotted === null ? new Date() : null,
      spottedBy: matchingPlate.spottedBy === null ? mockAccount.name : null
    };

    setFormData(updatedForm);
  }

  const platesToRender = Object.keys(formData)
    .map(key => formData[key])
    .filter(plate => {
      if (!searchTerm) {
        return true;
      }

      const searchValue = searchTerm.toLowerCase();

      const plateName = plate.fullName.toLowerCase()

      // full name starts with
      if (plateName.startsWith(searchValue)) {
        return true;
      }

      // contained in the second word
      if (plateName.includes(` ${searchValue}`)) {
        return true;
      }

      // short name matches
      if (plate.stateOrProvince.toLowerCase() == searchValue) {
        return true;
      }

      return false;
    });

  function renderCheckboxes() {
    return platesToRender
      .map((plate) => (
        <div key={plate.plateKey} onClick={e => handleCheckboxChange(plate.plateKey, e)} className="flex flex-row grow gap-3 text-black justify-stretch items-center">
          <h1 className="text-lg">{ !!plate.dateSpotted ? "checked" : "" }</h1>
          { plate.country == "US" ? (<Image src={ `/${plate.plateImageUrl}`} alt={plate.stateOrProvince} width={300} height={500} />) : (<h1 className="text-2xl">{plate.fullName}</h1>) }
          <div className="plate-details">...plate details...</div>
        </div>
      ));
  };

  function renderSearch() {
    return (
      <div className="relative">
        <div className="absolute inset-y-0 start-0 flex items-center ps-3 pointer-events-none">
          <svg className="w-4 h-4 text-gray-200 dark:text-gray-400" aria-hidden="true" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 20 20">
            <path stroke="currentColor" strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="m19 19-4-4m0-7A7 7 0 1 1 1 8a7 7 0 0 1 14 0Z" />
          </svg>
        </div>
        <input type="search"
          name="search"
          key="search-input"
          autoFocus={true}
          ref={searchInputRef}
          id="default-search"
          className="block w-full p-4 ps-10 text-lg text-gray-900 border border-gray-300 rounded-lg bg-gray-200 focus:ring-blue-500 focus:border-blue-500"
          placeholder="Name or abbreviation (California or CA)..."
          onChange={event => setSearchTerm(event.target.value)}
          value={searchTerm || ""} />
        <button type="button"
          className="text-white absolute end-2.5 bottom-2.5 bg-gray-700 hover:bg-blue-800 focus:ring-4 focus:outline-none focus:ring-blue-300 font-medium rounded-lg text-sm px-4 py-2"
          onClick={event => setSearchTerm(null)}>
          Clear
        </button>
      </div>
    )
  }

  function saveNewSpots() {
    saveNewPlateData(formData);
    setShowPicker(false);
  }

  return (
    <>
      <Modal show={isShowPicker} onClose={() => setShowPicker(false)} initialFocus={searchInputRef} size="7xl">
        <Modal.Header className="[&>button]:hidden [&>h3]:grow">
          {renderSearch()}
        </Modal.Header>

        <Modal.Body>
          <div className="flex flex-col gap-5">
            {renderCheckboxes()}
          </div>
        </Modal.Body>

        <Modal.Footer>
          <div className="flex flex-row gap-3 grow justify-end">
            <Button onClick={() => setShowPicker(false)} color="gray" className="border-black">never mind</Button>
            <Button onClick={() => saveNewSpots()} className="bg-amber-800">Save Changes</Button>
          </div>
        </Modal.Footer>

      </Modal>
    </>
  )
}