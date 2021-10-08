export type Country = 'US' | 'CA' | 'MX'

export interface Territory {
  shortName: string,
  longName: string,
  country: Country,
  licensePlateImgs: string[]
}