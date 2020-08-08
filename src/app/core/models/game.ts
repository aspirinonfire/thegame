import { UsStates, CanadaProvinces } from "./territories";

export interface Game {
    id: string,
    name: string,
    createdBy: string,
    dateCreated: Date,
    dateFinished?: Date,
    licensePlates: LicensePlate[]
}

export interface LicensePlate {
    stateOrProvice: UsStates | CanadaProvinces,
    dateSpotted: Date,
    spottedBy: string
}