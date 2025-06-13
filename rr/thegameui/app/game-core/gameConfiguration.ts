import type { Territory, TerritoryModifier } from "./models/Territory";

export const territories: Map<string, Territory> = new Map<string, Territory>([
  // US
  [
    "US-AL",
    {
      shortName: "AL",
      longName: "Alabama",
      country: "US",
      scoreMultiplier: 2,
      borders: new Set<string>(["US-FL", "US-GA", "US-TN", "US-MS"]),
    }
  ],
  [
    "US-AK",
    {
      shortName: "AK",
      longName: "Alaska",
      country: "US",
      scoreMultiplier: 2,
      borders: new Set<string>([]),
    }
  ],
  [
    "US-AZ",
    {
      shortName: "AZ",
      longName: "Arizona",
      country: "US",
      borders: new Set<string>(["US-NM", "US-UT", "US-NV", "US-CA"]),
    }
  ],
  [
    "US-AR",
    {
      shortName: "AR",
      longName: "Arkansas",
      country: "US",
      scoreMultiplier: 3,
      borders: new Set<string>(["US-LA", "US-MS", "US-TN", "US-MO", "US-OK", "US-TX"]),
    }
  ],
  [
    "US-CA",
    {
      shortName: "CA",
      longName: "California",
      country: "US",
      modifier: ["West Coast"],
      borders: new Set<string>(["US-AZ", "US-NV", "US-OR"]),
    }
  ],
  [
    "US-CO",
    {
      shortName: "CO",
      longName: "Colorado",
      country: "US",
      borders: new Set<string>(["US-NM", "US-OK", "US-KS", "US-NE", "US-WY", "US-UT"]),
    }
  ],
  [
    "US-CT",
    {
      shortName: "CT",
      longName: "Connecticut",
      country: "US",
      modifier: ["East Coast"],
      scoreMultiplier: 3,
      borders: new Set<string>(["US-RI", "US-MA", "US-NY"]),
    }
  ],
  [
    "US-DE",
    {
      shortName: "DE",
      longName: "Delaware",
      country: "US",
      modifier: ["East Coast"],
      scoreMultiplier: 3,
      borders: new Set<string>(["US-NJ", "US-PA", "US-MD"]),
    }
  ],
  [
    "US-FL",
    {
      shortName: "FL",
      longName: "Florida",
      country: "US",
      modifier: ["East Coast"],
      borders: new Set<string>(["US-GA", "US-AL"]),
    }
  ],
  [
    "US-GA",
    {
      shortName: "GA",
      longName: "Georgia",
      country: "US",
      modifier: ["East Coast"],
      scoreMultiplier: 2,
      borders: new Set<string>(["US-SC", "US-NC", "US-TN", "US-AL", "US-FL"]),
    }
  ],
  [
    "US-HI",
    {
      shortName: "HI",
      longName: "Hawaii",
      country: "US",
      scoreMultiplier: 4,
      borders: new Set<string>([]),
    }
  ],
  [
    "US-ID",
    {
      shortName: "ID",
      longName: "Idaho",
      country: "US",
      borders: new Set<string>(["US-WA", "US-OR", "US-NV", "US-UT", "US-WY", "US-MT"]),
    }
  ],
  [
    "US-IL",
    {
      shortName: "IL",
      longName: "Illinois",
      country: "US",
      borders: new Set<string>(["US-WI", "US-IA", "US-MO", "US-KY", "US-IN"]),
    }
  ],
  [
    "US-IN",
    {
      shortName: "IN",
      longName: "Indiana",
      country: "US",
      scoreMultiplier: 3,
      borders: new Set<string>(["US-IL", "US-KY", "US-OH", "US-MI"]),
    }
  ],
  [
    "US-IA",
    {
      shortName: "IA",
      longName: "Iowa",
      country: "US",
      scoreMultiplier: 3,
      borders: new Set<string>(["US-MN", "US-SD", "US-NE", "US-MO", "US-IL", "US-WI"]),
    }
  ],
  [
    "US-KS",
    {
      shortName: "KS",
      longName: "Kansas",
      country: "US",
      scoreMultiplier: 3,
      borders: new Set<string>(["US-OK", "US-MO", "US-NE", "US-CO"]),
    }
  ],
  [
    "US-KY",
    {
      shortName: "KY",
      longName: "Kentucky",
      country: "US",
      scoreMultiplier: 3,
      borders: new Set<string>(["US-TN", "US-VA", "US-WV", "US-OH", "US-IN", "US-IL", "US-MO"]),
    }
  ],
  [
    "US-LA",
    {
      shortName: "LA",
      longName: "Louisiana",
      country: "US",
      scoreMultiplier: 3,
      borders: new Set<string>(["US-MS", "US-AR", "US-TX"]),
    }
  ],
  [
    "US-ME",
    {
      shortName: "ME",
      longName: "Maine",
      country: "US",
      modifier: ["East Coast"],
      scoreMultiplier: 5,
      borders: new Set<string>(["US-NH"]),
    }
  ],
  [
    "US-MD",
    {
      shortName: "MD",
      longName: "Maryland",
      country: "US",
      modifier: ["East Coast"],
      scoreMultiplier: 5,
      borders: new Set<string>(["US-DE", "US-PA", "US-WV", "US-VA"]),
    }
  ],
  [
    "US-MA",
    {
      shortName: "MA",
      longName: "Massachusetts",
      country: "US",
      modifier: ["East Coast"],
      scoreMultiplier: 5,
      borders: new Set<string>(["US-NH", "US-VT", "US-NY", "US-CT", "US-RI"]),
    }
  ],
  [
    "US-MI",
    {
      shortName: "MI",
      longName: "Michigan",
      country: "US",
      scoreMultiplier: 2,
      borders: new Set<string>(["US-WI", "US-IN", "US-OH"]),
    }
  ],
  [
    "US-MN",
    {
      shortName: "MN",
      longName: "Minnesota",
      country: "US",
      scoreMultiplier: 2,
      borders: new Set<string>(["US-ND", "US-SD", "US-IA", "US-WI"]),
    }
  ],
  [
    "US-MS",
    {
      shortName: "MS",
      longName: "Mississippi",
      country: "US",
      scoreMultiplier: 2,
      borders: new Set<string>(["US-AL", "US-TN", "US-AR", "US-LA"]),
    }
  ],
  [
    "US-MO",
    {
      shortName: "MO",
      longName: "Missouri",
      country: "US",
      scoreMultiplier: 2,
      borders: new Set<string>(["US-AR", "US-TN", "US-KY", "US-IL", "US-IA", "US-NE", "US-KS", "US-OK"]),
    }
  ],
  [
    "US-MT",
    {
      shortName: "MT",
      longName: "Montana",
      country: "US",
      scoreMultiplier: 2,
      borders: new Set<string>(["US-ID", "US-WY", "US-SD", "US-ND"]),
    }
  ],
  [
    "US-NE",
    {
      shortName: "NE",
      longName: "Nebraska",
      country: "US",
      scoreMultiplier: 2,
      borders: new Set<string>(["US-KS", "US-MO", "US-IA", "US-SD", "US-WY", "US-CO"]),
    }
  ],
  [
    "US-NV",
    {
      shortName: "NV",
      longName: "Nevada",
      country: "US",
      borders: new Set<string>(["US-AZ", "US-UT", "US-ID", "US-OR", "US-CA"]),
    }
  ],
  [
    "US-NH",
    {
      shortName: "NH",
      longName: "New Hampshire",
      country: "US",
      modifier: ["East Coast"],
      scoreMultiplier: 5,
      borders: new Set<string>(["US-VT", "US-MA", "US-ME"]),
    }
  ],
  [
    "US-NJ",
    {
      shortName: "NJ",
      longName: "New Jersey",
      country: "US",
      modifier: ["East Coast"],
      borders: new Set<string>(["US-NY", "US-PA", "US-DE"]),
    }
  ],
  [
    "US-NM",
    {
      shortName: "NM",
      longName: "New Mexico",
      country: "US",
      borders: new Set<string>(["US-TX", "US-OK", "US-CO", "US-AZ"]),
    }
  ],
  [
    "US-NY",
    {
      shortName: "NY",
      longName: "New York",
      country: "US",
      modifier: ["East Coast"],
      borders: new Set<string>(["US-PA", "US-NJ", "US-CT", "US-MA", "US-VT"]),
    }
  ],
  [
    "US-NC",
    {
      shortName: "NC",
      longName: "North Carolina",
      country: "US",
      modifier: ["East Coast"],
      borders: new Set<string>(["US-VA", "US-TN", "US-GA", "US-SC"]),
    }
  ],
  [
    "US-ND",
    {
      shortName: "ND",
      longName: "North Dakota",
      country: "US",
      scoreMultiplier: 3,
      borders: new Set<string>(["US-MT", "US-SD", "US-MN"]),
    }
  ],
  [
    "US-OH",
    {
      shortName: "OH",
      longName: "Ohio",
      country: "US",
      borders: new Set<string>(["US-MI", "US-IN", "US-KY", "US-WV", "US-PA"]),
    }
  ],
  [
    "US-OK",
    {
      shortName: "OK",
      longName: "Oklahoma",
      country: "US",
      borders: new Set<string>(["US-TX", "US-AR", "US-MO", "US-KS", "US-CO", "US-NM"]),
    }
  ],
  [
    "US-OR",
    {
      shortName: "OR",
      longName: "Oregon",
      country: "US",
      modifier: ["West Coast"],
      borders: new Set<string>(["US-CA", "US-NV", "US-ID", "US-WA"]),
    }
  ],
  [
    "US-PA",
    {
      shortName: "PA",
      longName: "Pennsylvania",
      country: "US",
      borders: new Set<string>(["US-OH", "US-WV", "US-MD", "US-DE", "US-NJ", "US-NY"]),
    }
  ],
  [
    "US-RI",
    {
      shortName: "RI",
      longName: "Rhode Island",
      country: "US",
      modifier: ["East Coast"],
      scoreMultiplier: 5,
      borders: new Set<string>(["US-MA", "US-CT"]),
    }
  ],
  [
    "US-SC",
    {
      shortName: "SC",
      longName: "South Carolina",
      country: "US",
      modifier: ["East Coast"],
      scoreMultiplier: 3,
      borders: new Set<string>(["US-NC", "US-GA"]),
    }
  ],
  [
    "US-SD",
    {
      shortName: "SD",
      longName: "South Dakota",
      country: "US",
      scoreMultiplier: 2,
      borders: new Set<string>(["US-NE", "US-IA", "US-MN", "US-ND", "US-MT", "US-WY"]),
    }
  ],
  [
    "US-TN",
    {
      shortName: "TN",
      longName: "Tennessee",
      country: "US",
      scoreMultiplier: 2,
      borders: new Set<string>(["US-AL", "US-GA", "US-NC", "US-VA", "US-KY", "US-MO", "US-AR", "US-MS"]),
    }
  ],
  [
    "US-TX",
    {
      shortName: "TX",
      longName: "Texas",
      country: "US",
      borders: new Set<string>(["US-LA", "US-AR", "US-OK", "US-NM"]),
    }
  ],
  [
    "US-UT",
    {
      shortName: "UT",
      longName: "Utah",
      country: "US",
      borders: new Set<string>(["US-AZ", "US-CO", "US-WY", "US-ID", "US-NV"]),
    }
  ],
  [
    "US-VT",
    {
      shortName: "VT",
      longName: "Vermont",
      country: "US",
      scoreMultiplier: 5,
      borders: new Set<string>(["US-NY", "US-MA", "US-NH"]),
    }
  ],
  [
    "US-VA",
    {
      shortName: "VA",
      longName: "Virginia",
      country: "US",
      modifier: ["East Coast"],
      scoreMultiplier: 4,
      borders: new Set<string>(["US-MD", "US-WV", "US-KY", "US-TN", "US-NC"]),
    }
  ],
  [
    "US-WA",
    {
      shortName: "WA",
      longName: "Washington",
      country: "US",
      modifier: ["West Coast"],
      borders: new Set<string>(["US-OR", "US-ID"]),
    }
  ],
  [
    "US-WV",
    {
      shortName: "WV",
      longName: "West Virginia",
      country: "US",
      scoreMultiplier: 4,
      borders: new Set<string>(["US-VA", "US-MD", "US-PA", "US-OH", "US-KY"]),
    }
  ],
  [
    "US-WI",
    {
      shortName: "WI",
      longName: "Wisconsin",
      country: "US",
      scoreMultiplier: 2,
      borders: new Set<string>(["US-MN", "US-IA", "US-IL", "US-MI"]),
    }
  ],
  [
    "US-WY",
    {
      shortName: "WY",
      longName: "Wyoming",
      country: "US",
      borders: new Set<string>(["US-CO", "US-NE", "US-SD", "US-MT", "US-ID", "US-UT"]),
    }
  ],
  // canada
  [
    "CA-AB",
    {
      shortName: "AB",
      longName: "Alberta",
      country: "CA",
      borders: new Set<string>([])
    }
  ],
  [
    "CA-BC",
    {
      shortName: "BC",
      longName: "British Columbia",
      country: "CA",
      borders: new Set<string>([])
    }
  ],
  [
    "CA-MB",
    {
      shortName: "MB",
      longName: "Manitoba",
      country: "CA",
      borders: new Set<string>([])
    }
  ],
  [
    "CA-NB",
    {
      shortName: "NB",
      longName: "New Brunswick",
      country: "CA",
      borders: new Set<string>([])
    }
  ],
  [
    "CA-NL",
    {
      shortName: "NL",
      longName: "Newfoundland and Labrador",
      country: "CA",
      borders: new Set<string>([])
    }
  ],
  [
    "CA-NT",
    {
      shortName: "NT",
      longName: "Northwest Territories",
      country: "CA",
      borders: new Set<string>([])
    }
  ],
  [
    "CA-NS",
    {
      shortName: "NS",
      longName: "Nova Scotia",
      country: "CA",
      borders: new Set<string>([])
    }
  ],
  [
    "CA-NU",
    {
      shortName: "NU",
      longName: "Nunavut",
      country: "CA",
      borders: new Set<string>([])
    }
  ],
  [
    "CA-ON",
    {
      shortName: "ON",
      longName: "Ontario",
      country: "CA",
      borders: new Set<string>([])
    }
  ],
  [
    "CA-PE",
    {
      shortName: "PE",
      longName: "Prince Edward Island",
      country: "CA",
      borders: new Set<string>([])
    }
  ],
  [
    "CA-QC",
    {
      shortName: "QC",
      longName: "Quebec",
      country: "CA",
      borders: new Set<string>([])
    }
  ],
  [
    "CA-SK",
    {
      shortName: "SK",
      longName: "Saskatchewan",
      country: "CA",
      borders: new Set<string>([])
    }
  ],
  [
    "CA-YT",
    {
      shortName: "YT",
      longName: "Yukon",
      country: "CA",
      borders: new Set<string>([])
    }
  ]
]);

export const territoriesByModifiers = Array.from(territories.entries())
  .filter(([_, ter]) => (ter.modifier ?? []).length > 0)
  .reduce((map, [key, ter]) => {
    for (const modifier of ter.modifier!) {
      const territoriesWithThisModifier = map.get(modifier) ?? new Set<TerritoryModifier>();
      territoriesWithThisModifier.add(key);
      map.set(modifier, territoriesWithThisModifier);
    }

    return map;
  }, new Map<TerritoryModifier, Set<string>>());

export const westCoastStates = territoriesByModifiers.get("East Coast") ?? new Set<string>();

export const eastCoastStates = territoriesByModifiers.get("West Coast") ?? new Set<string>();