using FTAnalyzer.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;
using static FTAnalyzer.ColourValues;

namespace FTAnalyzer
{
    public class Individual : IComparable<Individual>,
        IDisplayIndividual, IDisplayLooseDeath, IDisplayLooseBirth, IExportIndividual,
        IDisplayColourCensus, IDisplayColourBMD, IDisplayMissingData
    {
        // define relation type from direct ancestor to related by marriage and 
        // MARRIAGEDB ie: married to a direct or blood relation
        public const int UNKNOWN = 1, DIRECT = 2, DESCENDANT = 4, BLOOD = 8, MARRIEDTODB = 16, MARRIAGE = 32, UNSET = 64;
        public const string UNKNOWN_NAME = "UNKNOWN";

        public string IndividualID { get; private set; }
        string _forenames;
        string _fullname;
        string _gender;
        int _relationType;
        List<Fact> _allfacts;
        List<Fact> _allFileFacts;
        DoubleMetaphone surnameMetaphone;
        DoubleMetaphone forenameMetaphone;
        Dictionary<string, Fact> preferredFacts;
        public string Notes { get; private set; }
        public string StandardisedName { get; private set; }
        public bool HasParents { get; set; }
        public bool HasOnlyOneParent { get; set; }
        public bool Infamily { get; set; }
        public bool IsFlaggedAsLiving { get; private set; }
        public decimal Ahnentafel { get; set; }
        public string BudgieCode { get; set; }
        public string RelationToRoot { get; set; }
        public long RelationSort { get; set; }
        public CommonAncestor CommonAncestor { get; set; }
        public string UnrecognisedCensusNotes { get; private set; }
        public IList<Fact> Facts { get; private set; }
        public string Alias { get; set; }

        #region Constructors
        private Individual()
        {
            IndividualID = string.Empty;
            _forenames = string.Empty;
            Surname = string.Empty;
            forenameMetaphone = new DoubleMetaphone();
            surnameMetaphone = new DoubleMetaphone();
            MarriedName = string.Empty;
            StandardisedName = string.Empty;
            UnrecognisedCensusNotes = string.Empty;
            IsFlaggedAsLiving = false;
            Gender = "U";
            Alias = string.Empty;
            Ahnentafel = 0;
            BudgieCode = string.Empty;
            _relationType = UNSET;
            RelationToRoot = string.Empty;
            CommonAncestor = null;
            Infamily = false;
            Notes = string.Empty;
            HasParents = false;
            HasOnlyOneParent = false;
            ReferralFamilyID = string.Empty;
            Facts = new List<Fact>();
            ErrorFacts = new List<Fact>();
            Locations = new List<FactLocation>();
            FamiliesAsChild = new List<ParentalRelationship>();
            FamiliesAsParent = new List<Family>();
            preferredFacts = new Dictionary<string, Fact>();
            _allfacts = null;
            _allFileFacts = null;
        }

        public Individual(XmlNode node, IProgress<string> outputText)
            : this()
        {
            IndividualID = node.Attributes["ID"].Value;
            if (IndividualID == "I126001")
                Console.WriteLine("test");
            Name = FamilyTree.GetText(node, "NAME", false);
            Gender = FamilyTree.GetText(node, "SEX", false);
            Alias = FamilyTree.GetText(node, "ALIA", false);
            IsFlaggedAsLiving = node.SelectSingleNode("_FLGS/__LIVING") != null;
            forenameMetaphone = new DoubleMetaphone(Forename);
            surnameMetaphone = new DoubleMetaphone(Surname);
            Notes = FamilyTree.GetNotes(node);
            StandardisedName = FamilyTree.Instance.GetStandardisedName(IsMale, Forename);

            // Individual attributes
            AddFacts(node, Fact.NAME, outputText);
            AddFacts(node, Fact.AFN, outputText);
            AddFacts(node, Fact.ALIAS, outputText);
            AddFacts(node, Fact.DEGREE, outputText);
            AddFacts(node, Fact.EDUCATION, outputText);
            AddFacts(node, Fact.EMAIL, outputText);
            AddFacts(node, Fact.HEIGHT, outputText);
            AddFacts(node, Fact.MEDICAL_CONDITION, outputText);
            AddFacts(node, Fact.NAMESAKE, outputText);
            AddFacts(node, Fact.NATIONALITY, outputText);
            AddFacts(node, Fact.NAT_ID_NO, outputText);
            AddFacts(node, Fact.NUM_CHILDREN, outputText);
            AddFacts(node, Fact.NUM_MARRIAGE, outputText);
            AddFacts(node, Fact.OCCUPATION, outputText);
            AddFacts(node, Fact.ORIGIN, outputText);
            AddFacts(node, Fact.PHONE, outputText);
            AddFacts(node, Fact.PHYSICAL_DESC, outputText);
            AddFacts(node, Fact.PROPERTY, outputText);
            AddFacts(node, Fact.REFERENCE, outputText);
            AddFacts(node, Fact.SOCIAL_SECURITY, outputText);
            AddFacts(node, Fact.TITLE, outputText);
            AddFacts(node, Fact.WEIGHT, outputText);

            // Individual events - key facts first
            AddFacts(node, Fact.BIRTH, outputText);
            AddFacts(node, Fact.BIRTH_CALC, outputText);
            AddFacts(node, Fact.DEATH, outputText);
            AddFacts(node, Fact.CENSUS, outputText);

            // Individuals events non key facts
            AddFacts(node, Fact.ADOPTION, outputText);
            AddFacts(node, Fact.ADULT_CHRISTENING, outputText);
            AddFacts(node, Fact.BAPTISM, outputText);
            AddFacts(node, Fact.BAPTISM_LDS, outputText);
            AddFacts(node, Fact.BAR_MITZVAH, outputText);
            AddFacts(node, Fact.BAS_MITZVAH, outputText);
            AddFacts(node, Fact.BLESSING, outputText);
            AddFacts(node, Fact.BURIAL, outputText);
            AddFacts(node, Fact.CASTE, outputText);
            AddFacts(node, Fact.CHRISTENING, outputText);
            AddFacts(node, Fact.CIRCUMCISION, outputText);
            AddFacts(node, Fact.CONFIRMATION, outputText);
            AddFacts(node, Fact.CONFIRMATION_LDS, outputText);
            AddFacts(node, Fact.CREMATION, outputText);
            AddFacts(node, Fact.DESTINATION, outputText);
            AddFacts(node, Fact.DNA, outputText);
            AddFacts(node, Fact.ELECTION, outputText);
            AddFacts(node, Fact.EMIGRATION, outputText);
            AddFacts(node, Fact.EMPLOYMENT, outputText);
            AddFacts(node, Fact.ENDOWMENT_LDS, outputText);
            AddFacts(node, Fact.EXCOMMUNICATION, outputText);
            AddFacts(node, Fact.FIRST_COMMUNION, outputText);
            AddFacts(node, Fact.GRADUATION, outputText);
            AddFacts(node, Fact.IMMIGRATION, outputText);
            AddFacts(node, Fact.INITIATORY_LDS, outputText);
            AddFacts(node, Fact.LEGATEE, outputText);
            AddFacts(node, Fact.MILITARY, outputText);
            AddFacts(node, Fact.MISSION_LDS, outputText);
            AddFacts(node, Fact.NATURALIZATION, outputText);
            AddFacts(node, Fact.ORDINANCE, outputText);
            AddFacts(node, Fact.ORDINATION, outputText);
            AddFacts(node, Fact.PROBATE, outputText);
            AddFacts(node, Fact.RELIGION, outputText);
            AddFacts(node, Fact.RESIDENCE, outputText);
            AddFacts(node, Fact.RETIREMENT, outputText);
            AddFacts(node, Fact.SEALED_TO_PARENTS, outputText);
            AddFacts(node, Fact.SERVICE_NUMBER, outputText);
            AddFacts(node, Fact.WILL, outputText);

            // Custom facts
            AddFacts(node, Fact.CUSTOM_EVENT, outputText);
            AddFacts(node, Fact.CUSTOM_FACT, outputText);
            AddFacts(node, Fact.UNKNOWN, outputText);

            if (GeneralSettings.Default.AutoCreateCensusFacts)
            {
                AddCensusSourceFacts();
                AddCensusNoteFacts();
            }
        }

        internal Individual(Individual i)
        {
            if (i != null)
            {
                IndividualID = i.IndividualID;
                _forenames = i._forenames;
                Surname = i.Surname;
                forenameMetaphone = i.forenameMetaphone;
                surnameMetaphone = i.surnameMetaphone;
                MarriedName = i.MarriedName;
                StandardisedName = i.StandardisedName;
                _fullname = i._fullname;
                SortedName = i.SortedName;
                IsFlaggedAsLiving = i.IsFlaggedAsLiving;
                _gender = i._gender;
                Alias = i.Alias;
                Ahnentafel = i.Ahnentafel;
                BudgieCode = i.BudgieCode;
                _relationType = i._relationType;
                RelationToRoot = i.RelationToRoot;
                Infamily = i.Infamily;
                Notes = i.Notes;
                HasParents = i.HasParents;
                HasOnlyOneParent = i.HasOnlyOneParent;
                ReferralFamilyID = i.ReferralFamilyID;
                CommonAncestor = i.CommonAncestor;
                Facts = new List<Fact>(i.Facts);
                ErrorFacts = new List<Fact>(i.ErrorFacts);
                Locations = new List<FactLocation>(i.Locations);
                FamiliesAsChild = new List<ParentalRelationship>(i.FamiliesAsChild);
                FamiliesAsParent = new List<Family>(i.FamiliesAsParent);
                preferredFacts = new Dictionary<string, Fact>(i.preferredFacts);
            }
        }
        #endregion

        #region Properties

        public bool HasRangedBirthDate => BirthDate.DateType == FactDate.FactDateType.BET && BirthDate.StartDate.Year != BirthDate.EndDate.Year;

        public bool HasLostCousinsFact
        {
            get
            {
                foreach (Fact f in AllFacts)
                    if (f.FactType == Fact.LOSTCOUSINS || f.FactType == Fact.LC_FTA)
                        return true;
                return false;
            }
        }

        public int RelationType
        {
            get => _relationType;
            set
            {
                if (_relationType == UNKNOWN || _relationType > value)
                    _relationType = value;
            }
        }

        public bool IsBloodDirect => _relationType == BLOOD || _relationType == DIRECT || _relationType == DESCENDANT || _relationType == MARRIEDTODB;

        public bool HasNotes => Notes.Length > 0;
        public string HasNotesMac => HasNotes ? "Yes" : "No";

        public string Relation
        {
            get
            {
                switch (_relationType)
                {
                    case DIRECT: return Ahnentafel == 1 ? "Root Person" : "Direct Ancestor";
                    case BLOOD: return "Blood Relation";
                    case MARRIAGE: return "By Marriage";
                    case MARRIEDTODB: return "Marr to Direct/Blood";
                    case DESCENDANT: return "Descendant";
                    default: return "Unknown";
                }
            }
        }

        public IList<Fact> PersonalFacts => Facts;

        private IList<Fact> FamilyFacts
        {
            get
            {
                var familyfacts = new List<Fact>();
                foreach (Family f in FamiliesAsParent)
                    familyfacts.AddRange(f.Facts);
                return familyfacts;
            }
        }

        public IList<Fact> ErrorFacts { get; }

        int Factcount { get; set; }
        public IList<Fact> AllFacts
        {
            get
            {
                int currentFactCount = Facts.Count + FamilyFacts.Count;
                if (_allfacts == null || currentFactCount != Factcount)
                {
                    _allfacts = new List<Fact>();
                    _allfacts.AddRange(PersonalFacts);
                    _allfacts.AddRange(FamilyFacts);
                    _allFileFacts = _allfacts.Where(x => !x.Created).ToList();
                    Factcount = _allfacts.Count;
                }
                return _allfacts;
            }
        }

        public IList<Fact> AllFileFacts => _allFileFacts;

        public IList<IDisplayFact> AllGeocodedFacts
        {
            get
            {
                List<IDisplayFact> allGeocodedFacts = new List<IDisplayFact>();
                foreach (Fact f in AllFacts)
                    if (f.Location.IsGeoCoded(false) && f.Location.GeocodeStatus != FactLocation.Geocode.UNKNOWN)
                        allGeocodedFacts.Add(new DisplayFact(this, f));
                allGeocodedFacts.Sort();
                return allGeocodedFacts;
            }
        }

        public int GeoLocationCount => AllGeocodedFacts.Count;

        public IList<FactLocation> Locations { get; }

        public string Gender
        {
            get => _gender;
            private set
            {
                _gender = value;
                if (_gender.Length == 0)
                    _gender = "U";
            }
        }

        public bool GenderMatches(Individual that) => Gender == that.Gender || Gender == "U" || that.Gender == "U";

        public string SortedName { get; private set; }

        public string Name
        {
            get => _fullname;
            private set
            {
                string name = value;
                int startPos = name.IndexOf("/", StringComparison.Ordinal), endPos = name.LastIndexOf("/", StringComparison.Ordinal);
                if (startPos >= 0 && endPos > startPos)
                {
                    Surname = name.Substring(startPos + 1, endPos - startPos - 1);
                    _forenames = startPos == 0 ? UNKNOWN_NAME : name.Substring(0, startPos).Trim();
                }
                else
                {
                    Surname = UNKNOWN_NAME;
                    _forenames = name;
                }
                if (Surname == "?" || Surname.ToLower() == "mnu" || Surname.ToLower() == "lnu" || Surname.ToLower() == "_____" || Surname.Length == 0)
                    Surname = UNKNOWN_NAME;
                if (GeneralSettings.Default.TreatFemaleSurnamesAsUnknown && !IsMale && Surname.StartsWith("(", StringComparison.Ordinal) && Surname.EndsWith(")", StringComparison.Ordinal))
                    Surname = UNKNOWN_NAME;
                MarriedName = Surname;
                _fullname = SetFullName();
                SortedName = $"{_forenames} {Surname}".Trim();
            }
        }

        public string SetFullName()
        {
            return GeneralSettings.Default.ShowAliasInName && Alias.Length > 0
                ? $"{_forenames}  '{Alias}' {Surname}".Trim()
                : $"{_forenames} {Surname}".Trim();
        }

        public string Forename
        {
            get
            {
                if (_forenames == null)
                    return string.Empty;
                int pos = _forenames.IndexOf(" ", StringComparison.Ordinal);
                return pos > 0 ? _forenames.Substring(0, pos) : _forenames;
            }
        }

        public string ForenameMetaphone => forenameMetaphone.PrimaryKey;

        public string Forenames => GeneralSettings.Default.ShowAliasInName && Alias.Length > 0 ? $"{_forenames} '{Alias}' " : _forenames;

        public string Surname { get; private set; }

        public string SurnameMetaphone => surnameMetaphone.PrimaryKey;

        public string MarriedName { get; set; }

        public Fact BirthFact
        {
            get
            {
                Fact f = GetPreferredFact(Fact.BIRTH);
                if (f != null)
                    return f;
                f = GetPreferredFact(Fact.BIRTH_CALC);
                if (GeneralSettings.Default.UseBaptismDates)
                {
                    if (f != null)
                        return f;
                    f = GetPreferredFact(Fact.BAPTISM);
                    if (f != null)
                        return f;
                    f = GetPreferredFact(Fact.CHRISTENING);
                }
                return f;
            }
        }

        public FactDate BirthDate => BirthFact == null ? FactDate.UNKNOWN_DATE : BirthFact.FactDate;

        public DateTime BirthStart => BirthDate.StartDate != FactDate.MINDATE ? BirthDate.StartDate : BirthDate.EndDate;
        public DateTime BirthEnd => BirthDate.StartDate != FactDate.MAXDATE ? BirthDate.EndDate : BirthDate.StartDate;

        public FactLocation BirthLocation => (BirthFact == null) ? FactLocation.BLANK_LOCATION : BirthFact.Location;

        public Fact DeathFact
        {
            get
            {
                Fact f = GetPreferredFact(Fact.DEATH);
                if (GeneralSettings.Default.UseBurialDates)
                {
                    if (f != null)
                        return f;
                    f = GetPreferredFact(Fact.BURIAL);
                    if (f != null)
                        return f;
                    f = GetPreferredFact(Fact.CREMATION);
                }
                return f;
            }
        }

        public FactDate DeathDate => DeathFact == null ? FactDate.UNKNOWN_DATE : DeathFact.FactDate;

        public DateTime DeathStart => DeathDate.StartDate != FactDate.MINDATE ? DeathDate.StartDate : DeathDate.EndDate;
        public DateTime DeathEnd => DeathDate.EndDate != FactDate.MAXDATE ? DeathDate.EndDate : DeathDate.StartDate;

        public FactLocation DeathLocation => DeathFact == null ? FactLocation.BLANK_LOCATION : DeathFact.Location;

        public FactDate BurialDate
        {
            get
            {
                Fact f = GetPreferredFact(Fact.BURIAL);
                return f?.FactDate;
            }
        }

        public string Occupation
        {
            get
            {
                Fact occupation = GetPreferredFact(Fact.OCCUPATION);
                return occupation == null ? string.Empty : occupation.Comment;
            }
        }

        private int MaxAgeAtDeath => GetAge(DeathDate).MaxAge;

        public Age LifeSpan => GetAge(DateTime.Now);

        public FactDate LooseBirthDate
        {
            get
            {
                Fact loose = GetPreferredFact(Fact.LOOSEBIRTH);
                return loose == null ? FactDate.UNKNOWN_DATE : loose.FactDate;
            }
        }

        public string LooseBirth
        {
            get
            {
                FactDate fd = LooseBirthDate;
                return (fd.StartDate > fd.EndDate) ? "Alive facts after death, check data errors tab and children's births" : fd.ToString();
            }
        }

        public FactDate LooseDeathDate
        {
            get
            {
                Fact loose = GetPreferredFact(Fact.LOOSEDEATH);
                return loose == null ? FactDate.UNKNOWN_DATE : loose.FactDate;
            }
        }

        public string LooseDeath
        {
            get
            {
                FactDate fd = LooseDeathDate;
                return (fd.StartDate > fd.EndDate) ? "Alive facts after death, check data errors tab and children's births" : fd.ToString();
            }
        }

        public string IndividualRef => $"{IndividualID}: {Name}";

        public string ServiceNumber
        {
            get
            {
                Fact service = GetPreferredFact(Fact.SERVICE_NUMBER);
                return service == null ? string.Empty : service.Comment;
            }
        }

        public IList<Family> FamiliesAsParent { get; }

        public IList<ParentalRelationship> FamiliesAsChild { get; }

        public bool IsNaturalChildOf(Individual parent)
        {
            foreach (ParentalRelationship pr in FamiliesAsChild)
            {
                if (pr.Family != null)
                    return (pr.IsNaturalFather && parent.IsMale && parent.Equals(pr.Father)) ||
                           (pr.IsNaturalMother && !parent.IsMale && parent.Equals(pr.Mother));
            }
            return false;
        }

        public Individual NaturalFather
        {
            get
            {
                foreach (ParentalRelationship pr in FamiliesAsChild)
                {
                    if (pr.Family != null && pr.Father != null && pr.IsNaturalFather)
                        return pr.Father;
                }
                return null;
            }
        }

        public int FactCount(string factType) => Facts.Count(f => f.FactType == factType && f.FactErrorLevel == Fact.FactError.GOOD);

        public int ResidenceCensusFactCount => Facts.Count(f => f.FactType == Fact.RESIDENCE && f.IsCensusFact);

        public int ErrorFactCount(string factType, Fact.FactError errorLevel) => ErrorFacts.Count(f => f.FactType == factType && f.FactErrorLevel == errorLevel);

        public string MarriageDates
        {
            get
            {
                string output = string.Empty;
                foreach (Family f in FamiliesAsParent)
                    if (!string.IsNullOrEmpty(f.MarriageDate?.ToString()))
                        output += $"{f.MarriageDate}; ";
                return output.Length > 0 ? output.Substring(0, output.Length - 2) : output; // remove trailing ;
            }
        }

        public string MarriageLocations
        {
            get
            {
                string output = string.Empty;
                foreach (Family f in FamiliesAsParent)
                    if (!string.IsNullOrEmpty(f.MarriageLocation))
                        output += $"{f.MarriageLocation}; ";
                return output.Length > 0 ? output.Substring(0, output.Length - 2) : output; // remove trailing ;
            }
        }

        public int MarriageCount => FamiliesAsParent.Count;

        public int ChildrenCount => FamiliesAsParent.Sum(x => x.Children.Count);

        #endregion

        #region Boolean Tests

        public bool IsMale => _gender.Equals("M");

        public bool IsInFamily => Infamily;

        public bool IsMarried(FactDate fd)
        {
            if (IsSingleAtDeath())
                return false;
            return FamiliesAsParent.Any(f =>
            {
                FactDate marriage = f.GetPreferredFactDate(Fact.MARRIAGE);
                return (marriage != null && marriage.IsBefore(fd));
            });
        }

        public bool HasMilitaryFacts => Facts.Any(f => f.FactType == Fact.MILITARY || f.FactType == Fact.SERVICE_NUMBER);

        public bool HasCensusLocation(CensusDate when)
        {
            if (when == null) return false;
            foreach (Fact f in Facts)
            {
                if (f.IsValidCensus(when) && f.Location.ToString().Length > 0)
                    return true;
            }
            return false;
        }

        public bool CensusFactExists(FactDate factDate, bool includeCreated)
        {
            if (factDate == null) return false;
            foreach (Fact f in Facts)
            {
                if (f.IsValidCensus(factDate))
                    return !f.Created || includeCreated;
            }
            return false;
        }

        public bool IsCensusDone(CensusDate when) => IsCensusDone(when, true, true);
        public bool IsCensusDone(CensusDate when, bool includeUnknownCountries) => IsCensusDone(when, includeUnknownCountries, true);
        public bool IsCensusDone(CensusDate when, bool includeUnknownCountries, bool checkCountry)
        {
            if (when == null) return false;
            foreach (Fact f in Facts)
            {
                if (f.IsValidCensus(when))
                {
                    if (!checkCountry)
                        return true;
                    if (f.Location.CensusCountryMatches(when.Country, includeUnknownCountries))
                        return true;
                    if (Countries.IsUnitedKingdom(when.Country) && f.IsOverseasUKCensus(f.Country))
                        return true;
                }
            }
            return false;
        }

        public bool IsTaggedMissingCensus(CensusDate when)
        {
            return when != null && Facts.Any(x => x.FactType == Fact.MISSING && x.FactDate.Overlaps(when));
        }

        public bool IsLostCousinsEntered(CensusDate when) => IsLostCousinsEntered(when, true);
        public bool IsLostCousinsEntered(CensusDate when, bool includeUnknownCountries)
        {
            foreach (Fact f in Facts)
            {
                if (f.IsValidLostCousins(when))
                {
                    if (f.Location.CensusCountryMatches(when.Country, includeUnknownCountries) || BestLocation(when).CensusCountryMatches(when.Country, includeUnknownCountries))
                        return true;
                    Fact censusFact = GetCensusFact(f);
                    if (censusFact != null)
                    {
                        if (when.Country.Equals(Countries.SCOTLAND) && Countries.IsEnglandWales(censusFact.Country))
                            return false;
                        if (Countries.IsUnitedKingdom(when.Country) && censusFact.IsOverseasUKCensus(censusFact.Country))
                            return true;
                    }
                }
            }
            return false;
        }

        public bool HasLostCousinsFactWithNoCensusFact
        {
            get
            {
                foreach (CensusDate censusDate in CensusDate.LOSTCOUSINS_CENSUS)
                {
                    if (IsLostCousinsEntered(censusDate, false) && !IsCensusDone(censusDate))
                        return true;
                }
                return false;
            }
        }

        public bool MissingLostCousins(CensusDate censusDate, bool includeUnknownCountries)
        {
            bool isCensusDone = IsCensusDone(censusDate, includeUnknownCountries);
            bool isLostCousinsEntered = IsLostCousinsEntered(censusDate, includeUnknownCountries);
            return isCensusDone && !isLostCousinsEntered;
        }

        public bool IsAlive(FactDate when) => IsBorn(when) && !IsDeceased(when);

        public bool IsBorn(FactDate when) => !BirthDate.IsKnown || BirthDate.StartsBefore(when); // assume born if birthdate is unknown

        public bool IsDeceased(FactDate when) => DeathDate.IsKnown && DeathDate.IsBefore(when);

        public bool IsSingleAtDeath() => GetPreferredFact(Fact.UNMARRIED) != null || MaxAgeAtDeath < 16 || LifeSpan.MaxAge < 16;

        public bool IsBirthKnown() => BirthDate.IsKnown && BirthDate.IsExact;

        public bool IsDeathKnown() => DeathDate.IsKnown && DeathDate.IsExact;

        #endregion

        #region Age Functions

        public Age GetAge(FactDate when)
        {
            return new Age(this, when);
        }

        public Age GetAge(FactDate when, string factType)
        {
            return (factType == Fact.BIRTH || factType == Fact.PARENT) ? Age.BIRTH : new Age(this, when);
        }

        public Age GetAge(DateTime when)
        {
            string now = FactDate.Format(FactDate.FULL, when);
            return GetAge(new FactDate(now));
        }

        public int GetMaxAge(FactDate when)
        {
            return GetAge(when).MaxAge;
        }

        public int GetMinAge(FactDate when)
        {
            return GetAge(when).MinAge;
        }

        public int GetMaxAge(DateTime when)
        {
            string now = FactDate.Format(FactDate.FULL, when);
            return GetMaxAge(new FactDate(now));
        }

        public int GetMinAge(DateTime when)
        {
            string now = FactDate.Format(FactDate.FULL, when);
            return GetMinAge(new FactDate(now));
        }
        #endregion

        #region Fact Functions

        void AddFacts(XmlNode node, string factType, IProgress<string> outputText)
        {
            XmlNodeList list = node.SelectNodes(factType);
            bool preferredFact = true;
            foreach (XmlNode n in list)
            {
                try
                {
                    if (factType != Fact.NAME || !preferredFact)
                    {  // don't add first name in file as a fact as already given by SURNAME & FORENAME tags
                        Fact f = new Fact(n, this, preferredFact, null, outputText);
                        f.Location.FTAnalyzerCreated = false;
                        if (!f.Location.IsValidLatLong)
                            outputText.Report($"Found problem with Lat/Long for Location '{f.Location}' in facts for {IndividualID}: {Name}");
                        AddFact(f);
                        if (f.GedcomAge != null && f.GedcomAge.CalculatedBirthDate != FactDate.UNKNOWN_DATE)
                        {
                            string reason = $"Calculated from {f} with Age: {f.GedcomAge.GEDCOM_Age}";
                            Fact calculatedBirth = new Fact(IndividualID, Fact.BIRTH_CALC, f.GedcomAge.CalculatedBirthDate, FactLocation.UNKNOWN_LOCATION, reason, false, true);
                            AddFact(calculatedBirth);
                        }
                    }
                }
                catch (InvalidXMLFactException ex)
                {
                    FamilyTree ft = FamilyTree.Instance;
                    outputText.Report($"Error with Individual : {IndividualRef}\n       Invalid fact : {ex.Message}");
                }
                catch (TextFactDateException te)
                {
                    if (BirthDate.IsKnown)
                    {
                        int years;
                        switch (te.Message)
                        {
                            case "STILLBORN":
                                years = 0;
                                break;
                            case "INFANT":
                                years = 5;
                                break;
                            case "CHILD":
                                years = 14;
                                break;
                            case "YOUNG":
                                years = 21;
                                break;
                            case "UNMARRIED":
                            case "NEVER MARRIED":
                            case "NOT MARRIED":
                                years = -2;
                                break;
                            default:
                                years = -1;
                                break;
                        }
                        if (years >= 0 && factType == Fact.DEATH)  //only add a death fact if text is one of the death types
                        {
                            FactDate deathdate = BirthDate.AddEndDateYears(years);
                            Fact f = new Fact(n, this, preferredFact, deathdate, outputText);
                            AddFact(f);
                        }
                        else
                        {
                            Fact f = new Fact(n, this, preferredFact, FactDate.UNKNOWN_DATE, outputText); // write out death fact with unknown date
                            AddFact(f);
                            f = new Fact(string.Empty, Fact.UNMARRIED, FactDate.UNKNOWN_DATE, FactLocation.UNKNOWN_LOCATION, string.Empty, true, true);
                            AddFact(f);
                        }
                    }
                }
                preferredFact = false;
            }
        }

        public void AddFact(Fact fact)
        {
            FamilyTree ft = FamilyTree.Instance;
            if (ft.FactBeforeBirth(this, fact))
                fact.SetError((int)FamilyTree.Dataerror.FACTS_BEFORE_BIRTH, Fact.FactError.ERROR,
                    $"{fact.FactTypeDescription} fact recorded: {fact.FactDate} before individual was born");
            if (ft.FactAfterDeath(this, fact))
                fact.SetError((int)FamilyTree.Dataerror.FACTS_AFTER_DEATH, Fact.FactError.ERROR,
                    $"{fact.FactTypeDescription} fact recorded: {fact.FactDate} after individual died");

            switch (fact.FactErrorLevel)
            {
                case Fact.FactError.GOOD:
                    AddGoodFact(fact);
                    break;
                case Fact.FactError.WARNINGALLOW:
                    AddGoodFact(fact);
                    ErrorFacts.Add(fact);
                    break;
                case Fact.FactError.WARNINGIGNORE:
                case Fact.FactError.ERROR:
                    ErrorFacts.Add(fact);
                    break;
            }
        }

        void AddGoodFact(Fact fact)
        {
            Facts.Add(fact);
            if (fact.Preferred && !preferredFacts.ContainsKey(fact.FactType))
                preferredFacts.Add(fact.FactType, fact);
            AddLocation(fact);
        }

        /// <summary>
        /// Checks the individual's node data to see if any census references exist in the source records
        /// </summary>
        void AddCensusSourceFacts()
        {
            List<Fact> toAdd = new List<Fact>(); // we can't vary the facts collection whilst looping
            foreach (Fact f in Facts)
            {
                if (!f.IsCensusFact && !CensusFactExists(f.FactDate, true))
                {
                    foreach (FactSource s in f.Sources)
                    {
                        CensusReference cr = new CensusReference(IndividualID, s.SourceTitle + " " + s.SourceText, true);
                        if (OKtoAddReference(cr, true))
                        {
                            cr.Fact.Sources.Add(s);
                            toAdd.Add(cr.Fact);
                            if (cr.IsLCCensusFact)
                                CreateLCFact(toAdd, cr);
                        }
                        else
                            UpdateCensusFactReference(cr);
                    }
                }
            }
            foreach (Fact f in toAdd)
                AddFact(f);
        }

        void CreateLCFact(List<Fact> toAdd, CensusReference cr)
        {
            if (!IsLostCousinsEntered((CensusDate)cr.Fact.FactDate))
            {
                Fact lcFact = new Fact("LostCousins", Fact.LC_FTA, cr.Fact.FactDate, cr.Fact.Location, "Lost Cousins fact created by FTAnalyzer by recognising census ref " + cr.Reference, false, true);
                if (toAdd == null)
                    AddFact(lcFact);
                else
                    toAdd.Add(lcFact);
            }
        }

        /// <summary>
        /// Checks the notes against an individual to see if any census data exists
        /// </summary>
        void AddCensusNoteFacts()
        {
            if (HasNotes)
            {
                bool checkNotes = true;
                string notes = CensusReference.ClearCommonPhrases(Notes);
                while (checkNotes)
                {
                    checkNotes = false;
                    CensusReference cr = new CensusReference(IndividualID, notes, false);
                    if (OKtoAddReference(cr, false))
                    {   // add census fact even if other created census facts exist for that year
                        AddFact(cr.Fact);
                        if (cr.IsLCCensusFact)
                            CreateLCFact(null, cr);
                    }
                    else
                        UpdateCensusFactReference(cr);
                    if (cr.MatchString.Length > 0)
                    {
                        int pos = notes.IndexOf(cr.MatchString, StringComparison.OrdinalIgnoreCase);
                        if (pos != -1)
                        {
                            notes = notes.Remove(pos, cr.MatchString.Length);
                            checkNotes = notes.Length > 0 && cr.MatchString.Length > 0;
                        }
                    }
                }
                if (notes.Length > 10) // no point recording really short notes 
                    UnrecognisedCensusNotes = IndividualID + ": " + Name + ". Notes : " + notes;
            }
        }

        void UpdateCensusFactReference(CensusReference cr)
        {
            Fact censusFact = GetCensusFact(cr.Fact, false);
            if (censusFact != null && censusFact.CensusReference.Status.Equals(CensusReference.ReferenceStatus.BLANK) && (cr.IsKnownStatus))
                censusFact.SetCensusReferenceDetails(cr, CensusLocation.UNKNOWN, string.Empty);
        }

        bool OKtoAddReference(CensusReference cr, bool includeCreated) => cr.IsKnownStatus && !CensusFactExists(cr.Fact.FactDate, includeCreated);

        void AddLocation(Fact fact)
        {
            FactLocation loc = fact.Location;
            if (loc != null && !Locations.Contains(loc))
            {
                Locations.Add(loc);
                loc.AddIndividual(this);
            }
        }

        public Fact GetPreferredFact(string factType) => preferredFacts.ContainsKey(factType) ? preferredFacts[factType] : Facts.FirstOrDefault(f => f.FactType == factType);

        public FactDate GetPreferredFactDate(string factType)
        {
            Fact f = GetPreferredFact(factType);
            return (f == null || f.FactDate == null) ? FactDate.UNKNOWN_DATE : f.FactDate;
        }
        
        // Returns all facts of the given type.
        public IEnumerable<Fact> GetFacts(string factType) => Facts.Where(f => f.FactType == factType);

        public string SurnameAtDate(FactDate date)
        {
            string name = Surname;
            if (!IsMale)
            {
                foreach (Family marriage in FamiliesAsParent.OrderBy(f => f.MarriageDate))
                {
                    if ((marriage.MarriageDate.Equals(date) || marriage.MarriageDate.IsBefore(date)) && marriage.Husband != null)
                        name = marriage.Husband.Surname;
                }
            }
            return name;
        }

        public void QuestionGender(Family family, bool pHusband)
        {
            string description;
            if (Gender.Equals("U"))
            {
                string spouse = pHusband ? "husband" : "wife";
                description = $"Unknown gender but appears as a {spouse} in family {family.FamilyRef} check gender setting";
            }
            else
            {
                if (IsMale)
                    description = $"Male but appears as a wife in family {family.FamilyRef} check gender setting";
                else
                    description = $"Female but appears as husband in family {family.FamilyRef} check gender setting";
            }
            var gender = new Fact(family.FamilyID, Fact.GENDER, FactDate.UNKNOWN_DATE, null, description, true, true);
            gender.SetError(26, Fact.FactError.ERROR, description);
            AddFact(gender);
        }
        #endregion

        #region Location functions

        public FactLocation BestLocation(FactDate when) => FactLocation.BestLocation(AllFacts, when);  // this returns a Location a person was at for a given period

        public Fact BestLocationFact(FactDate when, int limit) => FactLocation.BestLocationFact(AllFacts, when, limit); // this returns a Fact a person was at for a given period

        public bool IsAtLocation(FactLocation loc, int level)
        {
            foreach (Fact f in AllFacts)
            {
                if (f.Location.Equals(loc, level))
                    return true;
            }
            return false;
        }
        #endregion

        readonly FactComparer factComparer = new FactComparer();

        public int DuplicateLCFacts
        {
            get
            {
                IEnumerable<Fact> lcFacts = AllFacts.Where(f => f.FactType == Fact.LOSTCOUSINS || f.FactType == Fact.LC_FTA);
                int distinctFacts = lcFacts.Distinct(factComparer).Count();
                return LostCousinsFacts - distinctFacts;
            }
        }

        public int DuplicateLCCensusFacts
        {
            get
            {
                IEnumerable<Fact> censusFacts = AllFacts.Where(f => f.IsLCCensusFact);
                int distinctFacts = censusFacts.Distinct(factComparer).Count();
                return censusFacts.Count() - distinctFacts;
            }
        }

        public int LostCousinsFacts => Facts.Count(f => f.FactType == Fact.LOSTCOUSINS || f.FactType == Fact.LC_FTA);

        public string ReferralFamilyID { get; set; }

        public Fact GetCensusFact(Fact lcFact, bool includeCreated = true)
        {
            return includeCreated
                ? Facts.FirstOrDefault(x => x.IsCensusFact && x.FactDate.Overlaps(lcFact.FactDate))
                : Facts.FirstOrDefault(x => x.IsCensusFact && !x.Created && x.FactDate.Overlaps(lcFact.FactDate));
        }

        public void FixIndividualID(int length)
        {
            try
            {
                IndividualID = IndividualID.Substring(0, 1) + IndividualID.Substring(1).PadLeft(length, '0');
            }
            catch (Exception)
            {  // don't error if Individual isn't of type Ixxxx
            }
        }

        #region Colour Census 
        CensusColour ColourCensusReport(CensusDate census)
        {
            if (BirthDate.IsAfter(census) || DeathDate.IsBefore(census) || GetAge(census).MinAge >= FactDate.MAXYEARS)
                return CensusColour.NOT_ALIVE; // not alive - grey
            if (!IsCensusDone(census))
            {
                if (IsTaggedMissingCensus(census))
                    return CensusColour.KNOWN_MISSING;
                if (IsCensusDone(census, true, false) || (Countries.IsUnitedKingdom(census.Country) && IsCensusDone(census.EquivalentUSCensus, true, false)))
                    return CensusColour.OVERSEAS_CENSUS; // checks if on census outside UK in census year or on prior year (to check US census)
                FactLocation location = BestLocation(census);
                if (CensusDate.IsLostCousinsCensusYear(census, true) && IsLostCousinsEntered(census) && !OutOfCountryCheck(census, location))
                    return CensusColour.LC_PRESENT_NO_CENSUS; // LC entered but no census entered - orange
                if (location.IsKnownCountry)
                {
                    if (OutOfCountryCheck(census, location))
                        return CensusColour.OUT_OF_COUNTRY; // Likely out of country on census date
                    return CensusColour.NO_CENSUS; // no census - red
                }
                return CensusColour.NO_CENSUS; // no census - red
            }
            if (!CensusDate.IsLostCousinsCensusYear(census, true))
                return CensusColour.CENSUS_PRESENT_NOT_LC_YEAR; // census entered but not LCyear - green
            if (IsLostCousinsEntered(census))
                return CensusColour.CENSUS_PRESENT_LC_PRESENT; // census + Lost cousins entered - green
                // we have a census in a LC year but no LC event check if country is a LC country.
            int year = census.StartDate.Year;
            if (year == 1841 && IsCensusDone(CensusDate.EWCENSUS1841, false))
                return CensusColour.CENSUS_PRESENT_LC_MISSING; // census entered LC not entered - yellow
            if (year == 1880 && IsCensusDone(CensusDate.USCENSUS1880, false))
                return CensusColour.CENSUS_PRESENT_LC_MISSING; // census entered LC not entered - yellow
            if (year == 1881 &&
                (IsCensusDone(CensusDate.EWCENSUS1881, false) || IsCensusDone(CensusDate.CANADACENSUS1881, false) ||
                 IsCensusDone(CensusDate.SCOTCENSUS1881, false)))
                return CensusColour.CENSUS_PRESENT_LC_MISSING; // census entered LC not entered - yellow
            if (year == 1911 && (IsCensusDone(CensusDate.EWCENSUS1911, false) || IsCensusDone(CensusDate.IRELANDCENSUS1911, false)))
                return CensusColour.CENSUS_PRESENT_LC_MISSING; // census entered LC not entered - yellow
            if (year == 1940 && IsCensusDone(CensusDate.USCENSUS1940, false))
                return CensusColour.CENSUS_PRESENT_LC_MISSING; // census entered LC not entered - yellow
            return CensusColour.CENSUS_PRESENT_NOT_LC_YEAR;  // census entered and LCyear but not LC country - green
        }

        public bool AliveOnAnyCensus(string country)
        {
            int ukCensus = (int)C1841 + (int)C1851 + (int)C1861 + (int)C1871 + (int)C1881 + (int)C1891 + (int)C1901 + (int)C1911 + (int)C1939;
            if (country.Equals(Countries.UNITED_STATES))
                return ((int)US1790 + (int)US1800 + (int)US1810 + (int)US1810 + (int)US1820 + (int)US1830 + (int)US1840 + (int)US1850 + (int)US1860 + (int)US1870 + (int)US1880 + (int)US1890 + (int)US1900 + (int)US1910 + (int)US1920 + (int)US1930 + (int)US1940) > 0;
            if (country.Equals(Countries.CANADA))
                return ((int)Can1851 + (int)Can1861 + (int)Can1871 + (int)Can1881 + (int)Can1891 + (int)Can1901 + (int)Can1906 + (int)Can1911 + (int)Can1916 + (int)Can1921) > 0;
            if (country.Equals(Countries.IRELAND))
                return ((int)Ire1901 + (int)Ire1911) > 0;
            if (country.Equals(Countries.SCOTLAND))
                return (ukCensus + (int)V1865 + (int)V1875 + (int)V1885 + (int)V1895 + (int)V1905 + (int)V1915 + (int)V1920 + (int)V1925) > 0;
            return ukCensus > 0;
        }

        public bool OutOfCountryOnAllCensus(string country)
        {
            if (country.Equals(Countries.UNITED_STATES))
                return CheckOutOfCountry("US1");
            if (country.Equals(Countries.CANADA))
                return CheckOutOfCountry("Can1");
            if (country.Equals(Countries.IRELAND))
                return CheckOutOfCountry("Ire1");
            return CheckOutOfCountry("C1");
        }

        public bool OutOfCountryCheck(CensusDate census, FactLocation location)
        {
            return (Countries.IsUnitedKingdom(census.Country) && !location.IsUnitedKingdom) ||
                  (!Countries.IsUnitedKingdom(census.Country) && census.Country != location.Country);
        }

        public bool OutOfCountry(CensusDate census)
        {
            return CheckOutOfCountry(census.PropertyName);
        }

        bool CheckOutOfCountry(string prefix)
        {
            foreach (PropertyInfo property in typeof(Individual).GetProperties())
            {
                if (property.Name.StartsWith(prefix, StringComparison.Ordinal))
                {
                    int value = (int)property.GetValue(this, null);
                    if (value != 0 && value != 6 && value != 7)
                        return false;
                }
            }
            return true;
        }
        #endregion

        #region Colour Census Values
        public CensusColour C1841 => ColourCensusReport(CensusDate.UKCENSUS1841);

        public CensusColour C1851 => ColourCensusReport(CensusDate.UKCENSUS1851);

        public CensusColour C1861 => ColourCensusReport(CensusDate.UKCENSUS1861);

        public CensusColour C1871 => ColourCensusReport(CensusDate.UKCENSUS1871);

        public CensusColour C1881 => ColourCensusReport(CensusDate.UKCENSUS1881);

        public CensusColour C1891 => ColourCensusReport(CensusDate.UKCENSUS1891);

        public CensusColour C1901 => ColourCensusReport(CensusDate.UKCENSUS1901);

        public CensusColour C1911 => ColourCensusReport(CensusDate.UKCENSUS1911);

        public CensusColour C1939 => ColourCensusReport(CensusDate.UKCENSUS1939);

        public CensusColour Ire1901 => ColourCensusReport(CensusDate.IRELANDCENSUS1901);

        public CensusColour Ire1911 => ColourCensusReport(CensusDate.IRELANDCENSUS1911);

        public CensusColour US1790 => ColourCensusReport(CensusDate.USCENSUS1790);

        public CensusColour US1800 => ColourCensusReport(CensusDate.USCENSUS1800);

        public CensusColour US1810 => ColourCensusReport(CensusDate.USCENSUS1810);

        public CensusColour US1820 => ColourCensusReport(CensusDate.USCENSUS1820);

        public CensusColour US1830 => ColourCensusReport(CensusDate.USCENSUS1830);

        public CensusColour US1840 => ColourCensusReport(CensusDate.USCENSUS1840);

        public CensusColour US1850 => ColourCensusReport(CensusDate.USCENSUS1850);

        public CensusColour US1860 => ColourCensusReport(CensusDate.USCENSUS1860);

        public CensusColour US1870 => ColourCensusReport(CensusDate.USCENSUS1870);

        public CensusColour US1880 => ColourCensusReport(CensusDate.USCENSUS1880);

        public CensusColour US1890 => ColourCensusReport(CensusDate.USCENSUS1890);

        public CensusColour US1900 => ColourCensusReport(CensusDate.USCENSUS1900);

        public CensusColour US1910 => ColourCensusReport(CensusDate.USCENSUS1910);

        public CensusColour US1920 => ColourCensusReport(CensusDate.USCENSUS1920);

        public CensusColour US1930 => ColourCensusReport(CensusDate.USCENSUS1930);

        public CensusColour US1940 => ColourCensusReport(CensusDate.USCENSUS1940);

        public CensusColour Can1851 => ColourCensusReport(CensusDate.CANADACENSUS1851);

        public CensusColour Can1861 => ColourCensusReport(CensusDate.CANADACENSUS1861);

        public CensusColour Can1871 => ColourCensusReport(CensusDate.CANADACENSUS1871);

        public CensusColour Can1881 => ColourCensusReport(CensusDate.CANADACENSUS1881);

        public CensusColour Can1891 => ColourCensusReport(CensusDate.CANADACENSUS1891);

        public CensusColour Can1901 => ColourCensusReport(CensusDate.CANADACENSUS1901);

        public CensusColour Can1906 => ColourCensusReport(CensusDate.CANADACENSUS1906);

        public CensusColour Can1911 => ColourCensusReport(CensusDate.CANADACENSUS1911);

        public CensusColour Can1916 => ColourCensusReport(CensusDate.CANADACENSUS1916);

        public CensusColour Can1921 => ColourCensusReport(CensusDate.CANADACENSUS1921);

        public CensusColour V1865 => ColourCensusReport(CensusDate.SCOTVALUATION1865);

        public CensusColour V1875 => ColourCensusReport(CensusDate.SCOTVALUATION1875);

        public CensusColour V1885 => ColourCensusReport(CensusDate.SCOTVALUATION1885);

        public CensusColour V1895 => ColourCensusReport(CensusDate.SCOTVALUATION1895);

        public CensusColour V1905 => ColourCensusReport(CensusDate.SCOTVALUATION1905);

        public CensusColour V1915 => ColourCensusReport(CensusDate.SCOTVALUATION1915);

        public CensusColour V1920 => ColourCensusReport(CensusDate.SCOTVALUATION1920);

        public CensusColour V1925 => ColourCensusReport(CensusDate.SCOTVALUATION1925);
        #endregion

        #region Colour BMD Values

        public BMDColour Birth => BirthDate.DateStatus(false);

        public BMDColour BaptChri
        {
            get
            {
                FactDate baptism = GetPreferredFactDate(Fact.BAPTISM);
                FactDate christening = GetPreferredFactDate(Fact.CHRISTENING);
                BMDColour baptismStatus = baptism.DateStatus(true);
                BMDColour christeningStatus = christening.DateStatus(true);
                if (baptismStatus.Equals(BMDColour.EMPTY))
                    return christeningStatus;
                if (christeningStatus.Equals(BMDColour.EMPTY))
                    return baptismStatus;
                return (int)baptismStatus < (int)christeningStatus ? baptismStatus : christeningStatus;
            }
        }

        BMDColour CheckMarriageStatus(Family fam)
        {
            // individual is a member of a family as parent so check family status
            if ((IndividualID == fam.HusbandID && fam.Wife == null) ||
                (IndividualID == fam.WifeID && fam.Husband == null))
                return BMDColour.NO_PARTNER; // no partner but has children
            if (fam.GetPreferredFact(Fact.MARRIAGE) == null)
                return BMDColour.NO_MARRIAGE; // has a partner but no marriage fact
            return fam.MarriageDate.DateStatus(false); // has a partner and a marriage so return date status
        }

        public BMDColour Marriage1
        {
            get
            {
                Family fam = Marriages(0);
                if (fam == null)
                {
                    if (MaxAgeAtDeath > 13 && GetPreferredFact(Fact.UNMARRIED) == null)
                        return BMDColour.NO_SPOUSE; // of marrying age but hasn't a partner or unmarried
                    return BMDColour.EMPTY;
                }
                return CheckMarriageStatus(fam);
            }
        }

        public BMDColour Marriage2
        {
            get
            {
                Family fam = Marriages(1);
                return fam == null ? BMDColour.EMPTY : CheckMarriageStatus(fam);
            }
        }

        public BMDColour Marriage3
        {
            get
            {
                Family fam = Marriages(2);
                return fam == null ? 0 : CheckMarriageStatus(fam);
            }
        }

        public string FirstMarriage => MarriageString(0);

        public string SecondMarriage => MarriageString(1);

        public string ThirdMarriage => MarriageString(2);

        public FactDate FirstMarriageDate
        {
            get
            {
                Family fam = Marriages(0);
                return fam == null ? FactDate.UNKNOWN_DATE : Marriages(0).MarriageDate;
            }
        }

        public FactDate SecondMarriageDate
        {
            get
            {
                Family fam = Marriages(1);
                return fam == null ? FactDate.UNKNOWN_DATE : Marriages(1).MarriageDate;
            }
        }

        public FactDate ThirdMarriageDate
        {
            get
            {
                Family fam = Marriages(2);
                return fam == null ? FactDate.UNKNOWN_DATE : Marriages(2).MarriageDate;
            }
        }

        public BMDColour Death
        {
            get
            {
                if (IsFlaggedAsLiving)
                    return BMDColour.ISLIVING;
                if (!DeathDate.IsKnown && GetMaxAge(DateTime.Now) < FactDate.MAXYEARS)
                    return GetMaxAge(DateTime.Now) < 90 ? BMDColour.EMPTY : BMDColour.OVER90;
                return DeathDate.DateStatus(false);
            }
        }

        public BMDColour CremBuri
        {
            get
            {
                FactDate cremation = GetPreferredFactDate(Fact.CREMATION);
                FactDate burial = GetPreferredFactDate(Fact.BURIAL);
                BMDColour cremationStatus = cremation.DateStatus(true);
                BMDColour burialStatus = burial.DateStatus(true);
                if (cremationStatus.Equals(BMDColour.EMPTY))
                    return burialStatus;
                if (burialStatus.Equals(BMDColour.EMPTY))
                    return cremationStatus;
                return (int)cremationStatus < (int)burialStatus ? cremationStatus : burialStatus;
            }
        }

        #endregion

        public float Score
        {
            get { return 0.0f; }
            // TODO Add scoring mechanism
        }

        public int LostCousinsCensusFactCount => Facts.Count(f => f.IsLCCensusFact);

        public int CensusFactCount => Facts.Count(f => f.IsCensusFact);

        public int CensusDateFactCount(CensusDate censusDate) => Facts.Count(f => f.IsValidCensus(censusDate));

        public bool IsLivingError => IsFlaggedAsLiving && DeathDate.IsKnown;

        public int CensusReferenceCount(CensusReference.ReferenceStatus referenceStatus) 
            => AllFacts.Count(f => f.IsCensusFact && f.CensusReference != null && f.CensusReference.Status.Equals(referenceStatus));

        Family Marriages(int number)
        {
            if (number < FamiliesAsParent.Count)
            {
                Family f = FamiliesAsParent.OrderBy(d => d.MarriageDate).ElementAt(number);
                return f;
            }
            return null;
        }

        string MarriageString(int number)
        {
            Family marriage = Marriages(number);
            if (marriage == null)
                return string.Empty;
            if (IndividualID == marriage.HusbandID && marriage.Wife != null)
                return $"To {marriage.Wife.Name}: {marriage.ToString()}";
            if (IndividualID == marriage.WifeID && marriage.Husband != null)
                return $"To {marriage.Husband.Name}: {marriage.ToString()}";
            return $"Married: {marriage.ToString()}";
        }

        public int NumMissingLostCousins(string country)
        {
            if (!AliveOnAnyCensus(country)) return 0;
            int numMissing = CensusDate.LOSTCOUSINS_CENSUS.Count(x => IsCensusDone(x) && !IsLostCousinsEntered(x));
            return numMissing;
        }

        #region Basic Class Functions
        public override bool Equals(object obj)
        {
            return obj is Individual && IndividualID.Equals(((Individual)obj).IndividualID);
        }

        public override int GetHashCode() => base.GetHashCode();

        public override string ToString() => $"{IndividualID}: {Name} b.{BirthDate}";

        public int CompareTo(Individual that)
        {
            // Individuals are naturally ordered by surname, then forenames,
            // then date of birth.
            if (that == null)
                return -1;
            int res = string.Compare(Surname, that.Surname, StringComparison.CurrentCulture);
            if (res == 0)
            {
                res = string.Compare(_forenames, that._forenames, StringComparison.Ordinal);
                if (res == 0)
                {
                    FactDate d1 = BirthDate;
                    FactDate d2 = that.BirthDate;
                    res = d1.CompareTo(d2);
                }
            }
            return res;
        }
        #endregion
    }
}
