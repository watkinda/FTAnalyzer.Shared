﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using FTAnalyzer.Properties;
using FTAnalyzer.Utilities;
#if !__MACOS__
using System.Web.UI.WebControls;
using FTAnalyzer.Mapping;
#endif
namespace FTAnalyzer
{
  public class FactLocation : IComparable<FactLocation>, IComparable, IDisplayLocation, IDisplayGeocodedLocation
  {
    #region Variables

    public const int UNKNOWN = -1, COUNTRY = 0, REGION = 1, SUBREGION = 2, ADDRESS = 3, PLACE = 4;
    public enum Geocode
    {
      UNKNOWN = -1, NOT_SEARCHED = 0, MATCHED = 1, PARTIAL_MATCH = 2, GEDCOM_USER = 3, NO_MATCH = 4,
      INCORRECT = 5, OUT_OF_BOUNDS = 6, LEVEL_MISMATCH = 7, OS_50KMATCH = 8, OS_50KPARTIAL = 9, OS_50KFUZZY = 10
    };

    public string OriginalText { get; private set; }
    public string GEDCOMLocation => FTAnalyzerCreated ? string.Empty : OriginalText;
    public string Country { get; set; }
    public string Region { get; set; }
    public string SubRegion { get; set; }
    public string Address { get; private set; }
    public string Place { get; private set; }
    public string CountryMetaphone => DoubleMetaphone.GetPrimaryMetaphone(Country);
    public string RegionMetaphone => DoubleMetaphone.GetPrimaryMetaphone(Region);
    public string SubRegionMetaphone => DoubleMetaphone.GetPrimaryMetaphone(SubRegion);
    public string AddressMetaphone => DoubleMetaphone.GetPrimaryMetaphone(Address);
    public string PlaceMetaphone => DoubleMetaphone.GetPrimaryMetaphone(Place);
    public string AddressNoNumerics 
    { 
      get
      {
        return FixNumerics(Address ?? string.Empty, false);
      }
    }

    public string PlaceNoNumerics 
    {
      get
      {
        return FixNumerics(Place ?? string.Empty, false);
      }
    }

    public string FuzzyMatch => $"{AddressMetaphone}:{SubRegionMetaphone}:{RegionMetaphone}:{CountryMetaphone}";
    public string FuzzyNoParishMatch => $"{AddressMetaphone}:{RegionMetaphone}:{CountryMetaphone}";
    public string ParishID { get; internal set; }
    public int Level { get; private set; }
    public Region KnownRegion { get; private set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double LatitudeM { get; set; }
    public double LongitudeM { get; set; }
    public Geocode GeocodeStatus { get; set; }
    public string FoundLocation { get; set; }
    public string FoundResultType { get; set; }
    public int FoundLevel { get; set; }
    public double PixelSize { get; set; }
    public string GEDCOMLoaded => FTAnalyzerCreated ? "No" : "Yes";
    public bool GEDCOMLatLong { get; set; }
    public bool FTAnalyzerCreated { get; set; }
#if __PC__
    public GeoResponse.CResult.CGeometry.CViewPort ViewPort { get; set; }
#endif
    readonly Dictionary<string, Individual> individuals;

    static Dictionary<string, string> COUNTRY_TYPOS = new Dictionary<string, string>();
    static Dictionary<string, string> REGION_TYPOS = new Dictionary<string, string>();
    static Dictionary<string, string> REGION_SHIFTS = new Dictionary<string, string>();
    static Dictionary<string, string> FREECEN_LOOKUP = new Dictionary<string, string>();
    static Dictionary<string, Tuple<string, string>> FINDMYPAST_LOOKUP = new Dictionary<string, Tuple<string, string>>();
    static IDictionary<string, FactLocation> LOCATIONS;
    static Dictionary<Tuple<int, string>, string> GOOGLE_FIXES = new Dictionary<Tuple<int, string>, string>();
    static Dictionary<Tuple<int, string>, string> LOCAL_GOOGLE_FIXES;

    static Dictionary<string, string> COUNTRY_SHIFTS = new Dictionary<string, string>();
    static Dictionary<string, string> CITY_ADD_COUNTRY = new Dictionary<string, string>();

    public static readonly IDictionary<Geocode, string> Geocodes = new Dictionary<Geocode, string>
    {
        { Geocode.UNKNOWN, "Unknown" },
        { Geocode.NOT_SEARCHED, "Not Searched" },
        { Geocode.GEDCOM_USER, "GEDCOM/User Data" },
        { Geocode.PARTIAL_MATCH, "Partial Match (Google)" },
        { Geocode.MATCHED, "Google Matched" },
        { Geocode.NO_MATCH, "No Match" },
        { Geocode.INCORRECT, "Incorrect (User Marked)" },
        { Geocode.OUT_OF_BOUNDS, "Outside Country Area" },
        { Geocode.LEVEL_MISMATCH, "Partial Match (Levels)" },
        { Geocode.OS_50KMATCH, "OS Gazetteer Match" },
        { Geocode.OS_50KPARTIAL, "Partial Match (Ord Surv)" },
        { Geocode.OS_50KFUZZY, "Fuzzy Match (Ord Surv)" }
    };

    public static readonly FactLocation BLANK_LOCATION = new FactLocation("Unknown", "0.0", "0.0", Geocode.GEDCOM_USER);
    public static readonly FactLocation UNKNOWN_LOCATION = new FactLocation(string.Empty, "0.0", "0.0", Geocode.UNKNOWN);
    public static readonly FactLocation TEMP = new FactLocation();

    #endregion

    #region Static Constructor
    static FactLocation()
    {
      ResetLocations();
    }

    public static void LoadConversions(string startPath)
    {
      // load conversions from XML file
      #region Fact Location Fixes
      if (startPath is null) return;
#if __MACOS__
            string filename = Path.Combine(startPath, @"../Resources/FactLocationFixes.xml");
#elif __IOS__
            string filename = Path.Combine(startPath, @"Resources/FactLocationFixes.xml");
#else
      string filename = Path.Combine(startPath, @"Resources\FactLocationFixes.xml");
#endif
      Console.WriteLine($"Loading factlocation fixes from: {filename}");
      if (File.Exists(filename) && !GeneralSettings.Default.SkipFixingLocations) // don't load file if skipping fixing locations
      {
        XmlDocument xmlDoc = new XmlDocument() { XmlResolver = null };
        string xml = File.ReadAllText(filename);
        StringReader sreader = new StringReader(xml);
        using (XmlReader reader = XmlReader.Create(sreader, new XmlReaderSettings() { XmlResolver = null }))
          xmlDoc.Load(reader);
        foreach (XmlNode n in xmlDoc.SelectNodes("Data/Fixes/CountryTypos/CountryTypo"))
        {
          string from = n.Attributes["from"].Value;
          string to = n.Attributes["to"].Value;
          if (COUNTRY_TYPOS.ContainsKey(from))
            Console.WriteLine(string.Format("Error duplicate country typos :{0}", from));
          if (!string.IsNullOrEmpty(from) && !string.IsNullOrEmpty(to))
            COUNTRY_TYPOS.Add(from, to);
        }
        foreach (XmlNode n in xmlDoc.SelectNodes("Data/Fixes/RegionTypos/RegionTypo"))
        {
          string from = n.Attributes["from"].Value;
          string to = n.Attributes["to"].Value;
          if (REGION_TYPOS.ContainsKey(from))
            Console.WriteLine(string.Format("Error duplicate region typos :{0}", from));
          if (!string.IsNullOrEmpty(from) && !string.IsNullOrEmpty(to))
            REGION_TYPOS.Add(from, to);
        }
        foreach (XmlNode n in xmlDoc.SelectNodes("Data/Fixes/ChapmanCodes/ChapmanCode"))
        {  // add Chapman code to Region Typos to convert locations with codes to region text strings
          string chapmanCode = n.Attributes["chapmanCode"].Value;
          string countyName = n.Attributes["countyName"].Value;
          if (REGION_TYPOS.ContainsKey(chapmanCode))
            Console.WriteLine(string.Format("Error duplicate region typos adding ChapmanCode :{0}", chapmanCode));
          if (!string.IsNullOrEmpty(chapmanCode) && !string.IsNullOrEmpty(countyName))
            REGION_TYPOS.Add(chapmanCode, countyName);
        }
        foreach (XmlNode n in xmlDoc.SelectNodes("Data/Fixes/DemoteCountries/CountryToRegion"))
        {
          string from = n.Attributes["region"].Value;
          string to = n.Attributes["country"].Value;
          if (!string.IsNullOrEmpty(from) && !string.IsNullOrEmpty(to))
          {
            if (COUNTRY_SHIFTS.ContainsKey(from))
              Console.WriteLine(string.Format("Error duplicate country shift :{0}", from));
            COUNTRY_SHIFTS.Add(from, to);
          }
        }
        foreach (XmlNode n in xmlDoc.SelectNodes("Data/Fixes/DemoteCountries/CityAddCountry"))
        {
          string from = n.Attributes["city"].Value;
          string to = n.Attributes["country"].Value;
          if (!string.IsNullOrEmpty(from) && !string.IsNullOrEmpty(to))
          {
            if (CITY_ADD_COUNTRY.ContainsKey(from))
              Console.WriteLine(string.Format("Error duplicate city add country :{0}", from));
            if (COUNTRY_SHIFTS.ContainsKey(from)) // also check country shifts for duplicates
              Console.WriteLine(string.Format("Error duplicate city in country shift :{0}", from));
            CITY_ADD_COUNTRY.Add(from, to);
          }
        }
        foreach (XmlNode n in xmlDoc.SelectNodes("Data/Fixes/DemoteRegions/RegionToParish"))
        {
          string from = n.Attributes["parish"].Value;
          string to = n.Attributes["region"].Value;
          if (REGION_SHIFTS.ContainsKey(from))
            Console.WriteLine(string.Format("Error duplicate region shift :{0}", from));
          if (!string.IsNullOrEmpty(from) && !string.IsNullOrEmpty(to))
          {
            REGION_SHIFTS.Add(from, to);
          }
        }
        foreach (XmlNode n in xmlDoc.SelectNodes("Data/Lookups/FreeCen/Lookup"))
        {
          string code = n.Attributes["code"].Value;
          string county = n.Attributes["county"].Value;
          if (FREECEN_LOOKUP.ContainsKey(county))
            Console.WriteLine(string.Format("Error duplicate freecen lookup :{0}", county));
          if (!string.IsNullOrEmpty(code) && !string.IsNullOrEmpty(county))
            FREECEN_LOOKUP.Add(county, code);
        }
        foreach (XmlNode n in xmlDoc.SelectNodes("Data/Lookups/FindMyPast/Lookup"))
        {
          string code = n.Attributes["code"].Value;
          string county = n.Attributes["county"].Value;
          string country = n.Attributes["country"].Value;
          if (FINDMYPAST_LOOKUP.ContainsKey(county))
            Console.WriteLine(string.Format("Error duplicate FindMyPast lookup :{0}", county));
          if (!string.IsNullOrEmpty(code) && !string.IsNullOrEmpty(county))
          {
            Tuple<string, string> result = new Tuple<string, string>(country, code);
            FINDMYPAST_LOOKUP.Add(county, result);
          }
        }
        foreach (XmlNode n in xmlDoc.SelectNodes("Data/GoogleGeocodes/CountryFixes/CountryFix"))
          AddGoogleFixes(GOOGLE_FIXES, n, COUNTRY);
        foreach (XmlNode n in xmlDoc.SelectNodes("Data/GoogleGeocodes/RegionFixes/RegionFix"))
          AddGoogleFixes(GOOGLE_FIXES, n, REGION);
        foreach (XmlNode n in xmlDoc.SelectNodes("Data/GoogleGeocodes/SubRegionFixes/SubRegionFix"))
          AddGoogleFixes(GOOGLE_FIXES, n, SUBREGION);
        foreach (XmlNode n in xmlDoc.SelectNodes("Data/GoogleGeocodes/MultiLevelFixes/MultiLevelFix"))
          AddGoogleFixes(GOOGLE_FIXES, n, UNKNOWN);
        ValidateTypoFixes();
        ValidateCounties();
        COUNTRY_SHIFTS = COUNTRY_SHIFTS.Concat(CITY_ADD_COUNTRY).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
      }
      else
      {
        Console.WriteLine("Failed to find FactLocationFixes.xml File");
      }
      #endregion
    }

    static void ValidateTypoFixes()
    {
      foreach (string typo in REGION_TYPOS.Values)
        if (!Regions.IsPreferredRegion(typo))
          Console.WriteLine($"Region typo: {typo} is not a preferred region.");
      foreach (string shift in COUNTRY_SHIFTS.Keys)
        if (!Regions.IsPreferredRegion(shift))
          Console.WriteLine($"Country shift: {shift} is not a preferred region.");
    }

    static void ValidateCounties()
    {
      foreach (Region region in Regions.UK_REGIONS)
      {
        if (region.CountyCodes.Count == 0 &&
            (region.Country == Countries.ENGLAND || region.Country == Countries.WALES || region.Country == Countries.SCOTLAND))
          Console.WriteLine($"Missing Conversions for region: {region}");
      }
    }

    public static void LoadGoogleFixesXMLFile(IProgress<string> progress)
    {
      progress.Report("");
#if __PC__
      LOCAL_GOOGLE_FIXES = new Dictionary<Tuple<int, string>, string>();
      try
      {
        string filename = Path.Combine(MappingSettings.Default.CustomMapPath, "GoogleFixes.xml");
        if (File.Exists(filename))
        {
          XmlDocument xmlDoc = new XmlDocument() { XmlResolver = null };
          string xml = File.ReadAllText(filename);
          StringReader sreader = new StringReader(xml);
          using (XmlReader reader = XmlReader.Create(sreader, new XmlReaderSettings() { XmlResolver = null }))
            xmlDoc.Load(reader);
          foreach (XmlNode n in xmlDoc.SelectNodes("GoogleGeocodes/CountryFixes/CountryFix"))
            AddGoogleFixes(LOCAL_GOOGLE_FIXES, n, COUNTRY);
          foreach (XmlNode n in xmlDoc.SelectNodes("GoogleGeocodes/RegionFixes/RegionFix"))
            AddGoogleFixes(LOCAL_GOOGLE_FIXES, n, REGION);
          foreach (XmlNode n in xmlDoc.SelectNodes("GoogleGeocodes/SubRegionFixes/SubRegionFix"))
            AddGoogleFixes(LOCAL_GOOGLE_FIXES, n, SUBREGION);
          foreach (XmlNode n in xmlDoc.SelectNodes("GoogleGeocodes/MultiLevelFixes/MultiLevelFix"))
            AddGoogleFixes(LOCAL_GOOGLE_FIXES, n, UNKNOWN);
          progress.Report(string.Format("\nLoaded {0} Google Fixes.", LOCAL_GOOGLE_FIXES.Count));
        }
      }
      catch (Exception e)
      {
        LOCAL_GOOGLE_FIXES = new Dictionary<Tuple<int, string>, string>();
        progress.Report(string.Format("Error processing user defined GoogleFixes.xml file. File will be ignored.\n\nError was : {0}", e.Message));
      }
#endif
    }

    static void AddGoogleFixes(Dictionary<Tuple<int, string>, string> dictionary, XmlNode n, int level)
    {
      string fromstr = n.Attributes["from"].Value;
      string to = n.Attributes["to"].Value;
      Tuple<int, string> from = new Tuple<int, string>(level, fromstr.ToUpperInvariant());
      if (from != null && fromstr.Length > 0 && to != null)
      {
        if (!dictionary.ContainsKey(from))
          dictionary.Add(from, to);
      }
    }
    #endregion

    #region Object Constructors
    FactLocation()
    {
      OriginalText = string.Empty;
      Country = string.Empty;
      Region = string.Empty;
      SubRegion = string.Empty;
      Address = string.Empty;
      Place = string.Empty;
      ParishID = null;
      individuals = new Dictionary<string, Individual>();
      Latitude = 0;
      Longitude = 0;
      LatitudeM = 0;
      LongitudeM = 0;
      Level = UNKNOWN;
      GeocodeStatus = Geocode.NOT_SEARCHED;
      GEDCOMLatLong = false;
      FoundLocation = string.Empty;
      FoundResultType = string.Empty;
      FoundLevel = -2;
      FTAnalyzerCreated = true; // override when GEDCOM created.
#if __PC__
      ViewPort = new Mapping.GeoResponse.CResult.CGeometry.CViewPort();
#endif
    }

    FactLocation(string location, string latitude, string longitude, Geocode status)
        : this(location)
    {
      Latitude = double.TryParse(latitude, out double temp) ? temp : 0;
      Longitude = double.TryParse(longitude, out temp) ? temp : 0;
#if __PC__
      NetTopologySuite.Geometries.Coordinate point = new NetTopologySuite.Geometries.Coordinate(Longitude, Latitude);
      NetTopologySuite.Geometries.Coordinate mpoint = MapTransforms.TransformCoordinate(point);

      LongitudeM = mpoint.X;
      LatitudeM = mpoint.Y;
#endif
      GeocodeStatus = status;
      if (status == Geocode.NOT_SEARCHED && (Latitude != 0 || Longitude != 0))
        status = Geocode.GEDCOM_USER;
    }

    FactLocation(string location) : this()
    {
      string loc = location.Trim();
      if (string.IsNullOrEmpty(loc))
        return;

      // Save the original location
      OriginalText = loc;

      // Remove Family Historian privacy markers
      loc = loc.Replace("[[", "").Replace("]]", "");

      // Split into constituent parts
      string[] parts = loc.Split(',');
      Country = parts[parts.Length - 1].Trim();
      Level = COUNTRY;

      if (parts.Length >= 2)
      {
        Region = parts[parts.Length - 2].Trim();
        Level = REGION;

        if (parts.Length >= 3)
        {
          SubRegion = parts[parts.Length - 3].Trim();
          Level = SUBREGION;

          if (parts.Length >= 4)
          {
            Address = parts[parts.Length - 4].Trim();
            Level = ADDRESS;

            if (parts.Length >= 5)
            {
              string[] place = new string[parts.Length - 4];
              Array.Copy(parts, place, parts.Length - 4);
              Place = string.Join(", ", place).Trim();
              Level = PLACE;
            }
          }
        }
      }

      if (!GeneralSettings.Default.SkipFixingLocations)
      {
        if (!GeneralSettings.Default.AllowEmptyLocations)
          FixEmptyFields();
        RemoveDiacritics();
        FixRegionFullStops();
        FixCountryFullStops();
        FixMultipleSpacesAmpersandsCommas();
        FixUKGBTypos();
        FixCountryTypos();
        //Country = EnhancedTextInfo.ToTitleCase(FixRegionTypos(Region).ToLower());
        ShiftCountryToRegion();
        Region = FixRegionTypos(Region);
        ShiftRegionToParish();
        FixDoubleLocations();
      }
      KnownRegion = Regions.GetRegion(Region);

      if (!GeneralSettings.Default.SkipFixingLocations)
        FixCapitalisation();
    }

    #endregion

    #region Static Functions
    public static FactLocation GetLocation(string place, bool addLocation = true) => GetLocation(place, string.Empty, string.Empty, Geocode.NOT_SEARCHED, addLocation);

    public static FactLocation GetLocation(string place, string latitude, string longitude, Geocode status, bool addLocation = true, bool updateLatLong = false)
    {
      FactLocation temp;
      if (string.IsNullOrEmpty(place))
        return BLANK_LOCATION;
      if (place.ToUpper() == "UNKNOWN")
        return UNKNOWN_LOCATION;
      // GEDCOM lat/long will be prefixed with NS and EW which needs to be +/- to work.
      latitude = latitude.Replace("N", "").Replace("S", "-");
      longitude = longitude.Replace("W", "-").Replace("E", "");
      if (LOCATIONS.TryGetValue(place, out FactLocation result))
      {  // found location now check if we need to update its geocoding
        if (updateLatLong && !result.IsGeoCoded(true))
        {  // we are updating and old value isn't geocoded 
          temp = new FactLocation(place, latitude, longitude, status);
          if (temp.IsGeoCoded(true))
          {
            result.Latitude = temp.Latitude;
            result.LatitudeM = temp.LatitudeM;
            result.Longitude = temp.Longitude;
            result.LongitudeM = temp.LongitudeM;
            SaveLocationToDatabase(result);
          }
        }
      }
      else
      {
        result = new FactLocation(place, latitude, longitude, status);
        if (LOCATIONS.TryGetValue(result.ToString(), out temp))
        {
          if (updateLatLong)
          {
            if ((!temp.IsGeoCoded(true) && result.IsGeoCoded(true)) || !result.GecodingMatches(temp))
            {  // we are updating the old value isn't geocoded so we can overwrite or the new value doesn't match old database value so overwrite
              temp.Latitude = result.Latitude;
              temp.LatitudeM = result.LatitudeM;
              temp.Longitude = result.Longitude;
              temp.LongitudeM = result.LongitudeM;
              SaveLocationToDatabase(temp);
            }
          }
          return temp;
        }
        if (addLocation)
        {
          if (updateLatLong && result.IsGeoCoded(true))
            SaveLocationToDatabase(result);
          LOCATIONS.Add(result.ToString(), result);
          if (result.Level > COUNTRY)
          {   // recusive call to GetLocation forces create of lower level objects and stores in locations
            result.GetLocation(result.Level - 1);
          }
        }
      }
      return result; // should return object that is in list of locations 
    }

    bool GecodingMatches(FactLocation temp)
        => Latitude == temp.Latitude && Longitude == temp.Longitude && LatitudeM == temp.LatitudeM && LongitudeM == temp.LongitudeM;

    public bool IsValidLatLong => Latitude >= -90 && Latitude <= 90 && Longitude >= -180 && Longitude <= 180;

    public static List<FactLocation> ExposeFactLocations => LOCATIONS.Values.ToList();

    static void SaveLocationToDatabase(FactLocation loc)
    {
      loc.GeocodeStatus = Geocode.GEDCOM_USER;
      loc.FoundLocation = string.Empty;
      loc.FoundLevel = -2;
#if __PC__
      loc.ViewPort = new Mapping.GeoResponse.CResult.CGeometry.CViewPort();
      if (DatabaseHelper.IsLocationInDatabase(loc.ToString()))
      {   // check whether the location in database is geocoded.
        FactLocation inDatabase = new FactLocation(loc.ToString());
        DatabaseHelper.GetLocationDetails(inDatabase);
        if (!inDatabase.IsGeoCoded(true) || !loc.GecodingMatches(inDatabase))
          DatabaseHelper.UpdateGeocode(loc); // only update if existing record wasn't geocoded or doesn't match database contents
      }
      else
        DatabaseHelper.InsertGeocode(loc);
#endif
    }

    public static FactLocation LookupLocation(string place)
    {
      LOCATIONS.TryGetValue(place, out FactLocation result);
      if (result is null)
        result = new FactLocation(place);
      return result;
    }

    public static IEnumerable<FactLocation> AllLocations => LOCATIONS.Values;

    public static void ResetLocations()
    {
      COUNTRY_TYPOS = new Dictionary<string, string>();
      REGION_TYPOS = new Dictionary<string, string>();
      COUNTRY_SHIFTS = new Dictionary<string, string>();
      REGION_SHIFTS = new Dictionary<string, string>();
      CITY_ADD_COUNTRY = new Dictionary<string, string>();
      FREECEN_LOOKUP = new Dictionary<string, string>();
      FINDMYPAST_LOOKUP = new Dictionary<string, Tuple<string, string>>();
      LOCATIONS = new Dictionary<string, FactLocation>();
      GOOGLE_FIXES = new Dictionary<Tuple<int, string>, string>();
      LOCAL_GOOGLE_FIXES = new Dictionary<Tuple<int, string>, string>();

      // set unknown location as unknown so it doesn't keep hassling to be searched
      LOCATIONS.Add("Unknown", UNKNOWN_LOCATION);
      if (!GeneralSettings.Default.SkipFixingLocations)
        LoadConversions(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location));
    }

    public static FactLocation BestLocation(IEnumerable<Fact> facts, FactDate when)
    {
      Fact result = BestLocationFact(facts, when, int.MaxValue);
      return result.Location;
    }

    public static Fact BestLocationFact(IEnumerable<Fact> facts, FactDate when, int limit)
    {
      // this returns a Fact for a FactLocation a person was at for a given period
      Fact result = new Fact("Unknown", Fact.UNKNOWN, FactDate.UNKNOWN_DATE, UNKNOWN_LOCATION);
      double minDistance = float.MaxValue;
      double distance;
      foreach (Fact f in facts)
      {
        if (f.FactDate.IsKnown && !string.IsNullOrEmpty(f.Location.OriginalText))
        {  // only deal with known dates and non empty locations
          if (Fact.RANGED_DATE_FACTS.Contains(f.FactType) && f.FactDate.StartDate.Year != f.FactDate.EndDate.Year) // If fact type is ranged year use least end of range
          {
            distance = Math.Min(Math.Abs(f.FactDate.StartDate.Year - when.BestYear), Math.Abs(f.FactDate.EndDate.Year - when.BestYear));
            distance = Math.Min(distance, Math.Abs(f.FactDate.BestYear - when.BestYear)); // also check mid point to ensure fact is picked up at any point in range
          }
          else
            distance = Math.Abs(f.FactDate.BestYear - when.BestYear);
          if (distance < limit)
          {
            if (distance < minDistance)
            { // this is a closer date but now check to ensure we aren't overwriting a known country with an unknown one.
              if (f.Location.IsKnownCountry || (!f.Location.IsKnownCountry && !result.Location.IsKnownCountry))
              {
                result = f;
                minDistance = distance;
              }
            }
          }
        }
      }
      return result;
    }

    public static void CopyLocationDetails(FactLocation from, FactLocation to)
    {
      if (to is null || from is null) return;
      to.Latitude = from.Latitude;
      to.Longitude = from.Longitude;
      to.LatitudeM = from.LatitudeM;
      to.LongitudeM = from.LongitudeM;
#if __PC__
      to.ViewPort.NorthEast.Lat = from.ViewPort.NorthEast.Lat;
      to.ViewPort.NorthEast.Long = from.ViewPort.NorthEast.Long;
      to.ViewPort.SouthWest.Lat = from.ViewPort.SouthWest.Lat;
      to.ViewPort.SouthWest.Long = from.ViewPort.SouthWest.Long;
#endif
      to.GeocodeStatus = from.GeocodeStatus;
      to.FoundLocation = from.FoundLocation;
      to.FoundResultType = from.FoundResultType;
      to.FoundLevel = from.FoundLevel;
    }
    #endregion

    #region Fix Location string routines
    void FixEmptyFields()
    {
      if (Country.Length == 0)
      {
        Country = Region;
        Region = SubRegion;
        SubRegion = Address;
        Address = Place;
        Place = string.Empty;
      }
      if (Region.Length == 0)
      {
        Region = SubRegion;
        SubRegion = Address;
        Address = Place;
        Place = string.Empty;
      }
      if (SubRegion.Length == 0)
      {
        SubRegion = Address;
        Address = Place;
        Place = string.Empty;
      }
      if (Address.Length == 0)
      {
        Address = Place;
        Place = string.Empty;
      }
    }

    void RemoveDiacritics()
    {
      Country = EnhancedTextInfo.RemoveDiacritics(Country);
      Region = EnhancedTextInfo.RemoveDiacritics(Region);
      SubRegion = EnhancedTextInfo.RemoveDiacritics(SubRegion);
      Address = EnhancedTextInfo.RemoveDiacritics(Address);
      Place = EnhancedTextInfo.RemoveDiacritics(Place);
    }

    void FixCapitalisation()
    {
      if (Country.Length > 1)
        Country = char.ToUpper(Country[0]) + Country.Substring(1);
      if (Region.Length > 1)
        Region = char.ToUpper(Region[0]) + Region.Substring(1);
      if (SubRegion.Length > 1)
        SubRegion = char.ToUpper(SubRegion[0]) + SubRegion.Substring(1);
      if (Address.Length > 1)
        Address = char.ToUpper(Address[0]) + Address.Substring(1);
      if (Place.Length > 1)
        Place = char.ToUpper(Place[0]) + Place.Substring(1);
    }

    private void FixRegionFullStops() => Region = Region.Replace(".", " ").Trim();

    private void FixCountryFullStops() => Country = Country.Replace(".", " ").Trim();

    private void FixMultipleSpacesAmpersandsCommas()
    {
      Country = Country.Replace("  ", " ").Replace("&", "and").Replace(",", "").Trim();
      Region = Region.Replace("  ", " ").Replace("&", "and").Replace(",", "").Trim();
      SubRegion = SubRegion.Replace("  ", " ").Replace("&", "and").Replace(",", "").Trim();
      Address = Address.Replace("  ", " ").Replace("&", "and").Replace(",", "").Trim();
      Place = Place.Replace("  ", " ").Replace("&", "and").Replace(",", "").Trim();
    }

    private void FixUKGBTypos()
    {
      if (Country == "UK" || Country == "GB")
      {
        if (Region == "Scotland" || Region == "England" || Region == "Wales")
        {
          Country = Region;
          Region = SubRegion;
          SubRegion = Address;
          Address = Place;
          Place = string.Empty;
        }
      }
    }

    private string LookupInDictionary(string place, IDictionary<string,string> typoMap)
    {
      string result = string.Empty;
      if (typoMap.ContainsKey(place))
        result = typoMap[place];

      if (string.IsNullOrEmpty(result))
      {
        // Couldn't find, so try again in title case
        string fixCase = EnhancedTextInfo.ToTitleCase(place.ToLower());
        if (typoMap.ContainsKey(fixCase))
          result = typoMap[fixCase];
      }

      return result;
    }

    private void FixCountryTypos()
    {
      string result = LookupInDictionary(Country, COUNTRY_TYPOS);
      if (!string.IsNullOrEmpty(result))
        Country = result;
    }

    private string FixRegionTypos(string toFix)
    {
      if (Country == Countries.AUSTRALIA && toFix.Equals("WA"))
        return "Western Australia"; // fix for WA = Washington

      string result = LookupInDictionary(toFix, REGION_TYPOS);
      return !string.IsNullOrEmpty(result) ? result : toFix;
    }

    private void ShiftCountryToRegion()
    {
      string result = LookupInDictionary(Country, COUNTRY_SHIFTS);
      if (!string.IsNullOrEmpty(result))
      {
        Place = $"{Place} {Address}";
        Address = SubRegion;
        SubRegion = Region;
        Region = Country;
        Country = result;
        if (Level < PLACE) Level++; // we have moved up a level
      }
    }

    private void ShiftRegionToParish()
    {
      if (!Countries.IsUnitedKingdom(Country))
        return; // don't shift regions if not UK

      string result = LookupInDictionary(Region, REGION_SHIFTS);
      if (!string.IsNullOrEmpty(result))
      {
        Place = $"{Place} {Address}";
        Address = SubRegion;
        SubRegion = Region;
        Region = result;
        if (Level < PLACE) Level++; // we have moved up a level
      }
    }

    void FixDoubleLocations()
    {
      if (Country.Equals(Region))
      {
        Region = SubRegion;
        SubRegion = Address;
        Address = Place;
        Place = string.Empty;
      }
      if (Region.Equals(SubRegion))
      {
        SubRegion = Address;
        Address = Place;
        Place = string.Empty;
      }
    }

    public string GoogleFixed
    {
      get
      {
        // first check the multifixes
        string result = FixedLocation;
        foreach (KeyValuePair<Tuple<int, string>, string> fix in LOCAL_GOOGLE_FIXES)
        {
          if (fix.Key.Item1 == UNKNOWN)
            result = result.Replace(fix.Key.Item2, fix.Value, StringComparison.OrdinalIgnoreCase);
        }
        if (result != FixedLocation)
          return result;

        foreach (KeyValuePair<Tuple<int, string>, string> fix in GOOGLE_FIXES)
        {
          if (fix.Key.Item1 == UNKNOWN)
            result = result.Replace(fix.Key.Item2, fix.Value, StringComparison.OrdinalIgnoreCase);
        }
        if (result != FixedLocation)
          return result;

        // now check the individual part fixes
        LOCAL_GOOGLE_FIXES.TryGetValue(new Tuple<int, string>(COUNTRY, Country.ToUpperInvariant()), out string countryFix);
        if (countryFix is null)
        {
          GOOGLE_FIXES.TryGetValue(new Tuple<int, string>(COUNTRY, Country.ToUpperInvariant()), out countryFix);
          if (countryFix is null)
            countryFix = Country;
        }
        LOCAL_GOOGLE_FIXES.TryGetValue(new Tuple<int, string>(REGION, Region.ToUpperInvariant()), out string regionFix);
        if (regionFix is null)
        {
          GOOGLE_FIXES.TryGetValue(new Tuple<int, string>(REGION, Region.ToUpperInvariant()), out regionFix);
          if (regionFix is null)
            regionFix = Region;
        }
        LOCAL_GOOGLE_FIXES.TryGetValue(new Tuple<int, string>(SUBREGION, SubRegion.ToUpperInvariant()), out string subRegionFix);
        if (subRegionFix is null)
        {
          GOOGLE_FIXES.TryGetValue(new Tuple<int, string>(SUBREGION, SubRegion.ToUpperInvariant()), out subRegionFix);
          if (subRegionFix is null)
            subRegionFix = SubRegion;
        }
        result = countryFix;
        if (!string.IsNullOrEmpty(regionFix) || GeneralSettings.Default.AllowEmptyLocations)
          result = regionFix + ", " + result;
        if (!string.IsNullOrEmpty(subRegionFix) || GeneralSettings.Default.AllowEmptyLocations)
          result = subRegionFix + ", " + result;
        if (!string.IsNullOrEmpty(Address) || GeneralSettings.Default.AllowEmptyLocations)
          result = Address + ", " + result;
        if (!string.IsNullOrEmpty(Place))
          result = Place + ", " + result;
        return TrimLeadingCommas(result);
      }
    }

    string TrimLeadingCommas(string toChange)
    {
      while (toChange.StartsWith(", ", StringComparison.Ordinal))
        toChange = toChange.Substring(2);
      return toChange.Trim();
    }

    #endregion
    #region Properties

    public string[] GetParts() => new string[] { Country, Region, SubRegion, Address, Place };

#if __PC__
    public System.Drawing.Image Icon => FactLocationImage.ErrorIcon(GeocodeStatus).Icon;
#endif

    public string AddressNumeric => FixNumerics(Address, true);

    public string PlaceNumeric => FixNumerics(Place, true);

    public bool IsKnownCountry => Countries.IsKnownCountry(Country);

    public bool IsUnitedKingdom => Countries.IsUnitedKingdom(Country);

    public bool IsEnglandWales => Countries.IsEnglandWales(Country);

    public string Geocoded => Geocodes.TryGetValue(GeocodeStatus, out string result) ? result : "Unknown";

    public static int GeocodedLocations => AllLocations.Count(l => l.IsGeoCoded(false));

    public static int LocationsCount => AllLocations.Count() - 1;

    public static int GEDCOMLocationsCount => AllLocations.Count(l => !l.FTAnalyzerCreated);

    public static int GEDCOMGeocodedCount => AllLocations.Count(l => l.GEDCOMLatLong);

    public string CensusCountry
    {
      get
      {
        return Countries.IsUnitedKingdom(Country)
            ? Countries.UNITED_KINGDOM
            : Countries.IsCensusCountry(Country) ? Country : Countries.UNKNOWN_COUNTRY;
      }
    }

    public string FreeCenCountyCode
    {
      get
      {
        FREECEN_LOOKUP.TryGetValue(Region, out string result);
        if (result is null)
          result = "all";
        return result;
      }
    }

    public Tuple<string, string> FindMyPastCountyCode
    {
      get
      {
        FINDMYPAST_LOOKUP.TryGetValue(Region, out Tuple<string, string> result);
        return result;
      }
    }

    public float ZoomLevel
    {
      get
      {
        float zoom = (Level + 3.75f) * 2.2f;  // default use level 
#if __PC__
        if (ViewPort != null)
        {
          double pixelWidth = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width;  // tweak to get best results as required 
          double GLOBE_WIDTH = 512; // a constant in Google's map projection
          var west = ViewPort.SouthWest.Long / 100000;
          var east = ViewPort.NorthEast.Long / 100000;
          var angle = east - west;
          if (angle < 0)
            angle += 360;
          if (west != 0 || east != 0)
            return (int)Math.Abs(Math.Round(Math.Log(pixelWidth * 360f / angle / GLOBE_WIDTH) / Math.Log(2)));
        }
#endif
        return zoom;
      }
    }

    public bool IsBlank => Country.Length == 0;
    public bool IsKnown => this != BLANK_LOCATION && this != UNKNOWN_LOCATION;

    public bool NeedsReverseGeocoding => FoundLocation.Length == 0 &&
                (GeocodeStatus == Geocode.GEDCOM_USER || GeocodeStatus == Geocode.OS_50KMATCH || GeocodeStatus == Geocode.OS_50KPARTIAL || GeocodeStatus == Geocode.OS_50KFUZZY);

    public string FixedLocation
    {
      get
      {
        string fixedLocation = Country;
        if (Region.Length > 0 || GeneralSettings.Default.AllowEmptyLocations)
          fixedLocation = $"{Region}, {fixedLocation}";
        if (SubRegion.Length > 0 || GeneralSettings.Default.AllowEmptyLocations)
          fixedLocation = $"{SubRegion}, {fixedLocation}";
        if (Address.Length > 0 || GeneralSettings.Default.AllowEmptyLocations)
          fixedLocation = $"{Address}, {fixedLocation}";
        if (Place.Length > 0)
          fixedLocation = $"{Place}, {fixedLocation}";
        return TrimLeadingCommas(fixedLocation);
      }
    }

    public string SortableLocation
    {
      get
      {
        string sortableLocation = Country;
        if (Region.Length > 0 || GeneralSettings.Default.AllowEmptyLocations)
          sortableLocation = $"{sortableLocation}, {Region}";
        if (SubRegion.Length > 0 || GeneralSettings.Default.AllowEmptyLocations)
          sortableLocation = $"{sortableLocation}, {SubRegion}";
        if (Address.Length > 0 || GeneralSettings.Default.AllowEmptyLocations)
          sortableLocation = $"{sortableLocation}, {Address}";
        if (Place.Length > 0)
          sortableLocation = $"{sortableLocation}, {Place}";
        return TrimLeadingCommas(sortableLocation);
      }
    }

    #endregion

    #region General Functions

    public FactLocation GetLocation(int level)
    {
      StringBuilder location = new StringBuilder(Country);
      if (level > COUNTRY && (Region.Length > 0 || GeneralSettings.Default.AllowEmptyLocations))
        location.Insert(0, Region + ", ");
      if (level > REGION && (SubRegion.Length > 0 || GeneralSettings.Default.AllowEmptyLocations))
        location.Insert(0, SubRegion + ", ");
      if (level > SUBREGION && (Address.Length > 0 || GeneralSettings.Default.AllowEmptyLocations))
        location.Insert(0, $"{Address}, ");
      if (level > ADDRESS && Place.Length > 0)
        location.Insert(0, $"{Place}, ");
      FactLocation newLocation = GetLocation(location.ToString());
      return newLocation;
    }

    public void AddIndividual(Individual ind)
    {
      if (ind != null && !individuals.ContainsKey(ind.IndividualID))
        individuals.Add(ind.IndividualID, ind);
    }

    public IList<string> Surnames
    {
      get
      {
        HashSet<string> names = new HashSet<string>();
        foreach (Individual i in individuals.Values)
          names.Add(i.Surname);
        List<string> result = names.ToList();
        result.Sort();
        return result;
      }
    }

    public bool SupportedLocation(int level) => Countries.IsCensusCountry(Country) && level == COUNTRY;

    public bool IsGeoCoded(bool recheckPartials)
    {
      if (GeocodeStatus == Geocode.UNKNOWN)
        return true;
      if (Longitude == 0.0 && Latitude == 0.0)
        return false;
      if (!recheckPartials &&
#if __PC__
                MappingSettings.Default.IncludePartials &&
#endif
                (GeocodeStatus == Geocode.PARTIAL_MATCH || GeocodeStatus == Geocode.LEVEL_MISMATCH || GeocodeStatus == Geocode.OS_50KPARTIAL))
        return true;
      return GeocodeStatus == Geocode.MATCHED || GeocodeStatus == Geocode.GEDCOM_USER ||
             GeocodeStatus == Geocode.OS_50KMATCH || GeocodeStatus == Geocode.OS_50KFUZZY;
      // remaining options return false ie: Geocode.OUT_OF_BOUNDS, Geocode.NO_MATCH, Geocode.NOT_SEARCHED, Geocode.INCORRECT
    }

    static readonly Regex numericFix = new Regex("\\d+[A-Za-z]?", RegexOptions.Compiled);

    string FixNumerics(string addressField, bool returnNumber)
    {
      int pos = addressField.IndexOf(" ", StringComparison.Ordinal);
      if (pos > 0 && pos < addressField.Length)
      {
        string number = addressField.Substring(0, pos);
        string name = addressField.Substring(pos + 1);
        Match matcher = numericFix.Match(number);
        if (matcher.Success)
          return returnNumber ? name + " - " + number : name;
      }
      return addressField;
    }

    public bool CensusCountryMatches(string s, bool includeUnknownCountries)
    {
      if (Country.Equals(s))
        return true;
      if (includeUnknownCountries)
      {
        if (!Countries.IsKnownCountry(Country)) // if we have an unknown country then say it matches
          return true;
      }
      if (Country == Countries.UNITED_KINGDOM && Countries.IsUnitedKingdom(s))
        return true;
      if (s == Countries.UNITED_KINGDOM && Countries.IsUnitedKingdom(Country))
        return true;
      if (Country == Countries.SCOTLAND || s == Countries.SCOTLAND)
        return false; // Either Country or s is not Scotland at this point, so not matching census country.
      if (Countries.IsEnglandWales(Country) && Countries.IsEnglandWales(s))
        return true;
      if (Countries.IsUnitedKingdom(Country) && Countries.IsUnitedKingdom(s))
        return true;
      return false;
    }
    #endregion

    public bool IsWithinUKBounds => Longitude >= -7.974074 && Longitude <= 1.879409 && Latitude >= 49.814376 && Latitude <= 60.970872;

    public string Bounds
    {
      get
      {
        string result = string.Empty;
#if __PC__
        if (ViewPort != null)
        {
          if (EmptyViewPort)
            DatabaseHelper.GetLocationDetails(this);
          if (!EmptyViewPort)
          {
            GeoResponse.CResult.CGeometry.CViewPort latLongViewPort = MapTransforms.ReverseTransformViewport(ViewPort);
            result = $"&bounds={latLongViewPort.NorthEast.Lat},{latLongViewPort.NorthEast.Long}|{latLongViewPort.SouthWest.Lat},{latLongViewPort.SouthWest.Long}";
          }
        }
#endif
        return result;
      }
    }

#if __PC__
    public bool EmptyViewPort => ViewPort.NorthEast.Lat == 0 && ViewPort.NorthEast.Long == 0 && ViewPort.SouthWest.Lat == 0 && ViewPort.SouthWest.Long == 0;
#endif

    #region Overrides

    public int CompareTo(object that) => CompareTo(that as FactLocation);

    public int CompareTo(FactLocation that) => CompareTo(that, PLACE);

    public int CompareTo(IDisplayLocation that, int level) => CompareTo((FactLocation)that, level);

    public virtual int CompareTo(FactLocation that, int level)
    {
      int res = string.Compare(Country, that.Country, StringComparison.Ordinal);
      if (res == 0 && level > COUNTRY)
      {
        res = string.Compare(Region, that.Region, StringComparison.Ordinal);
        if (res == 0 && level > REGION)
        {
          res = string.Compare(SubRegion, that.SubRegion, StringComparison.Ordinal);
          if (res == 0 && level > SUBREGION)
          {
            res = string.Compare(AddressNumeric, that.AddressNumeric, StringComparison.Ordinal);
            if (res == 0 && level > ADDRESS)
            {
              res = string.Compare(PlaceNumeric, that.PlaceNumeric, StringComparison.Ordinal);
            }
          }
        }
      }
      return res;
    }

    public override string ToString() => GeneralSettings.Default.SkipFixingLocations ? OriginalText : FixedLocation; //return location;

    public override bool Equals(object obj)
    {
      return obj is FactLocation && CompareTo((FactLocation)obj) == 0;
    }

    public static bool operator ==(FactLocation a, FactLocation b)
    {
      // If both are null, or both are same instance, return true.
      if (ReferenceEquals(a, b))
      {
        return true;
      }
      // If one is null, but not both, return false.
      if ((a is null) || (b is null))
        return false;
      return a.Equals(b);
    }

    public static bool operator !=(FactLocation a, FactLocation b) => !(a == b);

    public bool Equals(FactLocation that, int level) => CompareTo(that, level) == 0;

    public override int GetHashCode() => base.GetHashCode();

    public IComparer<IDisplayLocation> GetComparer(string columnName, bool ascending)
    {
      switch (columnName)
      {
        case "Country": return CompareComparableProperty<IDisplayLocation>(f => f.Country, ascending);
        case "Region": return CompareComparableProperty<IDisplayLocation>(f => f.Region, ascending);
        case "SubRegion": return CompareComparableProperty<IDisplayLocation>(f => f.SubRegion, ascending);
        case "Address": return CompareComparableProperty<IDisplayLocation>(f => f.Address, ascending);
        case "Place": return CompareComparableProperty<IDisplayLocation>(f => f.Place, ascending);
        case "Latitude": return CompareComparableProperty<IDisplayLocation>(f => f.Latitude, ascending);
        case "Longitude": return CompareComparableProperty<IDisplayLocation>(f => f.Longitude, ascending);
        case "Geocoded": return CompareComparableProperty<IDisplayLocation>(f => f.Geocoded, ascending);
        case "FoundLocation": return CompareComparableProperty<IDisplayLocation>(f => f.FoundLocation, ascending);
        case "GEDCOMLocation": return CompareComparableProperty<IDisplayLocation>(f => f.GEDCOMLocation, ascending);
        case "GEDCOMLoaded": return CompareComparableProperty<IDisplayLocation>(f => f.GEDCOMLoaded, ascending);
        default: return null;
      }
    }

    Comparer<T> CompareComparableProperty<T>(Func<IDisplayLocation, IComparable> accessor, bool ascending)
    {
      return Comparer<T>.Create((x, y) =>
      {
        var c1 = accessor(x as IDisplayLocation);
        var c2 = accessor(y as IDisplayLocation);
        var result = c1.CompareTo(c2);
        return ascending ? result : -result;
      });
    }
    #endregion
  }

}