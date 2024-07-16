using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using System;
using System.Collections.Generic;
using System.Text;
using static Backoffice.Shared.Models.Response.MarketFixtureResponse;

namespace Backoffice.Shared.Utilities
{
    public class MockData
    {
        public static SportTreeModel fakeSportTreeModel = new SportTreeModel()
        {
            Sports = new List<Sport>()
                    {
                        new Sport() { SportId = 1, SportName = "Soccer", SortOrder = 2,
                            Segments = new List<Segment>()
                            {
                                new Segment() { SegmentId = 1, SegmentName = "Menu Level 2.2", SortOrder = 1,
                                    Leagues = new List<League>()
                                    {
                                        new League() { LeagueId = 1, LeagueName = "Menu Level 3.1", SortOrder = 1 }
                                    }
                                }
                            }
                        },
                        new Sport() { SportId = 2, SportName = "Basketball", SortOrder = 5,
                            Segments = new List<Segment>()
                            {
                                new Segment() { SegmentId = 2, SegmentName = "Menu Level 2.2", SortOrder = 1,
                                    Leagues = new List<League>()
                                    {
                                        new League() { LeagueId = 2, LeagueName = "Menu Level 3.1", SortOrder = 1 }
                                    }
                                }
                            }
                        },
                        new Sport() { SportId = 3, SportName = "Baseball", SortOrder = 12,
                            Segments = new List<Segment>()
                            {
                                new Segment() { SegmentId = 3, SegmentName = "Menu Level 2.2", SortOrder = 1,
                                    Leagues = new List<League>()
                                    {
                                        new League() { LeagueId = 3, LeagueName = "Menu Level 3.1", SortOrder = 1 }
                                    }
                                }
                            }
                        },
                        new Sport() { SportId = 4, SportName = "Ice Hockey", SortOrder = 15,
                            Segments = new List<Segment>()
                            {
                                new Segment() { SegmentId = 4, SegmentName = "Menu Level 2.2", SortOrder = 1,
                                    Leagues = new List<League>()
                                    {
                                        new League() { LeagueId = 4, LeagueName = "Menu Level 3.1", SortOrder = 1 }
                                    }
                                }
                            }
                        },
                        new Sport() { SportId = 5, SportName = "Tennis", SortOrder = 4,
                            Segments = new List<Segment>()
                            {
                                new Segment() { SegmentId = 5, SegmentName = "Menu Level 2.2", SortOrder = 1,
                                    Leagues = new List<League>()
                                    {
                                        new League() { LeagueId = 5, LeagueName = "Menu Level 3.1", SortOrder = 1 }
                                    }
                                }
                            }
                        },
                        new Sport() { SportId = 6, SportName = "Handball", SortOrder = 14,
                            Segments = new List<Segment>()
                            {
                                new Segment() { SegmentId = 6, SegmentName = "Menu Level 2.2", SortOrder = 1,
                                    Leagues = new List<League>()
                                    {
                                        new League() { LeagueId = 6, LeagueName = "Menu Level 3.1", SortOrder = 1 }
                                    }
                                }
                            }
                        },
                        new Sport() { SportId = 12, SportName = "Rugby", SortOrder = 18,
                            Segments = new List<Segment>()
                            {
                                new Segment() { SegmentId = 12, SegmentName = "Menu Level 2.2", SortOrder = 1,
                                    Leagues = new List<League>()
                                    {
                                        new League() { LeagueId = 12, LeagueName = "Menu Level 3.1", SortOrder = 1 }
                                    }
                                }
                            }
                        },
                        new Sport() { SportId = 13, SportName = "Aussie Rules", SortOrder = 10,
                            Segments = new List<Segment>()
                            {
                                new Segment() { SegmentId = 1, SegmentName = "Menu Level 2.2", SortOrder = 1,
                                    Leagues = new List<League>()
                                    {
                                        new League() { LeagueId = 1, LeagueName = "Menu Level 3.2", SortOrder = 1 }
                                    }
                                },
                                new Segment() { SegmentId = 2, SegmentName = "Menu Level 2.3", SortOrder = 2,
                                    Leagues = new List<League>()
                                    {
                                        new League() { LeagueId = 1, LeagueName = "Menu Level 3.3", SortOrder = 1 },
                                        new League() { LeagueId = 2, LeagueName = "Menu Level 3.4", SortOrder = 2 },
                                        new League() { LeagueId = 3, LeagueName = "Menu Level 3.5", SortOrder = 3 }
                                    }
                                }
                            }
                        },
                        new Sport() { SportId = 16, SportName = "American Football", SortOrder = 1,
                            Segments = new List<Segment>()
                            {
                                new Segment() { SegmentId = 1, SegmentName = "Menu Level 2.2", SortOrder = 1,
                                    Leagues = new List<League>()
                                    {
                                        new League() { LeagueId = 1, LeagueName = "Menu Level 3.1", SortOrder = 1 }
                                    }
                                },
                                new Segment() { SegmentId = 2, SegmentName = "Menu Level 2.3", SortOrder = 2,
                                    Leagues = new List<League>()
                                    {
                                        new League() { LeagueId = 1, LeagueName = "Menu Level 3.1", SortOrder = 1 }
                                    }
                                }
                            }
                        },
                        new Sport() { SportId = 19, SportName = "Snooker", SortOrder = 17,
                            Segments = new List<Segment>()
                            {
                                new Segment() { SegmentId = 12, SegmentName = "Menu Level 2.2", SortOrder = 1,
                                    Leagues = new List<League>()
                                    {
                                        new League() { LeagueId = 12, LeagueName = "Menu Level 3.1", SortOrder = 1 }
                                    }
                                }
                            }
                        },
                        new Sport() { SportId = 20, SportName = "Badminton", SortOrder = 12,
                            Segments = new List<Segment>()
                            {
                                new Segment() { SegmentId = 20, SegmentName = "Menu Level 2.2", SortOrder = 1,
                                    Leagues = new List<League>()
                                    {
                                        new League() { LeagueId = 20, LeagueName = "Menu Level 3.1", SortOrder = 1 }
                                    }
                                }
                            }
                        },
                        new Sport() { SportId = 21, SportName = "Counter-Strike", SortOrder = 12,
                            Segments = new List<Segment>()
                            {
                                new Segment() { SegmentId = 21, SegmentName = "Menu Level 2.2", SortOrder = 1,
                                    Leagues = new List<League>()
                                    {
                                        new League() { LeagueId = 21, LeagueName = "Menu Level 3.1", SortOrder = 1 }
                                    }
                                }
                            }
                        },
                        new Sport() { SportId = 22, SportName = "Darts", SortOrder = 12,
                            Segments = new List<Segment>()
                            {
                                new Segment() { SegmentId = 22, SegmentName = "Menu Level 2.2", SortOrder = 1,
                                    Leagues = new List<League>()
                                    {
                                        new League() { LeagueId = 22, LeagueName = "Menu Level 3.1", SortOrder = 1 }
                                    }
                                }
                            }
                        }
                    }
        };

        public static SportTreeModel fakeSportTreeModelOutright = new SportTreeModel()
        {
            Sports = new List<Sport>()
                    {
                        new Sport() { SportId = 1, SportName = "Soccer", SortOrder = 2,
                            Segments = new List<Segment>()
                            {
                                new Segment() { SegmentId = 1, SegmentName = "Menu Level 2.2", SortOrder = 1,
                                    Leagues = new List<League>()
                                    {
                                        new League() { LeagueId = 1, LeagueName = "Menu Level 3.1", SortOrder = 1 }
                                    }
                                }
                            }
                        },
                        new Sport() { SportId = 2, SportName = "Basketball", SortOrder = 5,
                            Segments = new List<Segment>()
                            {
                                new Segment() { SegmentId = 1, SegmentName = "Menu Level 2.2", SortOrder = 1,
                                    Leagues = new List<League>()
                                    {
                                        new League() { LeagueId = 1, LeagueName = "Menu Level 3.1", SortOrder = 1 }
                                    }
                                }
                            }
                        },
                        new Sport() { SportId = 3, SportName = "Baseball", SortOrder = 12,
                            Segments = new List<Segment>()
                            {
                                new Segment() { SegmentId = 1, SegmentName = "Menu Level 2.2", SortOrder = 1,
                                    Leagues = new List<League>()
                                    {
                                        new League() { LeagueId = 1, LeagueName = "Menu Level 3.1", SortOrder = 1 }
                                    }
                                }
                            }
                        },
                        new Sport() { SportId = 4, SportName = "Ice Hockey", SortOrder = 15,
                            Segments = new List<Segment>()
                            {
                                new Segment() { SegmentId = 1, SegmentName = "Menu Level 2.2", SortOrder = 1,
                                    Leagues = new List<League>()
                                    {
                                        new League() { LeagueId = 1, LeagueName = "Menu Level 3.1", SortOrder = 1 }
                                    }
                                }
                            }
                        },
                        new Sport() { SportId = 5, SportName = "Tennis", SortOrder = 4,
                            Segments = new List<Segment>()
                            {
                                new Segment() { SegmentId = 1, SegmentName = "Menu Level 2.2", SortOrder = 1,
                                    Leagues = new List<League>()
                                    {
                                        new League() { LeagueId = 1, LeagueName = "Menu Level 3.1", SortOrder = 1 }
                                    }
                                }
                            }
                        },
                        new Sport() { SportId = 6, SportName = "Handball", SortOrder = 14,
                            Segments = new List<Segment>()
                            {
                                new Segment() { SegmentId = 1, SegmentName = "Menu Level 2.2", SortOrder = 1,
                                    Leagues = new List<League>()
                                    {
                                        new League() { LeagueId = 1, LeagueName = "Menu Level 3.1", SortOrder = 1 }
                                    }
                                }
                            }
                        },
                        new Sport() { SportId = 12, SportName = "Rugby", SortOrder = 18,
                            Segments = new List<Segment>()
                            {
                                new Segment() { SegmentId = 1, SegmentName = "Menu Level 2.2", SortOrder = 1,
                                    Leagues = new List<League>()
                                    {
                                        new League() { LeagueId = 1, LeagueName = "Menu Level 3.1", SortOrder = 1 }
                                    }
                                }
                            }
                        },
                        new Sport() { SportId = 13, SportName = "Aussie Rules", SortOrder = 10,
                            Segments = new List<Segment>()
                            {
                                new Segment() { SegmentId = 1, SegmentName = "Menu Level 2.2", SortOrder = 1,
                                    Leagues = new List<League>()
                                    {
                                        new League() { LeagueId = 1, LeagueName = "Menu Level 3.1", SortOrder = 1 }
                                    }
                                }
                            }
                        },
                        new Sport() { SportId = 16, SportName = "American Football", SortOrder = 9,
                            Segments = new List<Segment>()
                            {
                                new Segment() { SegmentId = 1, SegmentName = "Menu Level 2.2", SortOrder = 1,
                                    Leagues = new List<League>()
                                    {
                                        new League() { LeagueId = 1, LeagueName = "Menu Level 3.1", SortOrder = 1 }
                                    }
                                }
                            }
                        },
                        new Sport() { SportId = 19, SportName = "Snooker", SortOrder = 17,
                            Segments = new List<Segment>()
                            {
                                new Segment() { SegmentId = 1, SegmentName = "Menu Level 2.2", SortOrder = 1,
                                    Leagues = new List<League>()
                                    {
                                        new League() { LeagueId = 1, LeagueName = "Menu Level 3.1", SortOrder = 1 }
                                    }
                                }
                            }
                        }
                    }
        };


        public static List<SportsTimeSettings> fakeSportsTimeSettings = new List<SportsTimeSettings>()
        {
            new SportsTimeSettings { SportsId = 1 , SportName = "Soccer", MarketId = 1, MarketName = "Handicap", LeagueId= 1, LeagueName = "Euro Cup", SegmentId = 1, SegmentName = "International",
                LiveMatchSettings = new LiveMatchSettings {Start = 1, End = 100, Interval = 10 , OddsKey = new OddsKey { FirstKey = 1, LastKey = 10, FirstKeyLocation = 1, LastKeyLocation =10 } },
                PrematchSettings = new PrematchSettings {Start = 1, End = 100, Interval = 10, OddsKey = new OddsKey { FirstKey = 1, LastKey = 10, FirstKeyLocation = 1, LastKeyLocation =10 } }},
            new SportsTimeSettings { SportsId = 2 , SportName = "Basketball", MarketId = 1, MarketName = "Handicap", LeagueId= 1, LeagueName = "USA", SegmentId = 1, SegmentName = "International",
                LiveMatchSettings = new LiveMatchSettings {Start = 1, End = 100, Interval = 10 , OddsKey = new OddsKey { FirstKey = 1, LastKey = 10, FirstKeyLocation = 1, LastKeyLocation =10 } },
                PrematchSettings = new PrematchSettings {Start = 1, End = 100, Interval = 10, OddsKey = new OddsKey { FirstKey = 1, LastKey = 10, FirstKeyLocation = 1, LastKeyLocation =10 } }},
        };

        public static List<MockDataFixtureModel> fakeFixtures = new List<MockDataFixtureModel>()
        {
            new MockDataFixtureModel()
                {
                    SegmentId = 1,
                    LeagueId = 1,
                    FixtureStartDate = new DateTime(2021, 6, 20, 11 , 0, 0),
                    HomeName = "PORT ADELAIDE POWER",
                    AwayName = "GEELONG CATS",
                    Markets = new List<MarketModel>
                    {
                        new MarketModel()
                        {
                            MarketId = "1",
                            MarketName = "Winner",
                            MarketTemplateId = MarketTemplate.FT1X2.ToString(),
                            Selections = new List<SelectionModel>()
                            {
                                new SelectionModel()
                                {
                                    DecimalOdds = "1.044"
                                },
                                new SelectionModel()
                                {
                                    DecimalOdds = "1.45"
                                },
                                new SelectionModel()
                                {
                                    DecimalOdds = "1.65"
                                }
                            }
                        },
                        new MarketModel()
                        {
                            MarketId = "2",
                            MarketName = "Total",
                            MarketTemplateId = MarketTemplate.FTOU.ToString(),
                            Selections = new List<SelectionModel>()
                            {
                                new SelectionModel()
                                {
                                    DecimalOdds = "1.044"
                                },
                                new SelectionModel()
                                {
                                    DecimalOdds = "1.45"
                                },
                                new SelectionModel()
                                {
                                    DecimalOdds = "1.65"
                                }
                            }
                        },
                        new MarketModel()
                        {
                            MarketId = "3",
                            MarketName = "Handicap",
                            MarketTemplateId = MarketTemplate.FTHDP.ToString(),
                            Selections = new List<SelectionModel>()
                            {
                                new SelectionModel()
                                {
                                    DecimalOdds = "1.044"
                                },
                                new SelectionModel()
                                {
                                    DecimalOdds = "1.45"
                                },
                                new SelectionModel()
                                {
                                    DecimalOdds = "1.65"
                                }
                            }
                        }
                    }
                },
                new MockDataFixtureModel()
                {
                    SegmentId = 1,
                    LeagueId = 1,
                    FixtureStartDate = new DateTime(2021, 6, 22, 09 , 0, 0),
                    HomeName = "Muroy",
                    AwayName = "Gofein",
                    Markets = new List<MarketModel>
                    {
                        new MarketModel()
                        {
                            MarketId = "210",
                            MarketName = "1st Set Game 6 - Winner",
                            MarketTemplateId = MarketTemplate.FT1X2.ToString(),
                            Selections = new List<SelectionModel>()
                            {
                                new SelectionModel()
                                {
                                    SelectionId = "4",
                                    DecimalOdds = "1.2"
                                },
                                new SelectionModel()
                                {
                                    SelectionId = "5",
                                    DecimalOdds = "4.05"
                                }
                            }
                        },
                        new MarketModel()
                        {
                            MarketId = "210",
                            MarketName = "1st Set Game 4 - Winner",
                            MarketTemplateId = MarketTemplate.FT1X2.ToString(),
                            Selections = new List<SelectionModel>()
                            {
                                new SelectionModel()
                                {
                                    SelectionId = "4",
                                    DecimalOdds = "1.2"
                                },
                                new SelectionModel()
                                {
                                    SelectionId = "5",
                                    DecimalOdds = "4.05"
                                }
                            }
                        },
                        new MarketModel()
                        {
                            MarketId = "2",
                            MarketName = "Total",
                            MarketTemplateId = MarketTemplate.FTOU.ToString(),
                            Selections = new List<SelectionModel>()
                            {
                                new SelectionModel()
                                {
                                    DecimalOdds = "1.044"
                                },
                                new SelectionModel()
                                {
                                    DecimalOdds = "1.45"
                                },
                                new SelectionModel()
                                {
                                    DecimalOdds = "1.65"
                                }
                            }
                        },
                        new MarketModel()
                        {
                            MarketId = "3",
                            MarketName = "Handicap",
                            MarketTemplateId = MarketTemplate.FTHDP.ToString(),
                            Selections = new List<SelectionModel>()
                            {
                                new SelectionModel()
                                {
                                    DecimalOdds = "1.044"
                                },
                                new SelectionModel()
                                {
                                    DecimalOdds = "1.45"
                                },
                                new SelectionModel()
                                {
                                    DecimalOdds = "1.65"
                                }
                            }
                        }
                    }
                }
        };
        public static List<KeyTimeModel> PrematchSettings = new List<KeyTimeModel>()
        {
            new KeyTimeModel() { SortOrder=1, Time= -41472000, TimeDisplay = "INST", Key = 101 },
            new KeyTimeModel() { SortOrder=2, Time= -20736000, TimeDisplay = "240D" },
            new KeyTimeModel() { SortOrder=3, Time= -10368000, TimeDisplay = "120D" },
            new KeyTimeModel() { SortOrder=4, Time= -5184000, TimeDisplay = "60D" },
            new KeyTimeModel() { SortOrder=5, Time= -2592000, TimeDisplay = "30D" },
            new KeyTimeModel() { SortOrder=6, Time= -1296000, TimeDisplay = "15D" },
            new KeyTimeModel() { SortOrder=7, Time= -864000, TimeDisplay = "10D" },
            new KeyTimeModel() { SortOrder=8, Time= -777600, TimeDisplay = "9D" },
            new KeyTimeModel() { SortOrder=9, Time= -691200, TimeDisplay = "8D" },
            new KeyTimeModel() { SortOrder=10, Time= -604800, TimeDisplay = "7D" },
            new KeyTimeModel() { SortOrder=11, Time= -518400, TimeDisplay = "6D" },
            new KeyTimeModel() { SortOrder=12, Time= -432000, TimeDisplay = "5D" },
            new KeyTimeModel() { SortOrder=13, Time= -345600, TimeDisplay = "4D" },
            new KeyTimeModel() { SortOrder=14, Time= -259200, TimeDisplay = "3D" },
            new KeyTimeModel() { SortOrder=15, Time= -216000, TimeDisplay = "2.5D" },
            new KeyTimeModel() { SortOrder=16, Time= -172800, TimeDisplay = "2D" },
            new KeyTimeModel() { SortOrder=17, Time= -129600, TimeDisplay = "36h" },
            new KeyTimeModel() { SortOrder=18, Time= -86400, TimeDisplay = "24h" },
            new KeyTimeModel() { SortOrder=19, Time= -64800, TimeDisplay = "18h" },
            new KeyTimeModel() { SortOrder=20, Time= -43200, TimeDisplay = "12h" },
            new KeyTimeModel() { SortOrder=21, Time= -21600, TimeDisplay = "6h" },
            new KeyTimeModel() { SortOrder=22, Time= -10800, TimeDisplay = "3h" },
            new KeyTimeModel() { SortOrder=23, Time= -7200, TimeDisplay = "2h" },
            new KeyTimeModel() { SortOrder=24, Time= -3600, TimeDisplay = "1h" },
            new KeyTimeModel() { SortOrder=25, Time= -1800, TimeDisplay = "30m" },
            new KeyTimeModel() { SortOrder=26, Time= -900, TimeDisplay = "15m" }
        };

        public static List<KeyTimeModel> LiveSettings = new List<KeyTimeModel>()
        {
            new KeyTimeModel() { TimeDisplay = "0", Key = 101},
            new KeyTimeModel() { TimeDisplay = "20" },
            new KeyTimeModel() { TimeDisplay = "40" },
            new KeyTimeModel() { TimeDisplay = "60" },
            new KeyTimeModel() { TimeDisplay = "70" },
            new KeyTimeModel() { TimeDisplay = "80" },
            new KeyTimeModel() { TimeDisplay = "90" }
        };

        public static List<SportsLiveTimeline> SportsLiveTimeline = new List<SportsLiveTimeline>()
        {
            new SportsLiveTimeline()
            {
                //Aussie Rules
                SportId = 13,
                Overtime = 80,
                Time = new List<int>() { 0, 20, 40, 70, 80, 90 }
            },
            new SportsLiveTimeline()
            {
                //Baseball
                SportId = 3,
                Overtime = 45,
                Time = new List<int>() { 0, 10, 20, 30, 35, 40 }
            },
            new SportsLiveTimeline()
            {
                //eBasketball
                SportId = 153,
                Overtime = 45,
                Time = new List<int>() { 0, 5, 10, 15, 20, 23 }
            },
            new SportsLiveTimeline()
            {
                //Ice Hockey
                SportId = 4,
                Overtime = 60,
                Time = new List<int>() { 0, 20, 40, 50,60, 65  }
            },
            new SportsLiveTimeline()
            {
                //eSoccer
                SportId = 137,
                Overtime = 0,
                Time = new List<int>() { 0, 2, 4, 6, 8, 9, 10 }
            },
            new SportsLiveTimeline()
            {
                //Field hockey
                SportId = 24,
                Overtime = 60,
                Time = new List<int>() { 0,15, 30, 45, 50, 60, 70 }
            },
            new SportsLiveTimeline()
            {
                //Futsal
                SportId = 29,
                Overtime = 40,
                Time = new List<int>() { 0, 10, 20, 30, 33,40,50 }
            },
            new SportsLiveTimeline()
            {
                //Handball
                SportId = 6,
                Overtime = 60,
                Time = new List<int>() { 0,15, 30, 47, 60, 70 }
            },
             new SportsLiveTimeline()
            {
                 //Kabaddi
                SportId = 138,
                Overtime = 0,
                Time = new List<int>() { 0, 10, 20, 30,40 }
            },
             new SportsLiveTimeline()
            {
                 //Rugby
                SportId = 12,
                Overtime = 80,
                Time = new List<int>() { 0,20, 40, 60, 70, 80, 90 }
            },
              new SportsLiveTimeline()
            {
                //Soccer
                SportId = 1,
                Overtime = 90,
                Time = new List<int>() { 0,15, 30, 45, 60, 75, 90, 105, 120 }
            },
                new SportsLiveTimeline()
            {
                //Basketball
                SportId = 2,
                Overtime = 40,
                Time = new List<int>() { 0, 10, 20, 30, 35, 40, 45 }
            },
                 new SportsLiveTimeline()
            {
                //American Football
                SportId = 16,
                Overtime = 60,
                Time = new List<int>() { 0, 15, 30, 45, 52, 60, 70 }
            },
        };
    }
}
