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
    stateOrProvince: string,
    country: Country,
    dateSpotted: Date | null,
    spottedBy: string | null
}