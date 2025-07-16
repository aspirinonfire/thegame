import { useShallow } from "zustand/shallow";
import { Button, Modal, ModalBody, ModalFooter, ModalHeader } from "flowbite-react";
import { ExclamationTriangleIcon } from "@heroicons/react/24/outline";
import { useEffect, useState } from "react";
import { useAppState } from "~/appState/useAppState";

/**
 * This component is responsible for show various app messages (eg. API errors, etc)
 */
const AppMessages = () => {
  const [error, dequeueError] = useAppState(useShallow((s) => 
    [s.apiErrors[0], s.dequeueError]));

  const [open, setOpen] = useState(false)

  // open whenever a new problem arrives
  useEffect(() => setOpen(!!error), [error])

  const close = () => {
    setOpen(false)
    dequeueError()
  }

  const renderErrorDetails = () => {
    if (!error?.errors) {
      return;
    }

    return Object.keys(error.errors).map(propName => {
      const propErrors = (error.errors ?? {})[propName];

      const errorDetails = propErrors.map((error, idx) => <p key={idx}>{error}</p>);

      return <div key={propName} className="pb-2">
        <p className="font-semibold">{propName}:&nbsp;</p>
        {errorDetails}
      </div>
    })
  }

  return (
    <Modal show={open} dismissible onClose={close} size="md" popup className="text-gray-600">
      <ModalHeader />
      
      <ModalBody className="flex flex-col items-center justify-center">
        <ExclamationTriangleIcon color="red" className="h-10 w-10" />
        <div className="text-sm font-semibold">{error?.title ?? 'Error'}</div>
        <div className="text-xs">Operation ID: {error?.GameRequestCorrelationId}</div>
        {!!error?.detail &&
          <div className="pt-5 text-xs text-center">
            {error.detail}
          </div>
        }
        
        {!!error?.errors &&
          <div className="flex flex-col pt-5 text-xs text-center">
            {renderErrorDetails()}
          </div>
        }
      </ModalBody>

      <ModalFooter className="flex flex-col items-center flex-wrap">
        <Button size="xs" color="light" onClick={close}>Dismiss</Button>
      </ModalFooter>
    </Modal>
  )
}

export default AppMessages;