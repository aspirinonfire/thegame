import type { ScoreMilestone } from "./models/ScoreData";
import type { Territory, TerritoryModifier } from "./models/Territory";

export const territories: Territory[] = [
  // US
  {
    key: "US-AL",
    shortName: "AL",
    longName: "Alabama",
    country: "US",
    scoreMultiplier: 2,
    borders: new Set<string>(["US-FL", "US-GA", "US-TN", "US-MS"]),
    modifier: ["South"]
  },
  {
    key: "US-AK",
    shortName: "AK",
    longName: "Alaska",
    country: "US",
    scoreMultiplier: 2,
    borders: new Set<string>([]),
  },
  {
    key: "US-AZ",
    shortName: "AZ",
    longName: "Arizona",
    country: "US",
    borders: new Set<string>(["US-NM", "US-UT", "US-NV", "US-CA"]),
    modifier: ["Southwest"]
  },
  {
    key: "US-AR",
    shortName: "AR",
    longName: "Arkansas",
    country: "US",
    scoreMultiplier: 3,
    borders: new Set<string>(["US-LA", "US-MS", "US-TN", "US-MO", "US-OK", "US-TX"]),
    modifier: ["South"]
  },
  {
    key: "US-CA",
    shortName: "CA",
    longName: "California",
    country: "US",
    modifier: ["West Coast"],
    borders: new Set<string>(["US-AZ", "US-NV", "US-OR"]),
  },
  {
    key: "US-CO",
    shortName: "CO",
    longName: "Colorado",
    country: "US",
    borders: new Set<string>(["US-NM", "US-OK", "US-KS", "US-NE", "US-WY", "US-UT"]),
  },
  {
    key: "US-CT",
    shortName: "CT",
    longName: "Connecticut",
    country: "US",
    modifier: ["East Coast"],
    scoreMultiplier: 3,
    borders: new Set<string>(["US-RI", "US-MA", "US-NY"]),
  },
  {
    key: "US-DE",
    shortName: "DE",
    longName: "Delaware",
    country: "US",
    modifier: ["East Coast"],
    scoreMultiplier: 3,
    borders: new Set<string>(["US-NJ", "US-PA", "US-MD"]),
  },
  {
    key: "US-FL",
    shortName: "FL",
    longName: "Florida",
    country: "US",
    modifier: ["East Coast", "South"],
    borders: new Set<string>(["US-GA", "US-AL"]),
  },
  {
    key: "US-GA",
    shortName: "GA",
    longName: "Georgia",
    country: "US",
    modifier: ["East Coast", "South"],
    scoreMultiplier: 2,
    borders: new Set<string>(["US-SC", "US-NC", "US-TN", "US-AL", "US-FL"])
  },
  {
    key: "US-HI",
    shortName: "HI",
    longName: "Hawaii",
    country: "US",
    scoreMultiplier: 4,
    borders: new Set<string>([]),
  },
  {
    key: "US-ID",
    shortName: "ID",
    longName: "Idaho",
    country: "US",
    borders: new Set<string>(["US-WA", "US-OR", "US-NV", "US-UT", "US-WY", "US-MT"]),
  },
  {
    key: "US-IL",
    shortName: "IL",
    longName: "Illinois",
    country: "US",
    borders: new Set<string>(["US-WI", "US-IA", "US-MO", "US-KY", "US-IN"]),
    modifier: ["Midwest"]
  },
  {
    key: "US-IN",
    shortName: "IN",
    longName: "Indiana",
    country: "US",
    scoreMultiplier: 3,
    borders: new Set<string>(["US-IL", "US-KY", "US-OH", "US-MI"]),
    modifier: ["Midwest"]
  },
  {
    key: "US-IA",
    shortName: "IA",
    longName: "Iowa",
    country: "US",
    scoreMultiplier: 3,
    borders: new Set<string>(["US-MN", "US-SD", "US-NE", "US-MO", "US-IL", "US-WI"]),
    modifier: ["Midwest"]
  },
  {
    key: "US-KS",
    shortName: "KS",
    longName: "Kansas",
    country: "US",
    scoreMultiplier: 3,
    borders: new Set<string>(["US-OK", "US-MO", "US-NE", "US-CO"]),
    modifier: ["Midwest"]
  },
  {
    key: "US-KY",
    shortName: "KY",
    longName: "Kentucky",
    country: "US",
    scoreMultiplier: 3,
    borders: new Set<string>(["US-TN", "US-VA", "US-WV", "US-OH", "US-IN", "US-IL", "US-MO"]),
    modifier: ["South"]
  },
  {
    key: "US-LA",
    shortName: "LA",
    longName: "Louisiana",
    country: "US",
    scoreMultiplier: 3,
    borders: new Set<string>(["US-MS", "US-AR", "US-TX"]),
    modifier: ["South"]
  },
  {
    key: "US-ME",
    shortName: "ME",
    longName: "Maine",
    country: "US",
    modifier: ["East Coast"],
    scoreMultiplier: 5,
    borders: new Set<string>(["US-NH"]),
  },
  {
    key: "US-MD",
    shortName: "MD",
    longName: "Maryland",
    country: "US",
    modifier: ["East Coast"],
    scoreMultiplier: 5,
    borders: new Set<string>(["US-DE", "US-PA", "US-WV", "US-VA"]),
  },
  {
    key: "US-MA",
    shortName: "MA",
    longName: "Massachusetts",
    country: "US",
    modifier: ["East Coast"],
    scoreMultiplier: 5,
    borders: new Set<string>(["US-NH", "US-VT", "US-NY", "US-CT", "US-RI"]),
  },
  {
    key: "US-MI",
    shortName: "MI",
    longName: "Michigan",
    country: "US",
    scoreMultiplier: 2,
    borders: new Set<string>(["US-WI", "US-IN", "US-OH"]),
    modifier: ["Midwest"]
  },
  {
    key: "US-MN",
    shortName: "MN",
    longName: "Minnesota",
    country: "US",
    scoreMultiplier: 2,
    borders: new Set<string>(["US-ND", "US-SD", "US-IA", "US-WI"]),
    modifier: ["Midwest"]
  },
  {
    key: "US-MS",
    shortName: "MS",
    longName: "Mississippi",
    country: "US",
    scoreMultiplier: 2,
    borders: new Set<string>(["US-AL", "US-TN", "US-AR", "US-LA"]),
    modifier: ["South"]
  },
  {
    key: "US-MO",
    shortName: "MO",
    longName: "Missouri",
    country: "US",
    scoreMultiplier: 2,
    borders: new Set<string>(["US-AR", "US-TN", "US-KY", "US-IL", "US-IA", "US-NE", "US-KS", "US-OK"]),
    modifier: ["Midwest"]
  },
  {
    key: "US-MT",
    shortName: "MT",
    longName: "Montana",
    country: "US",
    scoreMultiplier: 2,
    borders: new Set<string>(["US-ID", "US-WY", "US-SD", "US-ND"]),
  },
  {
    key: "US-NE",
    shortName: "NE",
    longName: "Nebraska",
    country: "US",
    scoreMultiplier: 2,
    borders: new Set<string>(["US-KS", "US-MO", "US-IA", "US-SD", "US-WY", "US-CO"]),
    modifier: ["Midwest"]
  },
  {
    key: "US-NV",
    shortName: "NV",
    longName: "Nevada",
    country: "US",
    borders: new Set<string>(["US-AZ", "US-UT", "US-ID", "US-OR", "US-CA"]),
  },
  {
    key: "US-NH",
    shortName: "NH",
    longName: "New Hampshire",
    country: "US",
    modifier: ["East Coast"],
    scoreMultiplier: 5,
    borders: new Set<string>(["US-VT", "US-MA", "US-ME"]),
  },
  {
    key: "US-NJ",
    shortName: "NJ",
    longName: "New Jersey",
    country: "US",
    modifier: ["East Coast"],
    borders: new Set<string>(["US-NY", "US-PA", "US-DE"]),
  },
  {
    key: "US-NM",
    shortName: "NM",
    longName: "New Mexico",
    country: "US",
    borders: new Set<string>(["US-TX", "US-OK", "US-CO", "US-AZ"]),
    modifier: ["Southwest"]
  },
  {
    key: "US-NY",
    shortName: "NY",
    longName: "New York",
    country: "US",
    modifier: ["East Coast"],
    borders: new Set<string>(["US-PA", "US-NJ", "US-CT", "US-MA", "US-VT"]),
  },
  {
    key: "US-NC",
    shortName: "NC",
    longName: "North Carolina",
    country: "US",
    modifier: ["East Coast", "South"],
    borders: new Set<string>(["US-VA", "US-TN", "US-GA", "US-SC"]),
  },
  {
    key: "US-ND",
    shortName: "ND",
    longName: "North Dakota",
    country: "US",
    scoreMultiplier: 3,
    borders: new Set<string>(["US-MT", "US-SD", "US-MN"]),
    modifier: ["Midwest"]
  },
  {
    key: "US-OH",
    shortName: "OH",
    longName: "Ohio",
    country: "US",
    borders: new Set<string>(["US-MI", "US-IN", "US-KY", "US-WV", "US-PA"]),
    modifier: ["Midwest"]
  },
  {
    key: "US-OK",
    shortName: "OK",
    longName: "Oklahoma",
    country: "US",
    borders: new Set<string>(["US-TX", "US-AR", "US-MO", "US-KS", "US-CO", "US-NM"]),
    modifier: ["Southwest", "South"]
  },
  {
    key: "US-OR",
    shortName: "OR",
    longName: "Oregon",
    country: "US",
    modifier: ["West Coast"],
    borders: new Set<string>(["US-CA", "US-NV", "US-ID", "US-WA"]),
  },
  {
    key: "US-PA",
    shortName: "PA",
    longName: "Pennsylvania",
    country: "US",
    borders: new Set<string>(["US-OH", "US-WV", "US-MD", "US-DE", "US-NJ", "US-NY"]),
  },
  {
    key: "US-RI",
    shortName: "RI",
    longName: "Rhode Island",
    country: "US",
    modifier: ["East Coast"],
    scoreMultiplier: 5,
    borders: new Set<string>(["US-MA", "US-CT"]),
  },
  {
    key: "US-SC",
    shortName: "SC",
    longName: "South Carolina",
    country: "US",
    modifier: ["East Coast", "South"],
    scoreMultiplier: 3,
    borders: new Set<string>(["US-NC", "US-GA"]),
  },
  {
    key: "US-SD",
    shortName: "SD",
    longName: "South Dakota",
    country: "US",
    scoreMultiplier: 2,
    borders: new Set<string>(["US-NE", "US-IA", "US-MN", "US-ND", "US-MT", "US-WY"]),
    modifier: ["Midwest"]
  },
  {
    key: "US-TN",
    shortName: "TN",
    longName: "Tennessee",
    country: "US",
    scoreMultiplier: 2,
    borders: new Set<string>(["US-AL", "US-GA", "US-NC", "US-VA", "US-KY", "US-MO", "US-AR", "US-MS"]),
    modifier: ["South"]
  },
  {
    key: "US-TX",
    shortName: "TX",
    longName: "Texas",
    country: "US",
    borders: new Set<string>(["US-LA", "US-AR", "US-OK", "US-NM"]),
    modifier: ["Southwest", "South"]
  },
  {
    key: "US-UT",
    shortName: "UT",
    longName: "Utah",
    country: "US",
    borders: new Set<string>(["US-AZ", "US-CO", "US-WY", "US-ID", "US-NV"]),
  },
  {
    key: "US-VT",
    shortName: "VT",
    longName: "Vermont",
    country: "US",
    scoreMultiplier: 5,
    borders: new Set<string>(["US-NY", "US-MA", "US-NH"]),
  },
  {
    key: "US-VA",
    shortName: "VA",
    longName: "Virginia",
    country: "US",
    modifier: ["East Coast"],
    scoreMultiplier: 4,
    borders: new Set<string>(["US-MD", "US-WV", "US-KY", "US-TN", "US-NC"]),
  },
  {
    key: "US-WA",
    shortName: "WA",
    longName: "Washington",
    country: "US",
    modifier: ["West Coast"],
    borders: new Set<string>(["US-OR", "US-ID"]),
  },
  {
    key: "US-WV",
    shortName: "WV",
    longName: "West Virginia",
    country: "US",
    scoreMultiplier: 4,
    borders: new Set<string>(["US-VA", "US-MD", "US-PA", "US-OH", "US-KY"]),
  },
  {
    key: "US-WI",
    shortName: "WI",
    longName: "Wisconsin",
    country: "US",
    scoreMultiplier: 2,
    borders: new Set<string>(["US-MN", "US-IA", "US-IL", "US-MI"]),
    modifier: ["Midwest"]
  },
  {
    key: "US-WY",
    shortName: "WY",
    longName: "Wyoming",
    country: "US",
    borders: new Set<string>(["US-CO", "US-NE", "US-SD", "US-MT", "US-ID", "US-UT"]),
  },
  // canada
  {
    key: "CA-AB",
    shortName: "AB",
    longName: "Alberta",
    country: "CA",
    borders: new Set<string>([])
  },
  {
    key: "CA-BC",
    shortName: "BC",
    longName: "British Columbia",
    country: "CA",
    borders: new Set<string>([])
  },
  {
    key: "CA-MB",
    shortName: "MB",
    longName: "Manitoba",
    country: "CA",
    borders: new Set<string>([])
  },
  {
    key: "CA-NB",
    shortName: "NB",
    longName: "New Brunswick",
    country: "CA",
    borders: new Set<string>([])
  },
  {
    key: "CA-NL",
    shortName: "NL",
    longName: "Newfoundland and Labrador",
    country: "CA",
    borders: new Set<string>([])
  },
  {
    key: "CA-NT",
    shortName: "NT",
    longName: "Northwest Territories",
    country: "CA",
    borders: new Set<string>([])
  },
  {
    key: "CA-NS",
    shortName: "NS",
    longName: "Nova Scotia",
    country: "CA",
    borders: new Set<string>([])
  },
  {
    key: "CA-NU",
    shortName: "NU",
    longName: "Nunavut",
    country: "CA",
    borders: new Set<string>([])
  },
  {
    key: "CA-ON",
    shortName: "ON",
    longName: "Ontario",
    country: "CA",
    borders: new Set<string>([])
  },
  {
    key: "CA-PE",
    shortName: "PE",
    longName: "Prince Edward Island",
    country: "CA",
    borders: new Set<string>([])
  },
  {
    key: "CA-QC",
    shortName: "QC",
    longName: "Quebec",
    country: "CA",
    borders: new Set<string>([])
  },
  {
    key: "CA-SK",
    shortName: "SK",
    longName: "Saskatchewan",
    country: "CA",
    borders: new Set<string>([])
  },
  {
    key: "CA-YT",
    shortName: "YT",
    longName: "Yukon",
    country: "CA",
    borders: new Set<string>([])
  }
];

export const territoryModifierScoreLookup = new Map<ScoreMilestone, number>([
  ["West Coast", 10],
  ["East Coast", 50],
  ["Midwest", 30],
  ["South", 30],
  ["Southwest", 10],
  ["Coast-to-Coast", 100],
  ["Globetrotter", 1000]
]);
  