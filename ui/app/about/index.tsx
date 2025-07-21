import { Button, Card, Modal, ModalBody, ModalHeader } from "flowbite-react";
import { useEffect, useState } from "react";
import { ArrowPathIcon, ExclamationCircleIcon, LockClosedIcon, ShareIcon, TrashIcon } from "@heroicons/react/24/outline";
import type { Route } from "./+types";
import LocalDateTime from "~/common-components/localDateTime";
import { useAppState } from "~/appState/useAppState";

export interface VersionInfo {
  deployDate: Date,
  sha: string
}

export async function clientLoader({
  params,
}: Route.ClientLoaderArgs) {
  return {
    versionInfoProm: fetch("/version.json")
  }
}

const AboutPage = ({
  loaderData,
} : Route.ComponentProps) => {
  const [isShowShareConfirmation, setIsShowShareConfirmation] = useState(false);
  const [isShowDeleteConfirmation, setIsShowDeleteConfirmation] = useState(false);
  const [versionInfo, setVersionString] = useState<VersionInfo>({
    deployDate: new Date("2025-06-18T00:00:00Z"),
    sha: "unknown"
  });

  const refreshApiAccessToken = useAppState(s => s.refreshAccessToken);

  useEffect(() => {
    loaderData.versionInfoProm
      .then(resp => {
        if (resp.ok && !resp.bodyUsed) {
          resp.json()
            .then((info: VersionInfo) => {
              setVersionString(info);
            })
            .catch(console.error);
        }
        // non-ok response is ignored

      })
    // ignore error
    .catch(console.error);

  }, []);

  const handleDeleteGameData = () => {
    setIsShowDeleteConfirmation(false);
    localStorage.clear();
    window.location.reload();
  };

  const handleShareApp = () => {
    const host = location.protocol.concat("//").concat(window.location.host);
    navigator.clipboard.writeText(host);
    setIsShowShareConfirmation(true);
  };

  const restartApp = async () => {
    try {
      const keys = await window.caches.keys();
      await Promise.all(keys.map(key => caches.delete(key)));
    } catch (err) {
      console.log('deleteCache err: ', err);
    }

    const workers = await navigator.serviceWorker.getRegistrations();
    workers.forEach(worker => worker.unregister());

    window.location.reload();
  };

  return <>
    <Card className="bg-gray-600">
      <div className="flex flex-col gap-7 m-5">
        <div className="flex flex-col grow">
          <h2 className="text-3xl">The License Plate game</h2>
          <p className="text-lg">
            by Alex Chernyak &copy; {new Date().getFullYear()}
          </p>
        </div>
        <div className="flex flex-wrap gap-2">
          <Button size="sm" className="bg-amber-700 animate-pulse" onClick={handleShareApp}>
            <ShareIcon className="mr-2 h-5 w-5" />
            Share the game
          </Button>
        </div>
        <div className="leading-relaxed mt-5 text-xs">
          <p>
            Made with React, Flowbite, ASP Net Core, coffee, ducktape, and WD-40
          </p>
          <p className="text-gray-400 pt-5">
            App deploy date: <LocalDateTime isoDateTime={versionInfo.deployDate} format="MMMM D, YYYY" />. Commit: {versionInfo.sha}
          </p>
        </div>
      </div>

      <div>
        <Button size="xs" color="gray" className="fixed bottom-0 left 0 mb-5 opacity-50" onClick={restartApp} >
          <ArrowPathIcon className="mr-1 h-4 w-4" />
          Reload App
        </Button>

        <Button size="xs" color="black" className="fixed bottom-0 left-40 mb-5 opacity-50" onClick={refreshApiAccessToken}>
          <LockClosedIcon className="mr-1 h-4 w-4" />
          Refresh Access
        </Button>

        <Button size="xs" color="failure" className="fixed bottom-0 right-0 m-5 opacity-50" onClick={() => setIsShowDeleteConfirmation(true)}>
          <TrashIcon className="mr-1 h-4 w-4"/>
          Delete All Game Data
        </Button>
      </div>

      <Modal show={isShowDeleteConfirmation} size="md" onClose={() => setIsShowDeleteConfirmation(false)} popup>
        <ModalHeader />
        <ModalBody>
          <div className="text-center">
            <ExclamationCircleIcon className="mx-auto mb-4 h-14 w-14 text-red-500" />
            <h3 className="mb-5 text-lg font-normal text-gray-400">
              Are you sure you want to delete all game data?
            </h3>
            <div className="flex justify-center gap-4">
              <Button color="red" onClick={handleDeleteGameData}>
                Yes, I'm sure
              </Button>
              <Button color="gray" onClick={() => setIsShowDeleteConfirmation(false)}>
                No, cancel
              </Button>
            </div>
          </div>
        </ModalBody>
      </Modal>

      <Modal show={isShowShareConfirmation} size="sm" onClose={() => setIsShowShareConfirmation(false)} popup>
        <ModalHeader />
        <ModalBody>
          <div className="text-center">
            <h3 className="mb-5 text-lg font-normal text-gray-400">
              Game URL was copied to your clipboard.
            </h3>
            <div className="flex justify-center">
              <Button color="gray" onClick={() => setIsShowShareConfirmation(false)}>
                Acknowledge
              </Button>
            </div>
          </div>
        </ModalBody>

      </Modal>
    </Card>
  </>
}

export default AboutPage;