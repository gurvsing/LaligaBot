using Microsoft.Bot.Builder;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BasicBot
{
    public struct FindMatchResponse
    {
        public string MatchDate { get; set; }

        public string Home { get; set; }

        public string Away { get; set; }

        public string Team { get; set; }

        public string ResponseText { get; set; }
    }
    public static class LaLigaBL
    {
        public static int TicketPrice = 50;
        public static Dictionary<string, string> matches = new Dictionary<string, string>();
        public static Dictionary<string, string> stadiums = new Dictionary<string, string>() {
            {"Barcelona" , "Camp Nou" }, {"Real Madrid", "Santiago Bernabeu" }, {"Atletico Madrid", "Wanda Metropolitan"},
            {"Real Betis", "Benito Villamarin"}, {"Leganes", "Butarque"}, {"Levante", "Ciutat de Valencia"}, {"Getafe", "Coliseum Alfonso Perez"},
            {"Villareal", "Estadio de la Ceramica"}, {"Huesca", "El Alcoraz" }, {"Eibar", "Ipurua"}, {"Valladolid", "Jose Zorrilla"}, {"Alaves", "Mendizorrotta"},
            {"Valencia", "Mestalla" }, {"Girona", "Montilivi"}, {"Sevilla", "Ramón Sánchez Pizjuán"}, {"Athletic Bilbao", "San Mames"}, {"Celta Vigo", "Balaidos"},
            {"Espanyol", "RCDE Stadium" }, {"Rayo Vallecano", "Vallecas"}, {"Real Sociedad", "Anoeta"}
        };
        public static readonly DateTime endDate = new DateTime(2019, 5, 15);
        public static readonly DateTime startDate = new DateTime(2018, 7, 15);

        public static FindMatchResponse FindMatch(LuisResponse luisResults)
        {
            string finalDate;
            string finalMatchDescription = "";
            var home = luisResults.Entities.ContainsKey("Home") ? luisResults.Entities["Home"] : null;
            var away = luisResults.Entities.ContainsKey("Away") ? luisResults.Entities["Away"] : null;
            var team = luisResults.Entities.ContainsKey("Team") ? luisResults.Entities["Team"] : null;

            finalDate = GetMatchDate();

            if (home != null && away == null && team != null && home != team)
                away = team;
            else if (home == null && away != null && team != null && away != team)
                home = team;


            if (home != null && away != null)
            {
                if (!matches.ContainsKey(home + away))
                {
                    matches[home + away] = finalDate;
                    finalMatchDescription = $"{home} will play {away} on {finalDate} at their stadium {stadiums[home]}";
                }
                else
                {
                    finalDate = matches[home + away];
                    finalMatchDescription = $"{home} will play {away} on {finalDate} at their stadium {stadiums[home]}";
                }
            }
            else if ((team != null && home == null && away == null) || (home != null && away == null) || (home == null && away != null))
            {
                var singleTeam = team ?? home ?? away;
                if (matches.ContainsKey("singleTeam"))
                    finalDate = matches[singleTeam];
                else
                    matches[singleTeam] = finalDate;
                finalMatchDescription = $"{singleTeam} will play their next home game on {finalDate} at their stadium {stadiums[singleTeam] }"; 

            }
            var findMatchResult = new FindMatchResponse()
            {
                Home = home,
                Away = away,
                MatchDate = finalDate,
                ResponseText = finalMatchDescription,
                Team = team,
            };

            return findMatchResult;
        }

        public static string PurchaseTicket(LuisResponse luisResults)
        {
            var finalResponse = "";
            FindMatchResponse findMatchResponse = FindMatch(luisResults);
            var home = findMatchResponse.Home;
            var away = findMatchResponse.Away;
            var team = findMatchResponse.Team;
            var number = luisResults.Entities["number"] ?? null;

            if(home != null && away != null)
            {
                var ticketNumber = 1;
                if (number != null)
                    ticketNumber = int.Parse(number);
                var ticketString = ticketNumber > 1 ? "tickets" : "ticket";
                finalResponse = $"You have chosen to purchase {ticketNumber} {ticketString} for the {home} vs {away} game at {stadiums[home]} taking place on {findMatchResponse.MatchDate}";
            }
            return "";
        }

        public static string GetMatchDate()
        {
            TimeSpan timeSpan = endDate - startDate;
            var randomTest = new Random();
            TimeSpan newSpan = new TimeSpan(0, randomTest.Next(0, (int)timeSpan.TotalMinutes), 0);
            DateTime newDate = startDate + newSpan;
            return newDate.ToShortDateString();
        }
    }
}
