"use client"
import { mockAccount } from "../common/data";
import { LicensePlateSpot, Territory } from "../common/gameCore/gameModels";
import React, { useRef, useState } from 'react';
import { Button, Modal } from "flowbite-react";
import Image from 'next/image'
import { GetPlateDataForRendering, GetTerritoryKey } from "../common/gameCore/gameRepository";

interface PickerControls {
  isShowPicker: boolean;
  setShowPicker: (isShown: boolean) => void;
  saveNewPlateData: (plates: { [key: string]: LicensePlateSpot }) => void;
  plateData: { [key: string]: LicensePlateSpot };
}

export default function PlatePicker({ isShowPicker, setShowPicker, saveNewPlateData, plateData }: PickerControls) {
  const [formData, setFormData] = useState(plateData);
  const [searchTerm, setSearchTerm] = useState<string | null>();

  const searchInputRef = useRef<HTMLInputElement>(null);

  const territoriesToRender = GetPlateDataForRendering()
    .filter(plate => {
      if (!searchTerm) {
        return true;
      }

      const searchValue = searchTerm.toLowerCase();

      const plateName = plate.longName.toLowerCase()

      // full name starts with
      if (plateName.startsWith(searchValue)) {
        return true;
      }

      // contained in the second word
      if (plateName.includes(` ${searchValue}`)) {
        return true;
      }

      // short name matches
      if (plate.shortName.toLowerCase() == searchValue) {
        return true;
      }

      return false;
    });

  function handleCheckboxChange(territory: Territory, clickEvent: React.MouseEvent) {
    clickEvent.stopPropagation();

    const updatedForm = {
      ...formData
    };

    const plateKey = GetTerritoryKey(territory);

    const matchingPlate = updatedForm[plateKey];
    if (!!matchingPlate) {
      delete updatedForm[plateKey];
    } else {
      updatedForm[plateKey] = {
        country: territory.country,
        stateOrProvince: territory.shortName,

        dateSpotted: new Date(),
        spottedBy: mockAccount.playerName
      } as LicensePlateSpot;
    }
    
    setFormData(updatedForm);
    setSearchTerm(null);
  }

  function handelSaveNewSpots() {
    saveNewPlateData(formData);
    setShowPicker(false);
    setSearchTerm(null);
  }

  function handleClose() {
    setShowPicker(false);
    setSearchTerm(null);
  }

  function renderUncheckedBox() {
    return (
      <svg width="50px" height="50px" className="fill-gray-400" viewBox="0 0 24 24" version="1.1" xmlns="http://www.w3.org/2000/svg">
        <title>ic_fluent_checkbox_unchecked_24_regular</title>
        <desc>Created with Sketch.</desc>
        <g id="🔍-Product-Icons" stroke="none" strokeWidth="1" fillRule="evenodd">
          <path d="M5.75,3 L18.25,3 C19.7687831,3 21,4.23121694 21,5.75 L21,18.25 C21,19.7687831 19.7687831,21 18.25,21 L5.75,21 C4.23121694,21 3,19.7687831 3,18.25 L3,5.75 C3,4.23121694 4.23121694,3 5.75,3 Z M5.75,4.5 C5.05964406,4.5 4.5,5.05964406 4.5,5.75 L4.5,18.25 C4.5,18.9403559 5.05964406,19.5 5.75,19.5 L18.25,19.5 C18.9403559,19.5 19.5,18.9403559 19.5,18.25 L19.5,5.75 C19.5,5.05964406 18.9403559,4.5 18.25,4.5 L5.75,4.5 Z" id="🎨Color">
          </path>
        </g>
      </svg>
    )
  }

  function renderCheckedBox() {
    return (
      <svg width="50px" height="50px" viewBox="0 0 24 24" version="1.1" xmlns="http://www.w3.org/2000/svg">
        <title>ic_fluent_checkbox_checked_24_regular</title>
        <desc>Created with Sketch.</desc>
        <g id="🔍-Product-Icons" stroke="none" strokeWidth="1" fillRule="evenodd">
          <path d="M18.25,3 C19.7687831,3 21,4.23121694 21,5.75 L21,18.25 C21,19.7687831 19.7687831,21 18.25,21 L5.75,21 C4.23121694,21 3,19.7687831 3,18.25 L3,5.75 C3,4.23121694 4.23121694,3 5.75,3 L18.25,3 Z M18.25,4.5 L5.75,4.5 C5.05964406,4.5 4.5,5.05964406 4.5,5.75 L4.5,18.25 C4.5,18.9403559 5.05964406,19.5 5.75,19.5 L18.25,19.5 C18.9403559,19.5 19.5,18.9403559 19.5,18.25 L19.5,5.75 C19.5,5.05964406 18.9403559,4.5 18.25,4.5 Z M10,14.4393398 L16.4696699,7.96966991 C16.7625631,7.6767767 17.2374369,7.6767767 17.5303301,7.96966991 C17.7965966,8.23593648 17.8208027,8.65260016 17.6029482,8.94621165 L17.5303301,9.03033009 L10.5303301,16.0303301 C10.2640635,16.2965966 9.84739984,16.3208027 9.55378835,16.1029482 L9.46966991,16.0303301 L6.46966991,13.0303301 C6.1767767,12.7374369 6.1767767,12.2625631 6.46966991,11.9696699 C6.73593648,11.7034034 7.15260016,11.6791973 7.44621165,11.8970518 L7.53033009,11.9696699 L10,14.4393398 L16.4696699,7.96966991 L10,14.4393398 Z" id="🎨Color">
          </path>
        </g>
      </svg>
    )
  }

  function renderCheckboxes() {
    return territoriesToRender
      .map((territory) => (
        <div key={GetTerritoryKey(territory)}
          onClick={e => handleCheckboxChange(territory, e)}
          className="flex flex-row grow gap-3 text-black justify-start items-center">
          <div className="flex w-1/5 md:w-1/4 justify-end">
            { !!formData[GetTerritoryKey(territory)] ? renderCheckedBox() : renderUncheckedBox()}
          </div>
          <div className="flex flex-col flex-grow justify-start">
            {territory.country == "US" ?
              (<Image src={`/${territory.licensePlateImgs[0]}`}
                unoptimized
                alt={territory.shortName}
                width="300"
                height="500"
                placeholder="blur"
                blurDataURL="data:image/jpeg;base64,/9j/4AAQSkZJRgABAQEBLAEsAAD/2wBDAAYEBQYFBAYGBQYHBwYIChAKCgkJChQODwwQFxQYGBcUFhYaHSUfGhsjHBYWICwgIyYnKSopGR8tMC0oMCUoKSj/2wBDAQcHBwoIChMKChMoGhYaKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCj/wAARCAAHAAoDASIAAhEBAxEB/8QAHwAAAQUBAQEBAQEAAAAAAAAAAAECAwQFBgcICQoL/8QAtRAAAgEDAwIEAwUFBAQAAAF9AQIDAAQRBRIhMUEGE1FhByJxFDKBkaEII0KxwRVS0fAkM2JyggkKFhcYGRolJicoKSo0NTY3ODk6Q0RFRkdISUpTVFVWV1hZWmNkZWZnaGlqc3R1dnd4eXqDhIWGh4iJipKTlJWWl5iZmqKjpKWmp6ipqrKztLW2t7i5usLDxMXGx8jJytLT1NXW19jZ2uHi4+Tl5ufo6erx8vP09fb3+Pn6/8QAHwEAAwEBAQEBAQEBAQAAAAAAAAECAwQFBgcICQoL/8QAtREAAgECBAQDBAcFBAQAAQJ3AAECAxEEBSExBhJBUQdhcRMiMoEIFEKRobHBCSMzUvAVYnLRChYkNOEl8RcYGRomJygpKjU2Nzg5OkNERUZHSElKU1RVVldYWVpjZGVmZ2hpanN0dXZ3eHl6goOEhYaHiImKkpOUlZaXmJmaoqOkpaanqKmqsrO0tba3uLm6wsPExcbHyMnK0tPU1dbX2Nna4uPk5ebn6Onq8vP09fb3+Pn6/9oADAMBAAIRAxEAPwDet/HOifZQVihBjTaM2QBAC8Drg9ucc4/Gqc3jfw9JNI5tlO5icixQf1ooqnK/QLn/2Q=="
                className="w-300 h-auto" />) :
              (<h1 className="text-2xl">{territory.longName} ({territory.country})</h1>)}
          </div>
        </div>
      ));
  };

  function renderSearch() {
    return (
      <div className="relative">
        <div className="absolute inset-y-0 start-0 flex items-center ps-3 pointer-events-none">
          <svg className="w-4 h-4 text-gray-400" aria-hidden="true" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 20 20">
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

  return (
    <>
      <Modal dismissible show={isShowPicker} onClose={handleClose} initialFocus={searchInputRef} size="2xl">
        <Modal.Header className="[&>button]:hidden [&>h3]:grow">
          {renderSearch()}
        </Modal.Header>

        <Modal.Body>
          <div className="flex flex-col gap-10">
            {renderCheckboxes()}
          </div>
        </Modal.Body>

        <Modal.Footer>
          <div className="flex flex-row grow items-center gap-5 justify-between">
            <Button onClick={handleClose} color="gray" className="border-black">never mind</Button>
            <Button onClick={handelSaveNewSpots} className="bg-amber-800">Save Changes</Button>
          </div>
        </Modal.Footer>

      </Modal>
    </>
  )
}