export default function About() {
  return (
    <div className="flex flex-col justify-center gap-6 rounded-lg bg-gray-50 px-6 py-10 md:w-2/5 md:px-20">
      <h2 className="text-3xl text-gray-800">The License Plate game</h2>
      <p className={`text-gray-800 md:text-small md:leading-normal`}>
        Alex Chernyak &copy; 2024
      </p>
    </div>
  )
}