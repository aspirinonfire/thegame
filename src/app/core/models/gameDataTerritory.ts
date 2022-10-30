export type Country = 'US' | 'CA' | 'MX'

export type TerritoryModifier = 'West Coast' | 'East Coast';

export interface Territory {
  shortName: string,
  longName: string,
  country: Country,
  licensePlateImgs: string[],
  modifier?: TerritoryModifier[] 
}

export interface StateBorder
{
  [key: string] : Border
}

export interface Border
{
  [Key: string]: boolean
}