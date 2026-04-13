using System;
using System.Collections.Generic;
using Backoffice.Shared.Enums;

namespace Backoffice.Shared.Utilities
{ public static class SportHelper
    {
        public static readonly IReadOnlyDictionary<Sport, string> Names = new Dictionary<Sport, string>
        {
            { Sport.ThreeXThreeBasketball, "3x3 Basketball" },
            { Sport.AmericanFootball, "American Football" },
            { Sport.AustralianRules, "Australian Rules" },
            { Sport.Badminton, "Badminton" },
            { Sport.Baseball, "Baseball" },
            { Sport.Basketball, "Basketball" },
            { Sport.BeachVolleyball, "Beach Volleyball" },
            { Sport.Boxing, "Boxing" },
            { Sport.Cricket, "Cricket" },
            { Sport.Cycling, "Cycling" },
            { Sport.Darts, "Darts" },
            { Sport.Esports, "eSports" },
            { Sport.Soccer, "Football" },
            { Sport.Futsal, "Futsal" },
            { Sport.GaelicFootball, "Gaelic Football" },
            { Sport.Golf, "Golf" },
            { Sport.Handball, "Handball" },
            { Sport.Hockey, "Hockey" },
            { Sport.HorseRacing, "Horse Racing" },
            { Sport.Hurling, "Hurling" },
            { Sport.IceHockey, "Ice Hockey" },
            { Sport.MartialArtsUFC, "Martial Arts/UFC" },
            { Sport.MotorSport, "Motor Sport" },
            { Sport.RugbyLeague, "Rugby League" },
            { Sport.RugbyUnion, "Rugby Union" },
            { Sport.Snooker, "Snooker" },
            { Sport.Squash, "Squash" },
            { Sport.TableTennis, "Table Tennis" },
            { Sport.Tennis, "Tennis" },
            { Sport.Volleyball, "Volleyball" }
        };

        public static bool IsVirtualSportId(int sportId)
        {
            var listOfVirtualSports = new List<int>();

            listOfVirtualSports.Add(11205870);
            //todo: append virtual sports here

            return listOfVirtualSports.Contains(sportId);
        }



    }
}
