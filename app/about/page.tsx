"use client"

export default function About() {
  function clearLocalStorage() {
    localStorage.clear();
    window.location.reload();
  }

  return (
    <>
      <h2 className="text-3xl pb-3">The License Plate game</h2>
      <div className="text-small leading-relaxed">
        <p>
          Alex Chernyak &copy; 2024
        </p>
        <p>
          Made with Next.JS, Tailwind, Flowbite, coffee, ducktape, and WD-40
        </p>
      </div>
      
      <div>
        <button type="button"
          className="fixed bottom-0 right-0 p-3 m-5 bg-red-800 hover:bg-red-700 focus:ring-gray-700 border-gray-700 focus:outline-none focus:ring-4 font-medium rounded-lg"
          onClick={clearLocalStorage}>
           !! Delete All Game Data !!
        </button>
    </div>
    </>
  );
}