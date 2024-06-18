"use client"

export default function About() {
  function clearLocalStorage() {
    localStorage.clear();
    window.location.reload();
  }

  return (
    <>
      <h2 className="text-3xl text-gray-800">The License Plate game</h2>
      <p className={`text-gray-800 md:text-small md:leading-normal`}>
        Alex Chernyak &copy; 2024
      </p>
      <div className="text-black">
        <button type="button" className="text-white bg-red-800 hover:bg-red-900 focus:outline-none focus:ring-4 focus:ring-gray-300 font-medium rounded-lg text-sm px-5 py-2.5 me-2 m-4 dark:bg-red-800 dark:hover:bg-red-700 dark:focus:ring-gray-700 dark:border-gray-700"
          onClick={clearLocalStorage}>
           !! Delete All Game Data !!
        </button>
    </div>
    </>
  );
}