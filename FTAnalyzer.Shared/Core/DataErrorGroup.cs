﻿using System.Collections.Generic;

namespace FTAnalyzer
{
    public class DataErrorGroup
    {
        static readonly string[] DATAERROR = 
            { "Birth after death/burial",
                "Birth after father aged 90+", 
                "Birth after mother aged 60+", 
                "Birth after mother's death", 
                "Birth more than 9m after father's death",
                "Birth before father aged 13", 
                "Birth before mother aged 13", 
                "Burial before death", 
                $"Aged more than {FactDate.MAXYEARS} at death", 
                "Facts dated before birth", 
                "Facts dated after death", 
                "Marriage after death", 
                "Marriage after spouse's death", 
                "Marriage before aged 13", 
                "Marriage before spouse aged 13", 
                "Lost Cousins tag in non Census Year", 
                "Lost Cousins tag in non supported Year",
                "Residence census date warnings",
                "Census date range too wide/Unknown",
                "Fact warning/error detected",
                "Unknown Fact Type",
                "Flagged as living but has death date",
                "Children Status Total Mismatch",
                "Duplicate Fact",
                "Possible Duplicate Fact",
                "On 1939 Register no precise birthdate",
                "Male Wifes and Female Husbands",
                "Couples with same surnames"
            };

        //"Later marriage before previous spouse died"

        public IList<DataError> Errors { get; private set; }
        readonly int errorNumber;

        public static string ErrorDescription(int errorNumber)
        {
            return DATAERROR[errorNumber];
        }

        public DataErrorGroup(int errorNumber, IList<DataError> errors)
        {
            this.errorNumber = errorNumber;
            Errors = errors;
        }

        public override string ToString()
        {
            return ErrorDescription(errorNumber);
        }
    }
}
