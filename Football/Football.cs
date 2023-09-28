using System.Linq.Expressions;
using System.Reflection;
using Xunit.Sdk;

namespace Football
{
    public class FootballTests
    {

        // you have a set of teams identified by name (Chargers, Rams, Seahawks, 49ers, Raiders, Cardinals, Bronocs)
        public static IEnumerable<object[]> Teams => new object[][]
   {
        // new object[] { new Team[] { new Team("Chargers"), new Team("Chargers") } }, // how can I make this unique? and where should it be enforced? - how can you limit instances to not have the same properties?
        new object[] { new Team[] { new Team("Rams"), new Team("Chargers") } },
        new object[] { new Team[] { new Team("Rams"), new Team("Chargers"), new Team("SeaHawks") } },
        new object[] { new Team[] { new Team("Rams"), new Team("Chargers"), new Team("SeaHawks"), new Team("49ers") } }
   };

        [Fact]
        public void TestSetOfTeams()
        {
            Team chargers = new Team("Chargers");
            Team chargers2 = new Team("Chargers");
            Team rams = new Team("Rams");
            Assert.IsType<Team>(chargers);
            Assert.IsType<Team>(rams);
            Assert.NotEqual(chargers, rams);
            Assert.NotEqual(chargers, chargers2);
        }

        [Fact]
        public void TestGame()
        {
            Team chargers = new Team("Chargers");
            Team rams = new Team("Rams");
            Team[] teams = { chargers, rams };
            Season mmxxiii = new Season(teams);
            Season.Game game = new(chargers, rams);
            game.Play();
            Assert.Equal(true, game.Final);
            Assert.NotNull(game.HomeTeamPoints);
            Assert.NotNull(game.AwayTeamPoints);
            Assert.Throws<Season.Game.GameAlreadyPlayed>(game.Play);
        }

        //  home wins are worth 2 season tallies
        // away wins are worth 3 season tallies
        // draws are always worth 1 season tallies
        [Fact]
        public void TestAddingSeasonPoints()
        {
            Team chargers = new Team("Chargers");
            Team rams = new Team("Rams");
            Team[] teams = { chargers, rams };
            Season mmxxiii = new Season(teams);
            Season.Game game = new(chargers, rams);
            game.Play();
            mmxxiii.AddSeasonPoints(game);
            Assert.Equal(game.AwayTeamPoints > game.HomeTeamPoints, mmxxiii.TallyTable[rams].Talies - 3 == mmxxiii.TallyTable[chargers].Talies);
            Assert.Equal(game.AwayTeamPoints < game.HomeTeamPoints, mmxxiii.TallyTable[rams].Talies + 2 == mmxxiii.TallyTable[chargers].Talies);
            Assert.Equal(game.AwayTeamPoints == game.HomeTeamPoints, mmxxiii.TallyTable[rams] == mmxxiii.TallyTable[chargers]);
            Assert.Equal(mmxxiii.TallyTable[chargers].Points, game.HomeTeamPoints - game.AwayTeamPoints);
            Assert.Equal(mmxxiii.TallyTable[rams].Points, game.AwayTeamPoints - game.HomeTeamPoints);

            mmxxiii.AddSeasonPoints(game);
            Assert.Equal(game.AwayTeamPoints > game.HomeTeamPoints, mmxxiii.TallyTable[rams].Talies - 6 == mmxxiii.TallyTable[chargers].Talies);
            Assert.Equal(game.AwayTeamPoints < game.HomeTeamPoints, mmxxiii.TallyTable[rams].Talies + 4 == mmxxiii.TallyTable[chargers].Talies);
            Assert.Equal(game.AwayTeamPoints == game.HomeTeamPoints, mmxxiii.TallyTable[rams] == mmxxiii.TallyTable[chargers]);
            Assert.Equal(mmxxiii.TallyTable[chargers].Points, (game.HomeTeamPoints - game.AwayTeamPoints) * 2);
            Assert.Equal(mmxxiii.TallyTable[rams].Points, (game.AwayTeamPoints - game.HomeTeamPoints) * 2);
        }

        // each season every team plays one game at home and one game away
        // teams will not play each other twice
        [Theory]
        [MemberData(nameof(Teams))]
        public void TestCreateSeasonGames(Team[] teams)
        {
            for (var i = 0; i < 10; i++)
            {
                Season mmxxiii = new Season(teams);
                Assert.Equal(teams.Length, mmxxiii.SeasonGames.Count);
                HashSet<Team> homeGames = new();
                HashSet<Team> awayGames = new();
                foreach (var seasonGame in mmxxiii.SeasonGames)
                {
                    Assert.NotEqual(seasonGame.AwayTeam, seasonGame.HomeTeam);
                    homeGames.Add(seasonGame.HomeTeam);
                    awayGames.Add(seasonGame.AwayTeam);
                    Season.Game reversedGame = new(seasonGame.AwayTeam, seasonGame.HomeTeam);
                    Assert.False(mmxxiii.SeasonGames.Contains(reversedGame));
                }
                Assert.Equal(mmxxiii.SeasonGames.Count * 2, homeGames.Count + awayGames.Count);
            }

        }

        // rank in order of most season tallies
        // in the event of a tie with season tallies the winner is determined by the difference in points scored by them vs the points scored against them
        [Theory]
        [MemberData(nameof(Teams))]
        public void TestRankTeams(Team[] teams)
        {
            Season mmxxiii = new Season(teams);
            mmxxiii.PlaySeasonGames();

            Console.WriteLine("-------- Tally Table --------");
            foreach (var team in mmxxiii.RankededTallyTable)
            {
                Console.WriteLine($"{team.Key} | Tallies: {team.Value.Talies} | Points: {team.Value.Points}");
            };
            Console.WriteLine("\n");

            var OrderedTallyTable = mmxxiii.RankededTallyTable.ToList();
            for (var i = 0; i < OrderedTallyTable.Count - 1; i++)
            {
                TeamRecord current = OrderedTallyTable[i].Value;
                TeamRecord next = OrderedTallyTable[i + 1].Value;
                if (current.Talies == next.Talies)
                {
                    Assert.True(current.Points >= next.Points);
                }
                else
                {
                    Assert.True(current.Talies >= next.Talies);
                }
            }

            Console.WriteLine("-------- Season Games --------");
            foreach (var game in mmxxiii.SeasonGames)
            {
                Console.WriteLine(game);
            }
            Console.WriteLine("\n");
        }

    }

    //  PRODUCTION CODE BELOW THIS LINE
    // *********************************

    /* 
        the league table should be available at any point in time with those who have played the most games ranked above those that have played less
        bonus points for keeping data private
    */

    public class Team
    {
        private readonly string _name;

        public Team(string Name)
        {
            _name = Name;
        }

        public override string ToString()
        {
            return _name;
        }
    }

    class Season
    {
        private readonly Dictionary<Team, TeamRecord> _tallyTable = new();

        public Dictionary<Team, TeamRecord> TallyTable => _tallyTable;

        public IOrderedEnumerable<KeyValuePair<Team, TeamRecord>> RankededTallyTable;

        public HashSet<Game> SeasonGames { get; }

        public Season(Team[] teams)
        {
            foreach (var team in teams)
            {
                _tallyTable.Add(team, new TeamRecord());
            }
            SeasonGames = CreateSeasonGames();
            RankededTallyTable = _tallyTable.OrderByDescending(team => team.Value.Talies).ThenByDescending(team => team.Value.Points);
        }

        private HashSet<Game> CreateSeasonGames()
        {
            HashSet<Game> seasonGames = new();
            List<Team> teamList = _tallyTable.Keys.ToList();
            Random rnd = new Random();

            if (teamList.Count < 2)
            {
                throw new Exception($"{teamList[0]} is the only team in the season. They cannot play themselves!");
            }

            List<int> games = new List<int>(teamList.Count);

            for (int homeTeam = 0; homeTeam < teamList.Count;)
            {
                bool playedBefore = false;
                int randomAwayTeam = rnd.Next(teamList.Count); // I think we can make this more efficient in the last iteration to add the only away team left over
                if (randomAwayTeam == homeTeam || games.Contains(randomAwayTeam))
                {
                    continue;
                }

                if (teamList.Count % 2 == 0)
                {


                    bool sharedOpponent = false;

                    if (randomAwayTeam < games.Count)
                    {
                        int awayTeamOtherOpponent = games[randomAwayTeam];

                        if (awayTeamOtherOpponent < games.Count)
                        {
                            int awayTeamOtherOpponentOtherOpponent = games[awayTeamOtherOpponent];
                            sharedOpponent = awayTeamOtherOpponentOtherOpponent == homeTeam;
                            playedBefore = awayTeamOtherOpponent == homeTeam;
                        }
                    }


                    if (!sharedOpponent && !playedBefore)
                    {
                        Game game = new Game(teamList[homeTeam], teamList[randomAwayTeam]);
                        seasonGames.Add(game);
                        games.Add(randomAwayTeam);
                        homeTeam++;
                    }
                }
                else
                {
                    if (randomAwayTeam < games.Count)
                    {
                        int awayTeamOtherOpponent = games[randomAwayTeam];
                        playedBefore = awayTeamOtherOpponent == homeTeam;
                    }

                    if (!playedBefore)
                    {
                        Game game = new Game(teamList[homeTeam], teamList[randomAwayTeam]);
                        seasonGames.Add(game);
                        games.Add(randomAwayTeam);
                        homeTeam++;
                    }
                }
            }

            return seasonGames;
        }

        public void PlaySeasonGames()
        {
            foreach (var game in SeasonGames)
            {
                game.Play();
                AddSeasonPoints(game);
            }
        }

        public void AddSeasonPoints(Game game)
        {
            var homeTeamRecord = _tallyTable[game.HomeTeam];
            var awayTeamRecord = _tallyTable[game.AwayTeam];

            homeTeamRecord.Points += game.HomeTeamPoints - game.AwayTeamPoints;
            awayTeamRecord.Points += game.AwayTeamPoints - game.HomeTeamPoints;

            if (game.HomeTeamPoints > game.AwayTeamPoints)
            {
                homeTeamRecord.Talies += 2;
            }
            else if (game.AwayTeamPoints > game.HomeTeamPoints)
            {
                awayTeamRecord.Talies += 3;
            }
            else
            {
                homeTeamRecord.Talies += 1;
                awayTeamRecord.Talies += 1;
            }
            RankededTallyTable = _tallyTable.OrderByDescending(team => team.Value.Talies).ThenByDescending(team => team.Value.Points);
        }

        public override string ToString()
        {
            string TallyTable = "";

            foreach (var rankedEntry in _tallyTable)
            {
                TallyTable += $"{rankedEntry.Key} | Talies: {rankedEntry.Value.Talies} | Points: {rankedEntry.Value.Points} \n";
            }
            return TallyTable;
        }



        public class Game
        {
            private Team _homeTeam;
            private Team _awayTeam;
            private int _homeTeamPoints;
            private int _awayTeamPoints;
            private bool _final = false;
            private bool _homeTeamWin;

            public Team HomeTeam => _homeTeam;
            public Team AwayTeam => _awayTeam;

            public bool Final => _final;

            public bool HomeTeamWin => _homeTeamWin;
            public int HomeTeamPoints => _homeTeamPoints;
            public int AwayTeamPoints => _awayTeamPoints;

            public Game(Team homeTeam, Team awayTeam)
            {
                _homeTeam = homeTeam;
                _awayTeam = awayTeam;
            }

            public class GameAlreadyPlayed : Exception
            {
                public GameAlreadyPlayed(string message) : base(message) { }
            }

            public void Play()
            {
                if (!_final)
                {
                    Random rnd = new Random();
                    _homeTeamPoints = rnd.Next(100);
                    _awayTeamPoints = rnd.Next(100);
                    _homeTeamWin = true ? HomeTeamPoints > _awayTeamPoints : false;
                    _final = true;
                }
                else
                {
                    throw new GameAlreadyPlayed($"{_homeTeam} already played the {_awayTeam} at home");
                }

            }

            public override string ToString()
            {
                return $"home: {_homeTeam} {_homeTeamPoints} | away: {_awayTeam} {_awayTeamPoints}";
            }
        }
    }
    public class TeamRecord
    {
        public int Talies;
        public int Points;

    }




}









