import React, { useEffect, useMemo, useRef, useState } from 'react';
import { Button, Modal, ModalBody, ModalFooter, ModalHeader, ToggleSwitch } from "flowbite-react";
import type { LicensePlateSpot } from '~/game-core/models/LicensePlateSpot';
import type { Territory } from '~/game-core/models/Territory';
import { useAppState } from '~/appState/useAppState';
import { territories } from '~/game-core/gameConfiguration';
import { MagnifyingGlassIcon } from '@heroicons/react/24/outline';
import { useShallow } from 'zustand/shallow';

interface PickerControls {
  isShowPicker: boolean;
  setShowPicker: (isShown: boolean) => void;
  saveNewPlateData: (plates: LicensePlateSpot[]) => void;
  plateData: LicensePlateSpot[];
}

interface TerritoryToRender extends Territory {
  searchProbability: number | undefined;
  aiPrompt?: string | null;
  isSelected: boolean;
  isVisible: boolean;
}

export const PlatePicker = ({ isShowPicker, setShowPicker, saveNewPlateData, plateData }: PickerControls) => {
  const [user, getMatchingPlates] = useAppState(useShallow(state =>
    [state.activeUser, state.getMatchingPlates]));

  const initialSelectedLookup = useMemo(() => plateData.reduce((lkp, plate) => {
    lkp[plate.key] = true;
    return lkp;
  }, {} as { [key: string]: boolean }), [plateData]);

  const [platesToRender, setPlatesToRender] = useState<TerritoryToRender[]>(() =>
    territories
      .map(ter => ({
        ...ter,
        searchProbability: undefined,
        aiPrompt: null,
        isSelected: !!initialSelectedLookup[ter.key],
        isVisible: true,
      }))
      .sort((a, b) => a.longName.localeCompare(b.longName))
  );
  const [searchTerm, setSearchTerm] = useState<string | null>();
  const [isSearching, setIsSearching] = useState(false);
  const searchInputRef = useRef<HTMLInputElement>(null);
  const debounceTimerRef = useRef<NodeJS.Timeout>(null);

  const [isAiMode, setIsAiMode] = useState(false);
  useEffect(() => {
    if (!!debounceTimerRef.current) {
      clearTimeout(debounceTimerRef.current);
    }
    setIsSearching(false);

    if (!searchTerm) {
      setIsAiMode(false);
      setPlatesToRender(prev => prev
        .map(p => ({ ...p, isVisible: true, searchProbability: undefined }))
        .sort((a, b) => a.longName.localeCompare(b.longName))
      );
      return;
    }

    // do a quick search, and fallback to ai search on no quick match
    const quick = getQuickSearchResults(platesToRender, searchTerm);
    const hasQuickMatch = quick.some(p => p.isVisible);
    if (hasQuickMatch) {
      setIsAiMode(false);
      setPlatesToRender(quick);
    } else {
      setIsSearching(true);
      setIsAiMode(true);
      
      const debounceTimeout = setTimeout(() => {
        getPlatesMatchingAiSearch(platesToRender, searchTerm)
          .then(setPlatesToRender)
          .finally(() => setIsSearching(false))
      }, 1000);

      if (!!debounceTimerRef.current) {
        clearTimeout(debounceTimerRef.current);
      }
      debounceTimerRef.current = debounceTimeout;
    }
  }, [searchTerm]);


  const [formChangePreview, setFormChangePreview] = useState<Map<string, boolean>>(new Map<string, boolean>());

  useEffect(() => {
    setFormChangePreview(new Map<string, boolean>());
  }, [isShowPicker]);

  async function getPlatesMatchingAiSearch(current: TerritoryToRender[], query: string) {
    const scoredMatches = await getMatchingPlates(query);
    const probByKey = new Map<string, number>(scoredMatches.map(m => [m.label.toLowerCase(), m.probability]));
    return current
      .map(p => {
        const prob = probByKey.get(p.key.toLowerCase());
        const visible = (prob ?? 0) > 0.03;
        return { ...p, searchProbability: prob ?? undefined, isVisible: visible } as TerritoryToRender;
      })
      .sort((a, b) => (b.searchProbability ?? 0) - (a.searchProbability ?? 0));
  }

  function getQuickSearchResults(current: TerritoryToRender[], query: string | null | undefined) {
    if (!query) {
      return current
        .map(p => ({ ...p, isVisible: true, searchProbability: undefined }) as TerritoryToRender)
        .sort((a, b) => a.longName.localeCompare(b.longName));
    }

    const searchL = query.toLowerCase();
    return current
      .map(p => {
        const plateName = p.longName.toLowerCase();
        const visible = plateName.startsWith(searchL) || plateName.includes(` ${searchL}`) || p.shortName.toLowerCase() === searchL;
        return { ...p, isVisible: visible, searchProbability: undefined } as TerritoryToRender;
      })
      .sort((a, b) => a.longName.localeCompare(b.longName));
  }

  // quick search matching handled inline in effect

  function handleCheckboxChange(territory: TerritoryToRender, clickEvent: React.MouseEvent) {
    clickEvent.stopPropagation();

    const newSelected = !territory.isSelected;
    setPlatesToRender(prev => prev.map(t => {
      if (t.key !== territory.key) { return t; }
      const next: TerritoryToRender = { ...t, isSelected: newSelected };
      if (newSelected && isAiMode && searchTerm) {
        next.aiPrompt = searchTerm;
      } else if (!newSelected) {
        next.aiPrompt = null;
      }
      return next;
    }));

    // Track preview vs initial
    setFormChangePreview(prev => {
      const next = new Map(prev);
      const initiallySelected = !!initialSelectedLookup[territory.key];
      if (newSelected === initiallySelected) {
        next.delete(territory.key);
      } else {
        next.set(territory.key, newSelected);
      }
      return next;
    });

    // aiPrompt handled in the single setPlatesToRender above

    // keep current search term to allow multiple selections within the same mode
  }

  function handelSaveNewSpots() {
    const playerId = user?.player?.playerId ?? -1;
    const playerName = user?.player?.playerName ?? "n/a";

    const toSave = platesToRender
      .filter(t => t.isSelected)
      .map<LicensePlateSpot>(t => ({
        key: t.key,
        country: t.country,
        stateOrProvince: t.shortName,
        spottedOn: new Date(),
        spottedByPlayerId: playerId,
        spottedByPlayerName: playerName,
        mlPrompt: t.aiPrompt ?? null
      }));
    saveNewPlateData(toSave);
    setShowPicker(false);
    // keep searchTerm to allow multiple selections within current mode
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
    return platesToRender
      .filter(t => t.isVisible)
      .map((territory) => (
        <div key={territory.key}
          data-testid={`select-plate-${territory.key}`}
          onClick={e => !isSearching && handleCheckboxChange(territory, e)}
          className="flex flex-row grow gap-3 text-black justify-start items-center">
          <div className={`flex w-1/5 md:w-1/4 justify-end ${isSearching ? "blur-[2px]" : null}`}>
            { territory.isSelected ? renderCheckedBox() : renderUncheckedBox() }
          </div>
          <div className="flex flex-col flex-grow justify-start">
            {territory.country == "US" ?
              // TODO better image rendering
              (<img src={`./plates/${territory.key}.jpg`.toLowerCase()}
                alt={territory.shortName}
                width="300"
                height="500"
                className={`w-300 h-auto ${ isSearching ? "blur-[3px] grayscale-90 opacity-50" : null }`} />) :
              (<h1 className="text-2xl">{territory.longName} ({territory.country})</h1>)}
            { !!territory.searchProbability && !isSearching &&
              <span className="text-xs text-gray-500">
                Probability:&nbsp;{(territory.searchProbability! * 100.0).toFixed(2)}%
              </span>
            }
          </div>
        </div>
      ));
  };

  function renderSearch() {
    return (
      <div className="relative">
        <MagnifyingGlassIcon className="h-6 absolute m-2 text-gray-400" />
        <input type="search"
          name="search"
          key="search-input"
          autoFocus={true}
          ref={searchInputRef}
          id="default-search"
          data-testid="plate-search-input"
          className="block w-full p-2 ps-10 placeholder:text-base text-gray-900 border border-gray-300 rounded-lg bg-gray-200 focus:ring-blue-500 focus:border-blue-500"
          placeholder="Name, abbreviation, or visual description"
          onChange={event => setSearchTerm(event.target.value)}
          value={searchTerm || ""} />
        <button type="button"
          className="text-white absolute end-2 bottom-1.25 bg-gray-700 hover:bg-blue-800 focus:ring-4 focus:outline-none focus:ring-blue-300 rounded-lg text-sm px-3 py-1.5"
          data-testid="plate-search-clear"
          onClick={_ => setSearchTerm(null)}>
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
