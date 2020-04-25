﻿using System;

namespace FTAnalyzer
{
    public class DuplicateIndividual
    {
        public Individual IndividualA { get; private set; }
        public Individual IndividualB { get; private set; }
        public int Score { get; private set; }

        public DuplicateIndividual(Individual a, Individual b)
        {
            IndividualA = a;
            IndividualB = b;
            CalculateScore();
        }

        public void CalculateScore()
        {
            Score = 0;
            Score += NameScore(IndividualA, IndividualB);
            ScoreDates(IndividualA.BirthDate, IndividualB.BirthDate);
            ScoreDates(IndividualA.DeathDate, IndividualB.DeathDate);
            LocationScore();
            Score += SharedParents() + SharedChildren() + DifferentParentsPenalty();
        }

        int NameScore(Individual indA, Individual indB)
        {
            int score = 0;
            if (indA != null && indB != null)
            {
                if (indA.Surname.Equals(indB.Surname, StringComparison.OrdinalIgnoreCase) && indA.Surname != Individual.UNKNOWN_NAME)
                    score += 20;
                if (indA.Forename.Equals(indB.Forename, StringComparison.OrdinalIgnoreCase) && indA.Forename != Individual.UNKNOWN_NAME)
                    score += 20;
            }
            return score;
        }

        void LocationScore()
        {
            if (IndividualA.BirthLocation.IsBlank || IndividualB.BirthLocation.IsBlank)
                return;
            if (IndividualA.BirthLocation.Equals(IndividualB.BirthLocation))
                Score += 75;
            if (IndividualA.BirthLocation.Country.Equals(IndividualB.BirthLocation.Country, StringComparison.OrdinalIgnoreCase))
                Score += 10;
            if (IndividualA.BirthLocation.Region.Equals(IndividualB.BirthLocation.Region, StringComparison.OrdinalIgnoreCase))
                Score += 10;
            if (IndividualA.BirthLocation.SubRegion.Equals(IndividualB.BirthLocation.SubRegion, StringComparison.OrdinalIgnoreCase))
                Score += 20;
            if (IndividualA.BirthLocation.Address.Equals(IndividualB.BirthLocation.Address, StringComparison.OrdinalIgnoreCase))
                Score += 40;
            if (IndividualA.BirthLocation.Place.Equals(IndividualB.BirthLocation.Place, StringComparison.OrdinalIgnoreCase))
                Score += 40;

            if (IndividualA.BirthLocation.CountryMetaphone.Equals(IndividualB.BirthLocation.CountryMetaphone, StringComparison.OrdinalIgnoreCase))
                Score += 5;
            if (IndividualA.BirthLocation.RegionMetaphone.Equals(IndividualB.BirthLocation.RegionMetaphone, StringComparison.OrdinalIgnoreCase))
                Score += 5;
            if (IndividualA.BirthLocation.SubRegionMetaphone.Equals(IndividualB.BirthLocation.SubRegionMetaphone, StringComparison.OrdinalIgnoreCase))
                Score += 10;
            if (IndividualA.BirthLocation.AddressMetaphone.Equals(IndividualB.BirthLocation.AddressMetaphone, StringComparison.OrdinalIgnoreCase))
                Score += 20;
            if (IndividualA.BirthLocation.PlaceMetaphone.Equals(IndividualB.BirthLocation.PlaceMetaphone, StringComparison.OrdinalIgnoreCase))
                Score += 20;

            if (IndividualA.BirthLocation.IsKnownCountry && IndividualB.BirthLocation.IsKnownCountry &&
                !IndividualA.BirthLocation.Country.Equals(IndividualB.BirthLocation.Country, StringComparison.OrdinalIgnoreCase))
                Score -= 250;
        }

        void ScoreDates(FactDate dateA, FactDate dateB)
        {
            if (dateA.IsKnown && dateB.IsKnown)
            {
                double distance = dateA.Distance(dateB);
                if (dateA.Equals(dateB))
                    Score += 50;
                else if (distance <= .25)
                    Score += 50;
                else if (distance <= .5)
                    Score += 20;
                else if (distance <= 1)
                    Score += 10;
                else if (distance <= 2)
                    Score += 5;
                else if (distance > 5 && distance < 20)
                    Score -= (int)(distance * distance);
                else
                    Score = -10000;  // distance is too big so set score to large negative
                if (dateA.IsExact && dateB.IsExact)
                    Score += 100;
            }
        }

        int SharedParents()
        {
            int score = 0;
            foreach (ParentalRelationship parentA in IndividualA.FamiliesAsChild)
            {
                foreach (ParentalRelationship parentB in IndividualB.FamiliesAsChild)
                {
                    if (parentA.Father == parentB.Father)
                        score += 50;
                    else
                        score += NameScore(parentA.Father, parentB.Father);
                    if (parentA.Mother == parentB.Mother)
                        score += 50;
                    else
                        score += NameScore(parentA.Mother, parentB.Mother);
                }
            }
            return score;
        }

        int DifferentParentsPenalty()
        {
            int score = 0;
            if (IndividualA.FamiliesAsChild.Count == 1 && IndividualB.FamiliesAsChild.Count == 1)
            { // both individuals have parents if none of them are shared parents apply a heavy penalty
                if (IndividualA.FamiliesAsChild[0].Father != null && IndividualA.FamiliesAsChild[0].Mother != null &&
                    IndividualB.FamiliesAsChild[0].Father != null && IndividualB.FamiliesAsChild[0].Mother != null &&
                    !IndividualA.FamiliesAsChild[0].Father.Equals(IndividualB.FamiliesAsChild[0].Father) &&
                    !IndividualA.FamiliesAsChild[0].Mother.Equals(IndividualB.FamiliesAsChild[0].Mother))
                    score = -500;
            }
            else if (IndividualA.FamiliesAsChild.Count > 0 && IndividualB.FamiliesAsChild.Count > 0)
            {
                if (SharedParents() == 0)
                    score = -250;
            }
            return score;
        }

        int SharedChildren()
        {
            int score = 0;
            foreach (Family familyA in IndividualA.FamiliesAsSpouse)
            {
                foreach (Family familyB in IndividualB.FamiliesAsSpouse)
                {
                    foreach (Individual familyBchild in familyB.Children)
                        if (familyA.Children.Contains(familyBchild))
                            score += 50;
                }
            }
            return score;
        }

        public override bool Equals(object obj)
        {
            if (obj is DuplicateIndividual)
                return (IndividualA.Equals(((DuplicateIndividual)obj).IndividualA) && IndividualB.Equals(((DuplicateIndividual)obj).IndividualB))
                    || (IndividualA.Equals(((DuplicateIndividual)obj).IndividualB) && IndividualB.Equals(((DuplicateIndividual)obj).IndividualA));
            return false;
        }

        public override int GetHashCode() => base.GetHashCode();
    }
}
