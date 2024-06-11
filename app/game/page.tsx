"use client"
import { useState } from 'react';
import PlatePicker from './platepicker';

export default function Game() {
  const [showPicker, setShowPicker] = useState(false);

  return (
    <div className="flex flex-col justify-center gap-6 rounded-lg bg-gray-50 px-6 py-10 md:w-2/5 md:px-20">
      <p className={`text-xl text-gray-800 md:text-3xl md:leading-normal`}>
        ...game
      </p>
      <button
        className="bg-blue-900 text-white active:bg-blue-600 font-bold uppercase text-sm px-6 py-3 rounded shadow hover:shadow-lg outline-none focus:outline-none mr-1 mb-1 ease-linear transition-all duration-150"
        type="button"
        onClick={() => setShowPicker(true)}>
        I spy...
      </button>
      { showPicker ? (<PlatePicker setShowPicker={(isShown: boolean) => setShowPicker(isShown)} /> ) : null }
    </div>
  )
}