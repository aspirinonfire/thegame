import { Country } from './gameDataTerritory';

export interface Game {
    id: string,
    name: string,
    createdBy: string,
    dateCreated: Date,
    dateFinished?: Date,
    licensePlates: { [K: string]: LicensePlate }
}

export interface LicensePlate {
    stateOrProvice: string,
    country: Country,
    dateSpotted: Date,
    spottedBy: string
}