import { StateBorder, Territory } from "../gameCore/gameDataTerritory";

export const mockGameData: Territory[] = [
  // us
  {
    shortName: 'AL', longName: "Alabama", country: 'US', licensePlateImgs: [], scoreMultiplier: 2,
  },
  {
    shortName: 'AK', longName: "Alaska", country: 'US', licensePlateImgs: [], scoreMultiplier: 2,
  },
  {
    shortName: 'AZ', longName: "Arizona", country: 'US', licensePlateImgs: []
  },
  {
    shortName: 'AR', longName: "Arkansas", country: 'US', licensePlateImgs: [], scoreMultiplier: 3,
  },
  {
    shortName: 'CA', longName: "California", country: 'US', licensePlateImgs: [], modifier: ['West Coast']
  },
  {
    shortName: 'CO', longName: "Colorado", country: 'US', licensePlateImgs: []
  },
  {
    shortName: 'CT', longName: "Connecticut", country: 'US', licensePlateImgs: [], modifier: ['East Coast'], scoreMultiplier: 3,
  },
  {
    shortName: 'DE', longName: "Delaware", country: 'US', licensePlateImgs: [], modifier: ['East Coast'], scoreMultiplier: 3,
  },
  {
    shortName: 'DC', longName: "District of Columbia", country: 'US', licensePlateImgs: [], scoreMultiplier: 5,
  },
  {
    shortName: 'FL', longName: "Florida", country: 'US', licensePlateImgs: [], modifier: ['East Coast']
  },
  {
    shortName: 'GA', longName: "Georgia", country: 'US', licensePlateImgs: [], modifier: ['East Coast'], scoreMultiplier: 2,
  },
  {
    shortName: 'HI', longName: "Hawaii", country: 'US', licensePlateImgs: [], scoreMultiplier: 4,
  },
  {
    shortName: 'ID', longName: "Idaho", country: 'US', licensePlateImgs: []
  },
  {
    shortName: 'IL', longName: "Illinois", country: 'US', licensePlateImgs: []
  },
  {
    shortName: 'IN', longName: "Indiana", country: 'US', licensePlateImgs: [], scoreMultiplier: 3,
  },
  {
    shortName: 'IA', longName: "Iowa", country: 'US', licensePlateImgs: [], scoreMultiplier: 3,
  },
  {
    shortName: 'KS', longName: "Kansas", country: 'US', licensePlateImgs: [], scoreMultiplier: 3,
  },
  {
    shortName: 'KY', longName: "Kentucky", country: 'US', licensePlateImgs: [], scoreMultiplier: 3,
  },
  {
    shortName: 'LA', longName: "Louisiana", country: 'US', licensePlateImgs: [], scoreMultiplier: 3,
  },
  {
    shortName: 'ME', longName: "Maine", country: 'US', licensePlateImgs: [], modifier: ['East Coast'], scoreMultiplier: 5,
  },
  {
    shortName: 'MD', longName: "Maryland", country: 'US', licensePlateImgs: [], modifier: ['East Coast'], scoreMultiplier: 5,
  },
  {
    shortName: 'MA', longName: "Massachusetts", country: 'US', licensePlateImgs: [], modifier: ['East Coast'], scoreMultiplier: 5,
  },
  {
    shortName: 'MI', longName: "Michigan", country: 'US', licensePlateImgs: [], scoreMultiplier: 2,
  },
  {
    shortName: 'MN', longName: "Minnesota", country: 'US', licensePlateImgs: [], scoreMultiplier: 2,
  },
  {
    shortName: 'MS', longName: "Mississippi", country: 'US', licensePlateImgs: [], scoreMultiplier: 2,
  },
  {
    shortName: 'MO', longName: "Missouri", country: 'US', licensePlateImgs: [], scoreMultiplier: 2,
  },
  {
    shortName: 'MT', longName: "Montana", country: 'US', licensePlateImgs: [], scoreMultiplier: 2,
  },
  {
    shortName: 'NE', longName: "Nebraska", country: 'US', licensePlateImgs: [], scoreMultiplier: 2,
  },
  {
    shortName: 'NV', longName: "Nevada", country: 'US', licensePlateImgs: []
  },
  {
    shortName: 'NH', longName: "New Hampshire", country: 'US', licensePlateImgs: [], modifier: ['East Coast'], scoreMultiplier: 5,
  },
  {
    shortName: 'NJ', longName: "New Jersey", country: 'US', licensePlateImgs: [], modifier: ['East Coast']
  },
  {
    shortName: 'NM', longName: "New Mexico", country: 'US', licensePlateImgs: []
  },
  {
    shortName: 'NY', longName: "New York", country: 'US', licensePlateImgs: [], modifier: ['East Coast']
  },
  {
    shortName: 'NC', longName: "North Carolina", country: 'US', licensePlateImgs: [], modifier: ['East Coast']
  },
  {
    shortName: 'ND', longName: "North Dakota", country: 'US', licensePlateImgs: [], scoreMultiplier: 3,
  },
  {
    shortName: 'OH', longName: "Ohio", country: 'US', licensePlateImgs: []
  },
  {
    shortName: 'OK', longName: "Oklahoma", country: 'US', licensePlateImgs: []
  },
  {
    shortName: 'OR', longName: "Oregon", country: 'US', licensePlateImgs: [], modifier: ['West Coast']
  },
  {
    shortName: 'PA', longName: "Pennsylvania", country: 'US', licensePlateImgs: []
  },
  {
    shortName: 'RI', longName: "Rhode Island", country: 'US', licensePlateImgs: [], modifier: ['East Coast'], scoreMultiplier: 5,
  },
  {
    shortName: 'SC', longName: "South Carolina", country: 'US', licensePlateImgs: [], modifier: ['East Coast'], scoreMultiplier: 3,
  },
  {
    shortName: 'SD', longName: "South Dakota", country: 'US', licensePlateImgs: [], scoreMultiplier: 2,
  },
  {
    shortName: 'TN', longName: "Tennessee", country: 'US', licensePlateImgs: [], scoreMultiplier: 2,
  },
  {
    shortName: 'TX', longName: "Texas", country: 'US', licensePlateImgs: []
  },
  {
    shortName: 'UT', longName: "Utah", country: 'US', licensePlateImgs: []
  },
  {
    shortName: 'VT', longName: "Vermont", country: 'US', licensePlateImgs: [], scoreMultiplier: 5,
  },
  {
    shortName: 'VA', longName: "Virginia", country: 'US', licensePlateImgs: [], modifier: ['East Coast'], scoreMultiplier: 4,
  },
  {
    shortName: 'WA', longName: "Washington", country: 'US', licensePlateImgs: [], modifier: ['West Coast']
  },
  {
    shortName: 'WV', longName: "West Virginia", country: 'US', licensePlateImgs: [], scoreMultiplier: 4,
  },
  {
    shortName: 'WI', longName: "Wisconsin", country: 'US', licensePlateImgs: [], scoreMultiplier: 2,
  },
  {
    shortName: 'WY', longName: "Wyoming", country: 'US', licensePlateImgs: []
  },
  // canada
  {
    shortName: 'AB', longName: 'Alberta',  country: 'CA', licensePlateImgs: []
  },
  {
    shortName: 'BC', longName: "British Columbia",  country: 'CA', licensePlateImgs: []
  },
  {
    shortName: 'MB', longName: "Manitoba",  country: 'CA', licensePlateImgs: []
  },
  {
    shortName: 'NB', longName: "New Brunswick",  country: 'CA', licensePlateImgs: []
  },
  {
    shortName: 'NL', longName: "Newfoundland and Labrador",  country: 'CA', licensePlateImgs: []
  },
  {
    shortName: 'NT', longName: "Northwest Territories",  country: 'CA', licensePlateImgs: []
  },
  {
    shortName: 'NS', longName: "Nova Scotia",  country: 'CA', licensePlateImgs: []
  },
  {
    shortName: 'NU', longName: "Nunavut",  country: 'CA', licensePlateImgs: []
  },
  {
    shortName: 'ON', longName: "Ontario",  country: 'CA', licensePlateImgs: []
  },
  {
    shortName: 'PE', longName: "Prince Edward Island",  country: 'CA', licensePlateImgs: []
  },
  {
    shortName: 'QC', longName: "Quebec",  country: 'CA', licensePlateImgs: []
  },
  {
    shortName: 'SK', longName: "Saskatchewan",  country: 'CA', licensePlateImgs: []
  },
  {
    shortName: 'YT', longName: "Yukon",  country: 'CA', licensePlateImgs: []
  }
];
// 
export const UsStateBorders: StateBorder  = {
  "AL": {"FL": true, "GA": true, "TN": true, "MS": true },
  "AK": { },
  "AZ": { "NM": true, "UT": true, "NV": true, "CA": true },
  "AR": { "LA": true, "MS": true, "TN": true, "MO": true, "OK": true, "TX": true },
  "CA": { "AZ": true, "NV": true, "OR": true },
  "CO": { "NM": true, "OK": true, "KS": true, "NE": true, "WY": true, "UT": true },
  "CT": { "RI": true, "MA": true, "NY": true },
  "DE": { "NJ": true, "PA": true, "MD": true },
  "FL": { "GA": true, "AL": true },
  "GA": { "SC": true, "NC": true, "TN": true, "AL": true, "FL": true },
  "HI": { },
  "ID": { "WA": true, "OR": true, "NV": true, "UT": true, "WY": true, "MT": true },
  "IL": { "WI": true, "IA": true, "MO": true, "KY": true, "IN": true },
  "IN": { "IL": true, "KY": true, "OH": true, "MI": true },
  "IA": { "MN": true, "SD": true, "NE": true, "MO": true, "IL": true, "WI": true },
  "KS": { "OK": true, "MO": true, "NE": true, "CO": true },
  "KY": { "TN": true, "VA": true, "WV": true, "OH": true, "IN": true, "IL": true, "MO": true },
  "LA": { "MS": true, "AR": true, "TX": true },
 
  "ME": { "NH": true },
  "MD": { "DE": true, "PA": true, "WV": true, "VA": true  },
  "MA": { "NH": true, "VT": true, "NY": true, "CT": true, "RI": true },
  "MI": { "WI": true, "IN": true, "OH": true },
  "MN": { "ND": true, "SD": true, "IA": true, "WI": true },
  "MS": { "AL": true, "TN": true, "AR": true, "LA": true },
  "MO": { "AR": true, "TN": true, "KY": true, "IL": true, "IA": true, "NE": true, "KS": true, "OK": true },
  "MT": { "ID": true, "WY": true, "SD": true, "ND": true },
 
  "NE": { "KS": true, "MO": true, "IA": true, "SD": true, "WY": true, "CO": true },
  "NV": { "AZ": true, "UT": true, "ID": true, "OR": true, "CA": true },
  "NH": { "VT": true, "MA": true, "ME": true },
  "NJ": { "NY": true, "PA": true, "DE": true },
  "NM": { "TX": true, "OK": true, "CO": true, "AZ": true },
  "NY": { "PA": true, "NJ": true, "CT": true, "MA": true, "VT": true },
  "NC": { "VA": true, "TN": true, "GA": true, "SC": true },
  "ND": { "MT": true, "SD": true, "MN": true },
  "OH": { "MI": true, "IN": true, "KY": true, "WV": true, "PA": true },
  "OK": { "TX": true, "AR": true, "MO": true, "KS": true, "CO": true, "NM": true },
  "OR": { "CA": true, "NV": true, "ID": true, "WA": true },
  "PA": { "OH": true, "WV": true, "MD": true, "DE": true, "NJ": true, "NY": true },
  "RI": { "MA": true, "CT": true },
  "SC": { "NC": true, "GA": true },
  "SD": { "NE": true, "IA": true, "MN": true, "ND": true, "MT": true, "WY": true },
  "TN": { "AL": true, "GA": true, "NC": true, "VA": true, "KY": true, "MO": true, "AR": true, "MS": true },
  "TX": { "LA": true, "AR": true, "OK": true, "NM": true },
  "UT": { "AZ": true, "CO": true, "WY": true, "ID": true, "NV": true },
  "VT": { "NY": true, "MA": true, "NH": true },
  "VA": { "MD": true, "WV": true, "KY": true, "TN": true, "NC": true },
  "WA": { "OR": true, "ID": true },
  "WV": { "VA": true, "MD": true, "PA": true, "OH": true, "KY": true },
  "WI": { "MN": true, "IA": true, "IL": true, "MI": true },
  "WY": { "CO": true, "NE": true, "SD": true, "MT": true, "ID": true, "UT": true}
 };