import { mockAccount } from "../common/data";
import { LicensePlate } from "../common/gameCore/gameModels";
import { useState } from 'react';

interface PickerControls {
  setShowPicker: (isShown: boolean) => void;
  saveNewPlateData: (plates: LicensePlate[]) => void;
  plateData: LicensePlate[];
}

export default function PlatePicker({ setShowPicker, saveNewPlateData, plateData }: PickerControls) {
  const [formData, setFormData] = useState(plateData);

  console.log("Rendering plate picker...");

  function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    saveNewPlateData(formData);
    setShowPicker(false);
  }

  function handleCheckboxChange(event: React.ChangeEvent<HTMLInputElement>) {
    // TODO pass in lookup??
    const plateKey = event.target.name;

    const matchingPlate = formData.find(plate => plate.stateOrProvince == plateKey);
    if (!matchingPlate) {
      console.error(`${plateKey} was not found! eh?!`);
      return;
    }
    
    const updatedPlate = {
      ...matchingPlate,
      dateSpotted: event.target.checked ? new Date() : null,
      // TODO wire up to backend
      spottedBy: event.target.checked ? mockAccount.name : null
    } as LicensePlate

    const updatedForm = formData.map(item => {
      if (item.stateOrProvince === updatedPlate.stateOrProvince) {
        return updatedPlate;
      }
      return item;
    });

    setFormData(updatedForm);
  }

  function renderCheckboxes() {
    return formData.map((item) => (
      // TODO use key
      // TODO make div clickable
      <div key={item.stateOrProvince} className="my-4 text-black text-lg leading-relaxed">
        <label>
          <input
            type="checkbox"
            // TODO use key
            name={item.stateOrProvince}
            checked={!!item.dateSpotted}
            onChange={handleCheckboxChange}
          />
          {/* TODO use full name */}
          {item.stateOrProvince}
        </label>
        {/* TODO add image */}
      </div>
    ));
  };

  return (
    <>
      <div
        className="justify-center items-center flex overflow-x-hidden overflow-y-hidden fixed inset-0 z-50 outline-none focus:outline-none">
        <div className="relative my-6 mx-auto min-w-full">
          {/*content*/}
          <div className="border-0 rounded-lg shadow-lg relative flex flex-col bg-white outline-none focus:outline-none">
            {/*header*/}
            <div className="flex items-start justify-between p-5 border-b border-solid border-blueGray-200 rounded-t">
              <h3 className="text-3xl font-semibold text-black">
                I Spy...
              </h3>
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