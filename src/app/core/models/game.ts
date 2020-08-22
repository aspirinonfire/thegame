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
    dateSpotted: Date,
    spottedBy: string
}