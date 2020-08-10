import { Game, LicensePlate } from '.';

export class GameVm implements Game {
  public readonly id: string;
  public readonly name: string;
  public readonly createdBy: string;
  public readonly dateCreated: Date;
  public readonly dateFinished?: Date | undefined;
  public readonly licensePlates: { [K: string]: LicensePlate; };

  public readonly platesSpotted: number;

  constructor(game: Game) {
    this.id = game.id;
    this.name = game.name;
    this.createdBy = game.createdBy;
    this.dateCreated = game.dateCreated;
    this.dateFinished = game.dateFinished;
    this.licensePlates = game.licensePlates;

    this.platesSpotted = Object.keys(game.licensePlates).length;
  }

  public get gameDuration(): number {
    return Math.floor((new Date().getTime() - new Date(this.dateCreated).getTime()) / (1000 * 60 * 60 * 24)) + 1;
  }
}