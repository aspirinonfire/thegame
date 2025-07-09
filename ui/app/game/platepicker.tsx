import React, { useEffect, useRef, useState } from 'react';
import { Button, Modal, ModalBody, ModalFooter, ModalHeader } from "flowbite-react";
import type { LicensePlateSpot } from '~/game-core/models/LicensePlateSpot';
import type { Territory } from '~/game-core/models/Territory';
import { useAppStore } from '~/useAppStore';
import { territories } from '~/game-core/gameConfiguration';

interface PickerControls {
  isShowPicker: boolean;
  setShowPicker: (isShown: boolean) => void;
  saveNewPlateData: (plates: LicensePlateSpot[]) => void;
  plateData: LicensePlateSpot[];
}

export const PlatePicker = ({ isShowPicker, setShowPicker, saveNewPlateData, plateData }: PickerControls) => {
  const user = useAppStore(state => state.activeUser);

  const [formChangePreview, setFormChangePreview] = useState<Map<string, boolean>>(new Map<string, boolean>());

  useEffect(() => {
    setFormChangePreview(new Map<string, boolean>());
  }, [isShowPicker]);

  const plateByKeyLkp = plateData.reduce((lkp, plate) => {
    lkp[plate.key] = plate;
    return lkp;
  }, {} as {[key: string]: LicensePlateSpot});
  const [formData, setFormData] = useState(plateByKeyLkp);
  const [searchTerm, setSearchTerm] = useState<string | null>();

  const searchInputRef = useRef<HTMLInputElement>(null);

  const territoriesToRender = territories
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

    const currentFormChanges = formChangePreview;

    const matchingPlate = updatedForm[territory.key];
    if (!!matchingPlate) {
      delete updatedForm[territory.key];
      currentFormChanges.set(territory.key, false);
    } else {
      updatedForm[territory.key] = {
        key: territory.key,
        dateSpotted: new Date(),
        spottedBy: user?.name ?? "n/a"
      } as LicensePlateSpot;
      currentFormChanges.set(territory.key, true);
    }
    
    setFormData(updatedForm);
    setSearchTerm(null);
    setFormChangePreview(currentFormChanges);
  }

  function handelSaveNewSpots() {
    saveNewPlateData(Object.values(formData));
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
        <g stroke="none" strokeWidth="1" fillRule="evenodd">
          <path d="M5.75,3 L18.25,3 C19.7687831,3 21,4.23121694 21,5.75 L21,18.25 C21,19.7687831 19.7687831,21 18.25,21 L5.75,21 C4.23121694,21 3,19.7687831 3,18.25 L3,5.75 C3,4.23121694 4.23121694,3 5.75,3 Z M5.75,4.5 C5.05964406,4.5 4.5,5.05964406 4.5,5.75 L4.5,18.25 C4.5,18.9403559 5.05964406,19.5 5.75,19.5 L18.25,19.5 C18.9403559,19.5 19.5,18.9403559 19.5,18.25 L19.5,5.75 C19.5,5.05964406 18.9403559,4.5 18.25,4.5 L5.75,4.5 Z" id="🎨Color">
          </path>
        </g>
      </svg>
    )
  }

  function renderCheckedBox() {
    return (
      <svg width="50px" height="50px" viewBox="0 0 24 24" version="1.1" xmlns="http://www.w3.org/2000/svg">
        <g stroke="none" strokeWidth="1" fillRule="evenodd">
          <path d="M18.25,3 C19.7687831,3 21,4.23121694 21,5.75 L21,18.25 C21,19.7687831 19.7687831,21 18.25,21 L5.75,21 C4.23121694,21 3,19.7687831 3,18.25 L3,5.75 C3,4.23121694 4.23121694,3 5.75,3 L18.25,3 Z M18.25,4.5 L5.75,4.5 C5.05964406,4.5 4.5,5.05964406 4.5,5.75 L4.5,18.25 C4.5,18.9403559 5.05964406,19.5 5.75,19.5 L18.25,19.5 C18.9403559,19.5 19.5,18.9403559 19.5,18.25 L19.5,5.75 C19.5,5.05964406 18.9403559,4.5 18.25,4.5 Z M10,14.4393398 L16.4696699,7.96966991 C16.7625631,7.6767767 17.2374369,7.6767767 17.5303301,7.96966991 C17.7965966,8.23593648 17.8208027,8.65260016 17.6029482,8.94621165 L17.5303301,9.03033009 L10.5303301,16.0303301 C10.2640635,16.2965966 9.84739984,16.3208027 9.55378835,16.1029482 L9.46966991,16.0303301 L6.46966991,13.0303301 C6.1767767,12.7374369 6.1767767,12.2625631 6.46966991,11.9696699 C6.73593648,11.7034034 7.15260016,11.6791973 7.44621165,11.8970518 L7.53033009,11.9696699 L10,14.4393398 L16.4696699,7.96966991 L10,14.4393398 Z" id="🎨Color">
          </path>
        </g>
      </svg>
    )
  }

  function renderCheckboxes() {
    return territoriesToRender
      .map((territory) => (
        <div key={territory.key}
          data-testid={`select-plate-${territory.key}`}
          onClick={e => handleCheckboxChange(territory, e)}
          className="flex flex-row grow gap-3 text-black justify-start items-center">
          <div className="flex w-1/5 md:w-1/4 justify-end">
            { !!formData[territory.key] ? renderCheckedBox() : renderUncheckedBox()}
          </div>
          <div className="flex flex-col flex-grow justify-start">
            {territory.country == "US" ?
              // TODO better image rendering
              (<img src={`./plates/${territory.key}.jpg`.toLowerCase()}
                alt={territory.shortName}
                width="300"
                height="500"
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
          className="block w-full p-2 ps-10 placeholder:text-base text-gray-900 border border-gray-300 rounded-lg bg-gray-200 focus:ring-blue-500 focus:border-blue-500"
          placeholder="Name or abbreviation"
          onChange={event => setSearchTerm(event.target.value)}
          value={searchTerm || ""} />
        <button type="button"
          className="text-white absolute end-2 bottom-1.25 bg-gray-700 hover:bg-blue-800 focus:ring-4 focus:outline-none focus:ring-blue-300 rounded-lg text-sm px-3 py-1.5"
          onClick={event => setSearchTerm(null)}>
          Clear
        </button>
      </div>
    )
  }

  function renderFormChangePreview() {
    return <>
      { formChangePreview.size > 0 &&
        <div className="flex flex-row gap-1 text-xs text-gray-500 pt-2">
        Updates: {[...formChangePreview].map(([key, value]) => {
          return (<span key={key} className={!value ? "line-through" : ""}>{key}</span>)
        })}
      </div>
      }
    </>
  }

  return (
    <>
      <Modal dismissible show={isShowPicker} onClose={handleClose} initialFocus={searchInputRef} size="xl">
        <ModalHeader className="[&>button]:hidden [&>h3]:grow">
          {renderSearch()}
          {renderFormChangePreview()}
        </ModalHeader>

        <ModalBody className={formChangePreview.size > 0 ? "pt-0" : "pt-1"}>
          <div className="flex flex-col gap-10">
            {renderCheckboxes()}
          </div>
        </ModalBody>

        <ModalFooter>
          <div className="flex flex-row grow items-center gap-5 justify-between">
            <Button onClick={handleClose} data-testid="cancel-spotted-changes" color="gray" className="border-black">never mind</Button>
            <Button onClick={handelSaveNewSpots} data-testid="save-spotted-changes" className="bg-amber-800">Save Changes</Button>
          </div>
        </ModalFooter>

      </Modal>
    </>
  )
}

export default PlatePicker;