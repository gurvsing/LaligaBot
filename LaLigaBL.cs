using Microsoft.Bot.Builder;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.LanguageGeneration;

namespace BasicBot
{

    public class LaLigaBL
    {
        private static readonly int TicketPrice = 50; // Ticket price
        private static Dictionary<string, string> stadiums = new Dictionary<string, string>() { // Team Stadiums
            { "Barcelona" , "Camp Nou" }, { "Real Madrid", "Santiago Bernabeu" }, { "Ath Madrid", "Wanda Metropolitan" },
            { "Betis", "Benito Villamarin" }, { "Leganes", "Butarque" }, { "Levante", "Ciutat de Valencia" }, { "Getafe", "Coliseum Alfonso Perez" },
            { "Villareal", "Estadio de la Ceramica" }, { "Huesca", "El Alcoraz" }, { "Eibar", "Ipurua" }, { "Valladolid", "Jose Zorrilla" }, { "Alaves", "Mendizorrotta" },
            { "Valencia", "Mestalla" }, { "Girona", "Montilivi" }, { "Sevilla", "Ramón Sánchez Pizjuán" }, { "Ath Bilbao", "San Mames" }, { "Celta", "Balaidos" },
            { "Espanol", "RCDE Stadium" }, { "Vallecano", "Vallecas" }, { "Sociedad", "Anoeta" }, { "Liverpool", "Anfield" }, {"Tottenham", "White Hart Lane"}, { "Ajax", "Johan Cruijff Arena" },
        };

        private static List<MatchJSON> AllGames = JsonConvert.DeserializeObject<List<MatchJSON>>(File.ReadAllLines(@".\Resources\LaLigaData.json")[0]); // All La Liga games played
        private static List<MatchJSON> FutureGames = JsonConvert.DeserializeObject<List<MatchJSON>>(File.ReadAllLines(@".\Resources\LaLigaRest.json")[0]); // All La Liga games left to be played
        private static List<MatchJSON> ChampionsLeagueGames = JsonConvert.DeserializeObject<List<MatchJSON>>(File.ReadAllLines(@".\Resources\ChampionsLeagueGames.json")[0]); // All Champions League games left to be played

        public static string CoreferenceTeam; // Last team discussed (home team if 2 teams discussed) to be saved in context for external entity 
        public static bool IsChampionsLeague = false; // Bool to determine picture, La Liga or Champions League
        public static FindMatchResponse LastGameInMemory; // Last discussed game in memory
        public TemplateEngine lgEngine = TemplateEngine.FromFiles(@".\Resources\LaLigaTemplates.lg"); // Language Generation Engine

        public LaLigaBL()
        {
        }

        public enum PictureType
        {
            ChampionsLeague,
            LaLiga,
            Ticket,
        }

        public FindMatchResponse FindMatch(LuisResponse luisResults, bool fromContext = false)
        {
            // If requested to get last discussed game, return it
            if (fromContext)
                return LastGameInMemory;

            var home = luisResults.Entities.ContainsKey("Home") ? luisResults.Entities["Home"] : null;
            var away = luisResults.Entities.ContainsKey("Away") ? luisResults.Entities["Away"] : null;
            var team = luisResults.Entities.ContainsKey("Team") ? luisResults.Entities["Team"] : null;
            var relative = luisResults.Entities.ContainsKey("Relative") ? luisResults.Entities["Relative"] : null;

            if (home != null && away == null && team != null && home != team)
                away = team;
            else if (home == null && away != null && team != null && away != team)
                home = team;

            MatchJSON matchInfo = GetMatchInfo(home, away, team, relative);

            var findMatchResult = new FindMatchResponse()
            {
                Home = matchInfo.HomeTeam,
                Away = matchInfo.AwayTeam,
                MatchDate = matchInfo.Date,
                ResponseText = matchInfo.MatchDescription,
                Team = team,
                PictureType = IsChampionsLeague ? PictureType.ChampionsLeague : PictureType.LaLiga,
            };

            CoreferenceTeam = team ?? home ?? away; // Save last team in context
            LastGameInMemory = findMatchResult; // Save last game found
            IsChampionsLeague = false; // Reset Champions League flag
            return findMatchResult;
        }

        public string PurchaseTicket(LuisResponse luisResults)
        {
            var home = luisResults.Entities.ContainsKey("Home") ? luisResults.Entities["Home"] : null;
            var away = luisResults.Entities.ContainsKey("Away") ? luisResults.Entities["Away"] : null;
            var team = luisResults.Entities.ContainsKey("Team") ? luisResults.Entities["Team"] : null;
            bool areTeamsPresent = (home ?? away ?? team) == null; // Check if teams are present, if yes get new game from FindMatch, if not get last game saved in context

            FindMatchResponse findMatchResponse = FindMatch(luisResults, areTeamsPresent);
            var finalResponse = string.Empty;
            home = findMatchResponse.Home;
            away = findMatchResponse.Away;
            var number = luisResults.Entities.ContainsKey("number") ? luisResults.Entities["number"] : null;
            var ticketNumber = 1;
            if (number != null)
                ticketNumber = int.Parse(number);
            var ticketString = ticketNumber > 1 ? "tickets" : "ticket";


            if (home != null && away != null)
            {
                finalResponse = lgEngine.EvaluateTemplate("PurchaseTicketTemplate", new { home = home, away = away, ticketNumber = ticketNumber, ticketString = ticketString, stadium = stadiums[home], date = findMatchResponse.MatchDate, price = ticketNumber * TicketPrice });
            }
            else
            {
                finalResponse = "Sorry we could not find the game you were trying to purchase tickets for.";
            }

            return finalResponse;
        }

        private MatchJSON GetMatchInfo(string home, string away, string team, string relative = "Previous")
        {
            var Game = new MatchJSON();

            if (home != null && away != null && home != away) // If 2 teams are detected in the query
            {
                // If game is found, return game info plus a description generated from language generation
                Game = AllGames.Where(match => match.HomeTeam == home && match.AwayTeam == away).SingleOrDefault();
                if (Game != null)
                {
                    var result = Game.FTR == "H" ? $"a home win for **{home}**" : Game.FTR == "A" ? $"an away win for **{away}**" : "a draw";
                    Game.MatchDescription = lgEngine.EvaluateTemplate("FindMatchResult", new { home = home, away = away, date = Game.Date, stadium = stadiums[home], result = result, homeGoals = Game.FTHG, awayGoals = Game.FTAG, });
                }
                else // If game is not found, check future games
                {
                    if (IsChampionsLeague)
                        Game = ChampionsLeagueGames.Where(match => match.HomeTeam == home && match.AwayTeam == away).SingleOrDefault();
                    else
                        Game = FutureGames.Where(match => match.HomeTeam == home && match.AwayTeam == away).SingleOrDefault();

                    if (Game == null)
                    {
                        Game.MatchDescription = "Sorry we cannot find the match you requested";
                    }

                    Game.MatchDescription = lgEngine.EvaluateTemplate("NextGameTemplate", new { team = home, awayOrHome = "at home against", opponent = away, date = Game.Date, home = home, stadium = stadiums[home] });
                }

            }
            else // If one team is detected in the query
            {
                var singleTeam = team ?? home ?? away;
                if (relative == "Previous") // If query is about team's last game, look for its last game
                {
                    Game = AllGames.Where(match => match.AwayTeam == singleTeam || match.HomeTeam == singleTeam).Last(); // Get the team's last game
                    if (Game != null) // If team's last game is found
                    {
                        var result = Game.FTR == "H" ? $"a home win for **{Game.HomeTeam}**" : Game.FTR == "A" ? $"an away win for **{Game.AwayTeam}**" : "a draw";
                        Game.MatchDescription = lgEngine.EvaluateTemplate("FindMatchResult", new { home = Game.HomeTeam, away = Game.AwayTeam, date = Game.Date, stadium = stadiums[Game.HomeTeam], result = result, homeGoals = Game.FTHG, awayGoals = Game.FTAG, });
                    }
                    else // If team's last game is not found
                    {
                        Game.MatchDescription = "Sorry we cannot find the match you requested";
                    }
                }
                else // Find team's next game
                {
                    if (IsChampionsLeague)
                        Game = ChampionsLeagueGames.Where(match => match.HomeTeam == singleTeam || match.AwayTeam == singleTeam).FirstOrDefault();
                    else
                        Game = FutureGames.Where(match => match.HomeTeam == singleTeam || match.AwayTeam == singleTeam).FirstOrDefault();

                    var awayOrHome = singleTeam == Game.HomeTeam ? "at home" : "away";
                    var otherTeam = singleTeam == Game.HomeTeam ? Game.AwayTeam : Game.HomeTeam;
                    Game.MatchDescription = lgEngine.EvaluateTemplate("NextGameTemplate", new { team = singleTeam, awayOrHome = awayOrHome, opponent = otherTeam, date = Game.Date, home = Game.HomeTeam, stadium = stadiums[Game.HomeTeam] });
                }
            }

            return Game;
        }

        // Class for matches in JSON
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

            public string MatchDescription { get; set; }
        }

        // Find Match Intent Response
        public class FindMatchResponse
        {
            public string MatchDate { get; set; }

            public string Home { get; set; }

            public string Away { get; set; }

            public string Team { get; set; }

            public string ResponseText { get; set; }

            public LaLigaBL.PictureType PictureType { get; set; }
        }
    }
}
