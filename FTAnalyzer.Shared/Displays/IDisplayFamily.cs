﻿namespace FTAnalyzer
{
    public interface IDisplayFamily
    {
        string FamilyID { get; }
        string HusbandID { get; }
        string Husband { get; }
        string WifeID { get; }
        string Wife { get; }
        string Marriage { get; }
        FactLocation Location { get; }
        string Children { get; }
        int FamilySize { get; }
    }
}
