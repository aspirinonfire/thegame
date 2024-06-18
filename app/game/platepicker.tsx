"use client"
import { mockAccount } from "../common/data";
import { LicensePlateSpot } from "../common/gameCore/gameModels";
import React, { useState } from 'react';

interface PickerControls {
  setShowPicker: (isShown: boolean) => void;
  saveNewPlateData: (plates: { [key: string]: LicensePlateSpot }) => void;
  plateData: { [key: string]: LicensePlateSpot };
}

export default function PlatePicker({ setShowPicker, saveNewPlateData, plateData }: PickerControls) {
  const [formData, setFormData] = useState(plateData);
  const [searchTerm, setSearchTerm] = useState<string | null>();

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
        <div key={plate.plateKey} className="my-4 text-black text-lg leading-relaxed" onClick={e => handleCheckboxChange(plate.plateKey, e)}>
          {/* disable default click behavior to allow clicking on entire div */}
          <label onClick={e => e.preventDefault()}>
            <input
              type="checkbox"
              name={plate.plateKey}
              checked={!!plate.dateSpotted}
              readOnly
              onClick={e => handleCheckboxChange(plate.plateKey, e)}
            />
            {plate.fullName}
          </label>
          {plate.country == "US" ? (<div className="plate-img" style={{
            backgroundImage: `url(${plate.plateImageUrl})`,
            height: "35vw",
            backgroundRepeat: "no-repeat",
            backgroundSize: "contain"
          }}></div>) : null}
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

  function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    saveNewPlateData(formData);
    setShowPicker(false);
  }

  return (
    <>
      <div
        className="justify-center items-center flex overflow-x-hidden overflow-y-hidden fixed inset-0 z-50 outline-none focus:outline-none">
        <div className="relative my-6 mx-auto min-w-full">
          {/*content*/}
          <div className="border-0 rounded-lg shadow-lg relative flex flex-col bg-white outline-none focus:outline-none">
            {/*header*/}
            <div className="flex-1 items-start p-5 border-b border-solid border-blueGray-200 rounded-t">
              {renderSearch()}
            </div>
            <form onSubmit={handleSubmit}>
              {/*body*/}
              {/* TODO add search */}
              <div className="relative p-6 flex-col overflow-y-auto max-h-72">
                {renderCheckboxes()}
              </div>
              {/*footer*/}
              <div className="flex items-center justify-end p-6 border-t border-solid border-blueGray-200 rounded-b">
                <button className="text-red-500 background-transparent font-bold uppercase px-6 py-2 text-sm outline-none focus:outline-none mr-1 mb-1 ease-linear transition-all duration-150"
                  type="button"
                  onClick={() => setShowPicker(false)}>
                  Close
                </button>
                <button className="bg-emerald-500 text-white active:bg-emerald-600 font-bold uppercase text-sm px-6 py-3 rounded shadow hover:shadow-lg outline-none focus:outline-none mr-1 mb-1 ease-linear transition-all duration-150"
                  type="submit">
                  Save Changes
                </button>
              </div>
            </form>
          </div>
        </div>
      </div>
      <div className="opacity-25 fixed inset-0 z-40 bg-black"></div>
    </>
  )
}