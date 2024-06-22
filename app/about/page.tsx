"use client"
import { HiOutlineShare, HiOutlineTrash, HiOutlineExclamationCircle } from "react-icons/hi";
import { Button, Modal } from "flowbite-react";
import { useState } from "react";

export default function About() {
  const [isShowShareConfirmation, setIsShowShareConfirmation] = useState(false);
  const [isShowDeleteConfirmation, setIsShowDeleteConfirmation] = useState(false);

  function handleDeleteGameData() {
    setIsShowDeleteConfirmation(false);
    localStorage.clear();
    window.location.reload();
  }

  function handleShareApp() {
    const host = location.protocol.concat("//").concat(window.location.host);
    navigator.clipboard.writeText(host);
    setIsShowShareConfirmation(true);
  }

  return (
    <>
      <div className="flex flex-col gap-7 m-5">
        <div className="flex flex-col grow">
          <h2 className="text-3xl">The License Plate game</h2>
          <p className="text-lg">
            by Alex Chernyak &copy; 2024
          </p>
          <a href="https://www.linkedin.com/in/alexander-chernyak-5633ab92/" target="_blank" className="text-sm mt-3 underline">Visit my LinkedIn</a>
        </div>
        <div className="flex flex-wrap gap-2">
          <Button size="sm" className="bg-amber-700 animate-pulse" onClick={handleShareApp}>
            <HiOutlineShare className="mr-2 h-5 w-5" />
            Share the game
          </Button>
        </div>
        <div className="leading-relaxed">
          <p className="mt-5 text-xs">
            Made with Next.JS, Tailwind, Flowbite, coffee, ducktape, and WD-40
          </p>
        </div>
      </div>

      <div>
        <Button size="xs" color="failure" className="fixed bottom-0 right-0 m-5 opacity-50" onClick={() => setIsShowDeleteConfirmation(true)}>
          <HiOutlineTrash className="mr-1 h-4 w-4"/>
          Delete All Game Data
        </Button>
      </div>

      <Modal show={isShowDeleteConfirmation} size="md" onClose={() => setIsShowDeleteConfirmation(false)} popup>
        <Modal.Header />
        <Modal.Body>
          <div className="text-center">
            <HiOutlineExclamationCircle className="mx-auto mb-4 h-14 w-14 text-gray-400 dark:text-gray-200" />
            <h3 className="mb-5 text-lg font-normal text-gray-500 dark:text-gray-400">
              Are you sure you want to delete all game data?
            </h3>
            <div className="flex justify-center gap-4">
              <Button color="gray" onClick={handleDeleteGameData}>
                {"Yes, I'm sure"}
              </Button>
              <Button color="failure" onClick={() => setIsShowDeleteConfirmation(false)}>
                No, cancel
              </Button>
            </div>
          </div>
        </Modal.Body>
      </Modal>

      <Modal show={isShowShareConfirmation} size="sm" onClose={() => setIsShowShareConfirmation(false)} popup>
        <Modal.Header />
        <Modal.Body>
          <div className="text-center">
            <h3 className="mb-5 text-lg font-normal text-gray-500 dark:text-gray-400">
              Game URL was copied to your clipboard.
            </h3>
            <div className="flex justify-center">
              <Button color="gray" onClick={() => setIsShowShareConfirmation(false)}>
                Acknowledge
              </Button>
            </div>
          </div>
        </Modal.Body>

      </Modal>
    </>
  );
}