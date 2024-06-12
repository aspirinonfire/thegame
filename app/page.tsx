// page.tsx is a special Next.js file that exports a React component, and it's required for the route to be accessible
export default function Home() {
  return (
    <div className="flex flex-col justify-center gap-6 rounded-lg bg-gray-50 px-6 py-10 md:w-2/5 md:px-20">
      <p className={`text-xl text-gray-800 md:text-3xl md:leading-normal`}>
        ...main
      </p>
    </div>
  );
}