using Microsoft.Bot.Builder;
using Newtonsoft.Json;
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
        public static readonly int TicketPrice = 50;
        public static Dictionary<string, string> matches = new Dictionary<string, string>();
        public static Dictionary<string, string> stadiums = new Dictionary<string, string>() {
            {"Barcelona" , "Camp Nou" }, {"Real Madrid", "Santiago Bernabeu" }, {"Ath Madrid", "Wanda Metropolitan"},
            {"Betis", "Benito Villamarin"}, {"Leganes", "Butarque"}, {"Levante", "Ciutat de Valencia"}, {"Getafe", "Coliseum Alfonso Perez"},
            {"Villareal", "Estadio de la Ceramica"}, {"Huesca", "El Alcoraz" }, {"Eibar", "Ipurua"}, {"Valladolid", "Jose Zorrilla"}, {"Alaves", "Mendizorrotta"},
            {"Valencia", "Mestalla" }, {"Girona", "Montilivi"}, {"Sevilla", "Ramón Sánchez Pizjuán"}, {"Ath Bilbao", "San Mames"}, {"Celta", "Balaidos"},
            {"Espanyol", "RCDE Stadium" }, {"Vallecano", "Vallecas"}, {"Sociedad", "Anoeta"}
        };
        public static List<MatchJSON> AllGames = JsonConvert.DeserializeObject<List<MatchJSON>>(File.ReadAllLines("LaLigaData.json")[0]);
        public static List<MatchJSON> FutureGames = JsonConvert.DeserializeObject<List<MatchJSON>>(File.ReadAllLines("LaLigaRest.json")[0]);
        public static List<MatchJSON> ChampionsLeagueGames = JsonConvert.DeserializeObject<List<MatchJSON>>(File.ReadAllLines("ChampionsLeagueGames.json")[0]);
        public static readonly DateTime endDate = new DateTime(2019, 5, 15);
        public static readonly DateTime startDate = new DateTime(2018, 7, 15);
        public static HashSet<string> Teams;
        public static string CoreferenceHome;
        public static string CoreferenceAway;
        public static bool IsChampionsLeague = false;

        public static FindMatchResponse FindMatch(LuisResponse luisResults)
        {
            MatchObject matchInfo;
            string finalMatchDescription = "";
            var home = luisResults.Entities.ContainsKey("Home") ? luisResults.Entities["Home"] : null;
            var away = luisResults.Entities.ContainsKey("Away") ? luisResults.Entities["Away"] : null;
            var team = luisResults.Entities.ContainsKey("Team") ? luisResults.Entities["Team"] : null;
            var relative = luisResults.Entities.ContainsKey("Relative") ? luisResults.Entities["Relative"] : null;

            if (home != null && away == null && team != null && home != team)
                away = team;
            else if (home == null && away != null && team != null && away != team)
                home = team;

            matchInfo = GetMatchInfo(home, away, team, relative);

            var findMatchResult = new FindMatchResponse()
            {
                Home = home,
                Away = away,
                MatchDate = matchInfo.MatchDate,
                ResponseText = matchInfo.MatchDescription,
                Team = team,
            };
            LuisServiceV3.CoreferenceHome = team ?? home ?? away;
            LaLigaBL.IsChampionsLeague = false;
            return findMatchResult;
        }

        public static string PurchaseTicket(LuisResponse luisResults)
        {
            var finalResponse = "";
            FindMatchResponse findMatchResponse = FindMatch(luisResults);
            var home = findMatchResponse.Home;
            var away = findMatchResponse.Away;
            var team = findMatchResponse.Team;
            var number = luisResults.Entities.ContainsKey("number") ? luisResults.Entities["number"] : null;
            var ticketNumber = 1;
            if (number != null)
                ticketNumber = int.Parse(number);
            var ticketString = ticketNumber > 1 ? "tickets" : "ticket";


            if (home != null && away != null)
            {
                finalResponse = $"You have chosen to purchase {ticketNumber} {ticketString} for the {home} vs {away} game at {stadiums[home]} taking place on {findMatchResponse.MatchDate}. The total price of the tickets is {ticketNumber * TicketPrice}. Would you like to proceed to payment?";
            }
            else if ((team != null && home == null && away == null) || (home != null && away == null) || (home == null && away != null))
            {
                string finalDate = "";
                var singleTeam = team ?? home ?? away;
                if (matches.ContainsKey("singleTeam"))
                    finalDate = matches[singleTeam];
                else
                    matches[singleTeam] = finalDate;
                finalResponse = $"You have chosen to purchase {ticketNumber} {ticketString} for the {singleTeam}'s next game at {stadiums[singleTeam]} taking place on {findMatchResponse.MatchDate}. The total price of the tickets is {ticketNumber * TicketPrice}. Would you like to proceed to payment?";
            };
            return finalResponse;
        }

        public static MatchObject GetMatchInfo(string home, string away, string team, string relative = "Previous")
        {
            var matchObject = new MatchObject();
            if (IsChampionsLeague)
            {

            }
            if(home != null && away != null && home != away)
            {
                var Game = AllGames.Where(match => match.HomeTeam == home && match.AwayTeam == away).SingleOrDefault();
                if (Game != null)
                {
                    matchObject = GetMatchObject(Game, home, away);
                }
                else
                {
                    if(IsChampionsLeague)
                        Game = ChampionsLeagueGames.Where(match => match.HomeTeam == home && match.AwayTeam == away).SingleOrDefault();
                    else
                        Game = FutureGames.Where(match => match.HomeTeam == home && match.AwayTeam == away).SingleOrDefault();

                    if(Game == null)
                    {
                        matchObject.MatchDescription = "Sorry we cannot find the match you requested";
                        return matchObject;
                    }
                    matchObject.AwayTeam = away;
                    matchObject.HomeTeam = home;
                    matchObject.MatchDate = Game.Date;
                    matchObject.MatchDescription = $"{home} will be playing at home against {away} on {matchObject.MatchDate}.\n The game will be played at {stadiums[home]}";
                };
                return matchObject;
            }
            else
            {
                var singleTeam = team ?? home ?? away;
                if (relative == "Previous")
                {
                    var Game = AllGames.Where(match => match.AwayTeam == singleTeam || match.HomeTeam == singleTeam).Last();
                    if (Game != null)
                    {
                        matchObject = GetMatchObject(Game, Game.HomeTeam, Game.AwayTeam);
                        return matchObject;
                    }
                    else
                    {
                        matchObject.MatchDescription = "Sorry we cannot find the match you requested";
                        return matchObject;
                    }
                }
                else
                {
                    MatchJSON Game;

                    if(IsChampionsLeague)
                        Game = ChampionsLeagueGames.Where(match => match.HomeTeam == singleTeam || match.AwayTeam == singleTeam).FirstOrDefault();
                    else
                        Game = FutureGames.Where(match => match.HomeTeam == singleTeam || match.AwayTeam == singleTeam).FirstOrDefault();

                    matchObject.HomeTeam = Game.HomeTeam;
                    matchObject.AwayTeam = Game.AwayTeam;
                    var awayOrHome = singleTeam == matchObject.HomeTeam ? "at home against" : "away against";
                    var otherTeam = singleTeam == matchObject.HomeTeam ? Game.AwayTeam : Game.HomeTeam;
                    matchObject.MatchDescription = $"{singleTeam}'s next game will be {awayOrHome} {otherTeam} on {Game.Date}.\n The game will played at the {Game.HomeTeam}'s stadium {stadiums[Game.HomeTeam]}.";
                }

            }
            return matchObject;
        }


        public static MatchObject GetMatchObject(MatchJSON Game, string home, string away)
        {
            var matchObject = new MatchObject();
            matchObject.AwayTeam = away;
            matchObject.HomeTeam = home;
            matchObject.MatchDate = Game.Date;
            matchObject.WinningTeam = Game.FTR == "H" ? home : Game.FTR == "A" ? away : null;
            var result = Game.FTR == "H" ? $"a home win for {home}" : Game.FTR == "A" ? $"an away win for {away}" : "a draw";
            matchObject.MatchResult = $"{Game.FTHG} - {Game.FTAG}";
            matchObject.MatchDescription = $"{home} played at Home against {away} on {Game.Date} at {stadiums[home]}.\nThe game ended with {result}. \n The score was {home} {Game.FTHG} - {Game.FTAG} {away} ";
            return matchObject;
        }

        public class FutureMatchJson
        {
            public string HomeTeam { get; set; }

            public string AwayTeam { get; set; }

            public string Date { get; set; }
        }

        public class MatchJSON
        {
            public string HomeTeam { get; set; }

            public string AwayTeam { get; set; }

            public string Date { get; set; }

            public string HTR { get; set; }

            public string FTR { get; set; }

            public int AC { get; set; }

            public int AF { get; set; }

            public int AR { get; set; }

            public int AST { get; set; }

            public int AY { get; set; }

            public int FTAG { get; set; }

            public int FTHG { get; set; }

            public int HC { get; set; }

            public int HF { get; set; }

            public int HR { get; set; }

            public int HS { get; set; }

            public int HST { get; set; }

            public int HTAG { get; set; }

            public int HTHG { get; set; }

            public int HY { get; set; }
        }

        public class MatchObject
        {
            public string WinningTeam { get; set; }
            public string MatchDate { get; set; }
            public string HomeTeam { get; set; }
            public string AwayTeam { get; set; }
            public string MatchResult { get; set; }
            public string MatchDescription { get; set; }

        }
    }
}
