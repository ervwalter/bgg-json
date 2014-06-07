using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using WebApi.OutputCache.V2;

namespace BoardGameGeekJsonApi.Controllers
{
    public class ChallengeController : ApiController
    {
        [Route("challenge/{id}")]
        [CacheOutput(ClientTimeSpan = 30)]
        public async Task<Challenge> Get(int id)
        {
            var cachedResult = Cache.Default.Get(Cache.ChallengeKey(id)) as Challenge;
            if (cachedResult != null)
            {
                Debug.WriteLine("Served Collection from Cache.");
                return cachedResult;
            }

            var client = new BoardGameGeekClient();
            var geeklist = await client.LoadGeekList(id);

            var options = ParseOptions(geeklist.Description);
            var goalPerGame = options.ContainsKey("GoalPerGame") ? int.Parse((string)options["GoalPerGame"]) : -1;
            var start = options.ContainsKey("Start") ? DateTime.Parse((string)options["Start"]) : (DateTime?)null;
            var end = options.ContainsKey("End") ? DateTime.Parse((string)options["End"]) : (DateTime?)null;

            var pageOne = await client.LoadPlays(geeklist.Username, 1, start, end);
            var plays = new List<PlayItem>();

            List<Task<Plays>> tasks = new List<Task<Plays>>();
            plays.AddRange(pageOne.Items);
            if (pageOne.Total > 100)
            {
                int remaining = pageOne.Total - 100;
                int page = 2;

                while (remaining > 0)
                {
                    tasks.Add(client.LoadPlays(geeklist.Username, page, start, end));
                    page++;
                    remaining -= 100;
                }
            }

            Challenge challenge = new Challenge()
            {
                GeekListId = id,
                Title = geeklist.Title,
                Username = geeklist.Username,
                Start = start != null ? start.Value.ToString("yyyy-MM-dd") : null,
                End = start != null ? end.Value.ToString("yyyy-MM-dd") : null,
                GoalPerGame = goalPerGame,
                Items = new List<ChallengeItem>()
            };

            var games = from item in geeklist.Items
                        select new
                        {
                            GameId = item.GameId,
                            Name = item.Name,
                            Description = item.Description
                        };


            var itemsById = new Dictionary<int, ChallengeItem>();

            foreach (var item in games)
            {
                var gameOptions = ParseOptions(item.Description);
                var challengeItem = new ChallengeItem()
                {
                    GameId = item.GameId,
                    Name = item.Name
                };

                if (gameOptions.ContainsKey("AltName"))
                {
                    challengeItem.Name = (string)gameOptions["AltName"];
                }
                if (gameOptions.ContainsKey("AdditionalGameId"))
                {
                    challengeItem.AdditionalGameIds = new List<int>();
                    var list = gameOptions["AdditionalGameId"] as List<string>;
                    if (list == null)
                    {
                        challengeItem.AdditionalGameIds.Add(int.Parse((string)gameOptions["AdditionalGameId"]));
                    }
                    else
                    {
                        challengeItem.AdditionalGameIds.AddRange(list.Select(i => int.Parse((string)i)));
                    }
                }
                
                challenge.Items.Add(challengeItem);
                itemsById[item.GameId] = challengeItem;
                if (challengeItem.AdditionalGameIds != null)
                {
                    foreach (var altGameId in challengeItem.AdditionalGameIds)
                    {
                        itemsById[altGameId] = challengeItem;
                    }
                }
            }

            var additionalPages = await Task.WhenAll(tasks);
            foreach (var page in additionalPages)
            {
                plays.AddRange(page.Items);
            }

            foreach (var play in plays)
            {
                if (itemsById.ContainsKey(play.GameId))
                {
                    itemsById[play.GameId].PlayCount++;
                }
            }

            challenge.Complete = true;
            foreach (var game in challenge.Items)
            {
                if (game.PlayCount >= goalPerGame)
                {
                    game.Complete = true;
                }
                else
                {
                    challenge.Complete = false;
                }
            }

            Cache.Default.Set(Cache.ChallengeKey(id), challenge, DateTimeOffset.Now.AddSeconds(15));
            return challenge;

        }

        public Dictionary<string, object> ParseOptions(string description)
        {
            var options = new Dictionary<string, object>(StringComparer.CurrentCultureIgnoreCase);
            var reader = new StringReader(description);
            var inOptions = false;
            var delimiters = new char[] { ':', '=' };
            var line = reader.ReadLine().Trim();
            while (line != null)
            {
                line = line.Trim();
                if (inOptions)
                {
                    if (line == "%End%")
                    {
                        inOptions = false;
                    }
                    else
                    {
                        var pieces = line.Split(delimiters, 2);
                        if (pieces.Length == 2)
                        {
                            var key = pieces[0].Trim();
                            if (options.ContainsKey(key))
                            {
                                var list = options[key] as List<string>;
                                if (list == null)
                                {
                                    list = new List<string>();
                                    list.Add((string)options[key]);
                                    options[key] = list;
                                }
                                list.Add(pieces[1].Trim());
                            }
                            else
                            {
                                options[key] = pieces[1].Trim();
                            }
                        }
                    }
                }
                else if (line == "%Options%")
                {
                    inOptions = true;
                }
                line = reader.ReadLine();
            }

            return options;
        }

    }
}