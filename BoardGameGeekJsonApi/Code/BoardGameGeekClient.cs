//
//  Adapted from the BoardGameGeek client library created by WebKoala
//  See this post for more information: http://boardgamegeek.com/thread/972785/c-async-api-client
//  Original source at https://github.com/WebKoala/W8BggApp
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Caching;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;

namespace BoardGameGeekJsonApi
{
    public class BoardGameGeekClient
    {
        public const string BASE_URL1 = "https://www.boardgamegeek.com/xmlapi";
        public const string BASE_URL2 = "https://www.boardgamegeek.com/xmlapi2";
        private static MemoryCache _gameCache = MemoryCache.Default;
        private const int GameCacheDuration = 43200; // 12 hours

        static BoardGameGeekClient()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }

        public async Task<GameDetails[]> ParallelLoadGames(IEnumerable<int> gameIds)
        {
            GameDetails[] results;
            var tasks = new List<Task<GameDetails>>();
            using (var throttler = new SemaphoreSlim(5))
            {
                foreach (var gameId in gameIds)
                {
                    await throttler.WaitAsync();
                    tasks.Add(Task<GameDetails>.Run(async () =>
                    {
                        try
                        {
                            Debug.WriteLine("Loading {0}...", gameId);
                            return await this.LoadGame(gameId, true);
                        }
                        finally
                        {
                            Debug.WriteLine("Done with {0}...", gameId);
                            throttler.Release();
                        }
                    }));

                }
                results = await Task.WhenAll(tasks);
            }
            return results;
        }

        public async Task<IEnumerable<CollectionItem>> LoadCollection(string username)
        {
            var baseGames = LoadGamesFromCollection(username, false);
            var expansions = LoadGamesFromCollection(username, true);

            await Task.WhenAll(baseGames, expansions);

            return baseGames.Result.Concat(expansions.Result);
        }

        private async Task<IEnumerable<CollectionItem>> LoadGamesFromCollection(string Username, bool GetExpansions)
        {
            try
            {

                Uri teamDataURI = new Uri(string.Format(BASE_URL2 + "/collection?username={0}&stats=1&{1}",
                    Username,
                    GetExpansions ? "subtype=boardgameexpansion" : "excludesubtype=boardgameexpansion"));


                XDocument xDoc = await ReadData(teamDataURI);

                // LINQ to XML.
                IEnumerable<CollectionItem> baseGames = from colItem in xDoc.Descendants("item")
                                                        select new CollectionItem
                                                        {
                                                            Name = GetStringValue(colItem.Element("name")),
                                                            GameId = GetIntValue(colItem, "objectid"),
                                                            AverageRating = GetDecimalValue(colItem.Element("stats").Element("rating").Element("average"), "value", -1),
                                                            ForTrade = GetBoolValue(colItem.Element("status"), "fortrade"),
                                                            Image = GetStringValue(colItem.Element("image")),
                                                            IsExpansion = GetExpansions,
                                                            MaxPlayers = GetIntValue(colItem.Element("stats"), "maxplayers"),
                                                            MinPlayers = GetIntValue(colItem.Element("stats"), "minplayers"),
                                                            NumPlays = GetIntValue(colItem.Element("numplays")),
                                                            Owned = GetBoolValue(colItem.Element("status"), "own"),
                                                            PlayingTime = GetIntValue(colItem.Element("stats"), "playingtime"),
                                                            PreOrdered = GetBoolValue(colItem.Element("status"), "preordered"),
                                                            PreviousOwned = GetBoolValue(colItem.Element("status"), "prevowned"),
                                                            Rank = GetRanking(colItem.Element("stats").Element("rating").Element("ranks")),
                                                            Rating = GetDecimalValue(colItem.Element("stats").Element("rating"), "value", -1),
                                                            Thumbnail = GetStringValue(colItem.Element("thumbnail")),
                                                            UserComment = GetStringValue(colItem.Element("comment")),
                                                            Want = GetBoolValue(colItem.Element("status"), "want"),
                                                            WantToBuy = GetBoolValue(colItem.Element("status"), "wanttobuy"),
                                                            WantToPlay = GetBoolValue(colItem.Element("status"), "wanttoplay"),
                                                            WishList = GetBoolValue(colItem.Element("status"), "wishlist"),
                                                            YearPublished = GetIntValue(colItem.Element("yearpublished"))
                                                        };
                return baseGames;

            }
            catch (Exception ex)
            {
                throw;
                ////ExceptionHandler(ex);
                //return new List<CollectionItem>();
            }
        }

        public async Task<IEnumerable<HotGame>> LoadHotness()
        {
            try
            {
                Uri teamDataURI = new Uri(BASE_URL2 + "/hot?thing=boardgame");
                XDocument xDoc = await ReadData(teamDataURI);

                // LINQ to XML.
                IEnumerable<HotGame> games = from Boardgame in xDoc.Descendants("item")
                                             select new HotGame
                                             {
                                                 Name = Boardgame.Element("name").Attribute("value").Value,
                                                 YearPublished = Boardgame.Element("yearpublished") != null ? int.Parse(Boardgame.Element("yearpublished").Attribute("value").Value) : 0,
                                                 Thumbnail = Boardgame.Element("thumbnail").Attribute("value").Value,
                                                 GameId = int.Parse(Boardgame.Attribute("id").Value),
                                                 Rank = int.Parse(Boardgame.Attribute("rank").Value)
                                             };

                return games;

            }
            catch (Exception ex)
            {
                throw;
                ////ExceptionHandler(ex);
                //return new List<HotGame>();
            }
        }

        public async Task<GeekList> LoadGeekList(int id)
        {
            try
            {
                Uri geekListURI = new Uri(string.Format(BASE_URL1 + "/geeklist/{0}", id));
                XDocument doc = await ReadData(geekListURI);

                var gl = doc.Root;
                return new GeekList()
                {
                    GeekListId = id,
                    Username = gl.Element("username").Value,
                    Title = gl.Element("title").Value,
                    Description = gl.Element("description").Value,
                    Items = (from item in gl.Elements("item")
                             where item.Attribute("objecttype").Value == "thing" && item.Attribute("subtype").Value == "boardgame"
                             select new GeekListItem()
                             {
                                 GameId = int.Parse(item.Attribute("objectid").Value),
                                 ImageId = item.Attribute("imageid") != null ? int.Parse(item.Attribute("imageid").Value) : (int?)null,
                                 Username = item.Attribute("username").Value,
                                 Name = item.Attribute("objectname").Value,
                                 Description = item.Element("body").Value,
                             }).ToList()
                };

            }
            catch (Exception ex)
            {
                throw;
                //return new GeekList();
            }
        }

        public async Task<Plays> LoadPlays(string username, int page = 1, DateTime? minDate = null, DateTime? maxDate = null)
        {
            try
            {
                string url = string.Format(BASE_URL2 + "/plays?username={0}&subtype=boardgame&excludesubtype=videogame&page={1}", username, page);
                if (minDate != null)
                {
                    url += "&mindate=" + minDate.Value.ToString("yyyy-MM-dd");
                }
                if (maxDate != null)
                {
                    url += "&maxdate=" + maxDate.Value.ToString("yyyy-MM-dd");
                }
                Uri teamDataURI = new Uri(url);
                XDocument xDoc = await ReadData(teamDataURI);

                // LINQ to XML.
                IEnumerable<PlayItem> gameCollection = from Boardgame in xDoc.Descendants("play")
                                                       select new PlayItem
                                                       {
                                                           Name = Boardgame.Element("item").Attribute("name").Value,
                                                           NumPlays = int.Parse(Boardgame.Attribute("quantity").Value),
                                                           GameId = int.Parse(Boardgame.Element("item").Attribute("objectid").Value),
                                                           PlayDate = safeParseDateTime(Boardgame.Attribute("date").Value),
                                                           Comments = Boardgame.Element("comments") != null ? TrimmedStringOrNull(Boardgame.Element("comments").Value) : null
                                                       };
                var plays = new Plays();
                plays.Total = int.Parse(xDoc.Root.Attribute("total").Value);
                plays.Items = gameCollection;
                return plays;
            }
            catch (Exception ex)
            {
                throw;
                //return new Plays();
            }
        }

        public async Task<GameDetails> LoadGame(int GameId, bool useLongCache)
        {

            GameDetails details = null;

            if (useLongCache)
            {
                details = _gameCache.Get(Cache.LongThingKey(GameId)) as GameDetails;
            }

            if (details != null)
            {
                return details;
            }

            try
            {
                Uri teamDataURI = new Uri(string.Format(BASE_URL2 + "/thing?id={0}&stats=1", GameId));
                XDocument xDoc = await ReadData(teamDataURI);

                // LINQ to XML.
                IEnumerable<GameDetails> gameCollection = from Boardgame in xDoc.Descendants("items")
                                                          select new GameDetails
                                                          {
                                                              Name = (from p in Boardgame.Element("item").Elements("name") where p.Attribute("type").Value == "primary" select p.Attribute("value").Value).SingleOrDefault(),
                                                              GameId = int.Parse(Boardgame.Element("item").Attribute("id").Value),
                                                              Artists = (from p in Boardgame.Element("item").Elements("link") where p.Attribute("type").Value == "boardgameartist" select p.Attribute("value").Value).ToList(),
                                                              AverageRating = decimal.Parse(Boardgame.Element("item").Element("statistics").Element("ratings").Element("average").Attribute("value").Value),
                                                              BGGRating = decimal.Parse(Boardgame.Element("item").Element("statistics").Element("ratings").Element("bayesaverage").Attribute("value").Value),
                                                              //Comments = LoadComments(Boardgame.Element("item").Element("comments")),
                                                              Description = Boardgame.Element("item").Element("description").Value,
                                                              Designers = (from p in Boardgame.Element("item").Elements("link") where p.Attribute("type").Value == "boardgamedesigner" select p.Attribute("value").Value).ToList(),
                                                              Expands = SetExpandsLinks(Boardgame),
                                                              Expansions = SetExpansionsLinks(Boardgame),
                                                              Mechanics = (from p in Boardgame.Element("item").Elements("link") where p.Attribute("type").Value == "boardgamemechanic" select p.Attribute("value").Value).ToList(),
                                                              Image = Boardgame.Element("item").Element("image") != null ? Boardgame.Element("item").Element("image").Value : string.Empty,
                                                              IsExpansion = SetIsExpansion(Boardgame),
                                                              Thumbnail = Boardgame.Element("item").Element("thumbnail") != null ? Boardgame.Element("item").Element("thumbnail").Value : string.Empty,
                                                              MaxPlayers = int.Parse(Boardgame.Element("item").Element("maxplayers").Attribute("value").Value),
                                                              MinPlayers = int.Parse(Boardgame.Element("item").Element("minplayers").Attribute("value").Value),
                                                              PlayerPollResults = LoadPlayerPollResults(Boardgame.Element("item").Element("poll")),
                                                              PlayingTime = int.Parse(Boardgame.Element("item").Element("playingtime").Attribute("value").Value),
                                                              Publishers = (from p in Boardgame.Element("item").Elements("link") where p.Attribute("type").Value == "boardgamepublisher" select p.Attribute("value").Value).ToList(),
                                                              Rank = GetRanking(Boardgame.Element("item").Element("statistics").Element("ratings").Element("ranks")),
                                                              //TotalComments = int.Parse(Boardgame.Element("item").Element("comments").Attribute("totalitems").Value),
                                                              YearPublished = int.Parse(Boardgame.Element("item").Element("yearpublished").Attribute("value").Value)
                                                          };

                details = gameCollection.FirstOrDefault();

                if (details.Expands != null && details.Expands.Count == 0)
                {
                    details.Expands = null;
                }
                if (details.Expansions != null && details.Expansions.Count == 0)
                {
                    details.Expansions = null;
                }

                if (details != null)
                {
                    _gameCache.Set(Cache.LongThingKey(details.GameId), details, DateTimeOffset.Now.AddSeconds(GameCacheDuration));
                }

                return details;
            }
            catch (Exception ex)
            {
                throw;
                //return null;
            }
        }

        private List<string> SetMechanics(XElement Boardgame)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<SearchResult>> Search(string query)
        {
            try
            {
                Uri teamDataURI = new Uri(string.Format(BASE_URL2 + "/search?query={0}&type=boardgame", query));

                XDocument xDoc = await ReadData(teamDataURI);

                // LINQ to XML.
                IEnumerable<SearchResult> gameCollection = from Boardgame in xDoc.Descendants("item")
                                                           select new SearchResult
                                                           {
                                                               Name = GetStringValue(Boardgame.Element("name"), "value"),
                                                               GameId = GetIntValue(Boardgame, "id")
                                                           };
                return gameCollection;

            }
            catch (Exception ex)
            {
                throw;
                //return new List<SearchResult>();
            }
        }

        public async Task<User> LoadUserDetails(string username)
        {
            try
            {
                Uri teamDataURI = new Uri(string.Format(BASE_URL2 + "/user?name={0}", username));

                XDocument xDoc = await ReadData(teamDataURI);

                // LINQ to XML.
                IEnumerable<User> users = from Boardgame in xDoc.Descendants("user")
                                          select new User
                                          {
                                              Avatar = GetStringValue(Boardgame.Element("avatarlink"), "value"),
                                              Username = username
                                          };
                return users.FirstOrDefault();

            }
            catch (Exception ex)
            {
                throw;
                //return new User();
            }
        }

        public async Task<IEnumerable<Comment>> LoadAllComments(int GameId, int totalComments)
        {
            try
            {
                List<Comment> comments = new List<Comment>();
                int page = 1;
                while ((page - 1) * 100 < totalComments)
                {
                    Uri teamDataURI = new Uri(string.Format(BASE_URL2 + "/thing?id={0}&stats=1&comments=1&page={1}", GameId, page));
                    XDocument xDoc = await ReadData(teamDataURI);
                    XElement commentsElement = xDoc.Element("items").Element("item").Element("comments");
                    var commentsRes = LoadComments(commentsElement);
                    comments.AddRange(commentsRes);
                    page++;
                }
                return comments;
            }
            catch (Exception)
            {
                throw;
                //return new List<Comment>();
            }
        }

        private bool SetIsExpansion(XElement Boardgame)
        {
            return (from p in Boardgame.Element("item").Elements("link")
                    where
                        p.Attribute("type").Value == "boardgamecategory" && p.Attribute("id").Value == "1042"
                    select p.Attribute("value").Value).FirstOrDefault() != null;
        }

        private List<BoardGameLink> SetExpandsLinks(XElement Boardgame)
        {
            var links = from p in Boardgame.Element("item").Elements("link")
                        where p.Attribute("type").Value == "boardgameexpansion" &&
                            p.Attribute("inbound") != null && p.Attribute("inbound").Value == "true"
                        select new BoardGameLink
                        {
                            Name = p.Attribute("value").Value,
                            GameId = int.Parse(p.Attribute("id").Value)
                        };

            return links.ToList();
        }

        private List<BoardGameLink> SetExpansionsLinks(XElement Boardgame)
        {
            var links = from p in Boardgame.Element("item").Elements("link")
                        where p.Attribute("type").Value == "boardgameexpansion" &&
                            (p.Attribute("inbound") == null || p.Attribute("inbound").Value != "true")
                        select new BoardGameLink
                        {
                            Name = p.Attribute("value").Value,
                            GameId = int.Parse(p.Attribute("id").Value)
                        };

            return links.ToList();
        }

        private string GetStringValue(XElement element, string attribute = null, string defaultValue = "")
        {
            if (element == null)
                return defaultValue;

            if (attribute == null)
                return element.Value;

            XAttribute xatt = element.Attribute(attribute);
            if (xatt == null)
                return defaultValue;

            return xatt.Value;
        }
        private int GetIntValue(XElement element, string attribute = null, int defaultValue = -1)
        {
            string val = GetStringValue(element, attribute, null);
            if (val == null)
                return defaultValue;

            int retVal;
            if (!int.TryParse(val, out retVal))
                retVal = defaultValue;
            return retVal;
        }
        private bool GetBoolValue(XElement element, string attribute = null, bool defaultValue = false)
        {
            string val = GetStringValue(element, attribute, null);
            if (val == null)
                return defaultValue;

            int retVal;
            if (!int.TryParse(val, out retVal))
                return defaultValue;

            return retVal == 1;
        }
        private decimal GetDecimalValue(XElement element, string attribute = null, decimal defaultValue = -1)
        {
            string val = GetStringValue(element, attribute, null);
            if (val == null)
                return defaultValue;

            decimal retVal;
            if (!decimal.TryParse(val, out retVal))
                return defaultValue;

            return retVal;
        }
        private List<PlayerPollResult> LoadPlayerPollResults(XElement xElement)
        {
            List<PlayerPollResult> playerPollResult = new List<PlayerPollResult>();
            if (xElement != null)
            {
                foreach (XElement results in xElement.Elements("results"))
                {
                    PlayerPollResult pResult = new PlayerPollResult()
                    {
                        Best = GetIntResultScore(results, "Best"),
                        Recommended = GetIntResultScore(results, "Recommended"),
                        NotRecommended = GetIntResultScore(results, "Not Recommended")
                    };
                    SetNumplayers(pResult, results);
                    playerPollResult.Add(pResult);
                }
            }
            return playerPollResult;
        }
        private void SetNumplayers(PlayerPollResult pResult, XElement results)
        {

            string value = results.Attribute("numplayers").Value;
            if (value.Contains("+"))
            {
                pResult.NumPlayersIsAndHigher = true;
            }
            value = value.Replace("+", string.Empty);

            int res = 0;
            int.TryParse(value, out res);

            pResult.NumPlayers = res;
        }
        private int GetIntResultScore(XElement results, string selector)
        {
            int res = 0;
            try
            {
                string value = (from p in results.Elements("result") where p.Attribute("value").Value == selector select p.Attribute("numvotes").Value).FirstOrDefault();

                if (value != null)
                    int.TryParse(value, out res);
            }
            catch (Exception)
            {
                return 0;
            }

            return res;
        }
        private int GetRanking(XElement rankingElement)
        {
            string val = (from p in rankingElement.Elements("rank") where p.Attribute("id").Value == "1" select p.Attribute("value").Value).SingleOrDefault();
            int rank;

            if (val == null)
                rank = -1;
            else if (val.ToLower().Trim() == "not ranked")
                rank = -1;
            else if (!int.TryParse(val, out rank))
                rank = -1;

            return rank;
        }

        private List<Comment> LoadComments(XElement commentsElement)
        {
            List<Comment> comments = new List<Comment>();

            if (commentsElement != null)
                foreach (XElement commentElement in commentsElement.Elements("comment"))
                {
                    Comment c = new Comment()
                    {
                        Text = commentElement.Attribute("value").Value,
                        Username = commentElement.Attribute("username").Value
                    };

                    decimal rating;
                    decimal.TryParse(commentElement.Attribute("rating").Value, out rating);
                    c.Rating = rating;

                    comments.Add(c);
                }
            return comments;
        }

        private async Task<XDocument> ReadData(Uri requestUrl)
        {
            Debug.WriteLine("Downloading " + requestUrl.ToString());
            // Due to malformed header I cannot use GetContentAsync and ReadAsStringAsync :(
            // UTF-8 is now hard-coded...

            XDocument data = null;
            int retries = 0;
            while (data == null && retries < 60)
            {
                retries++;
                var request = WebRequest.CreateHttp(requestUrl);
                request.Timeout = 15000;
                using (var response = (HttpWebResponse)(await request.GetResponseAsync()))
                {
                    if (response.StatusCode == HttpStatusCode.Accepted)
                    {
                        await Task.Delay(500);
                        continue;
                    }
                    using (var reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                    {
                        data = XDocument.Parse(await reader.ReadToEndAsync());
                    }
                }
            }

            if (data != null)
            {
                return data;
            }
            else
            {
                throw new Exception("Failed to download BGG data.");
            }



            //using (var client = new HttpClient())
            //{
            //    var responseBytes = await client.GetByteArrayAsync(requestUrl);
            //    client.las
            //    var xmlResponse = Encoding.UTF8.GetString(responseBytes, 0, responseBytes.Length);

            //    return XDocument.Parse(xmlResponse);
            //}
        }

        private DateTime safeParseDateTime(string date)
        {
            DateTime dt;
            if (!DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
            {
                dt = DateTime.MinValue;
            }
            return dt;
        }

        private string TrimmedStringOrNull(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }
            else
            {
                return value.Trim();
            }
        }
    }
}
