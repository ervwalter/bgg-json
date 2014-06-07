using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Caching;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Http;
using WebApi.OutputCache.V2;

namespace BoardGameGeekJsonApi.Controllers
{
    public class CollectionController : ApiController
    {
        [Route("collection/{username}")]
        [CacheOutput(ClientTimeSpan = 30)]
        public async Task<List<CollectionItem>> Get(string username, bool grouped = false, bool details = false)
        {
            var local = Request.IsLocal();
            var cachedResult = Cache.Default.Get(Cache.CollectionKey(username, grouped, details)) as List<CollectionItem>;
            if (cachedResult != null && !local)
            {
                Debug.WriteLine("Served Collection from Cache.");
                return cachedResult;
            }

            var client = new BoardGameGeekClient();

            var gamesById = (await client.LoadCollection(username)).ToDictionary(g => g.GameId);
            var games = from g in gamesById.Values select g;

            if (grouped || details)
            {
                foreach (var game in games)
                {
                    if (game.UserComment.Contains("%Expands:"))
                    {
                        game.IsExpansion = true;
                    }
                }
                var expansions = from g in gamesById.Values where g.IsExpansion == true orderby g.Name select g; ;

                List<int> gameIds = new List<int>();
                if (details)
                {
                    // get details for everything
                    gameIds = games.Select(g => g.GameId).ToList();
                }
                else if (grouped)
                {
                    // get details only for expansions
                    gameIds = expansions.Select(g => g.GameId).ToList();
                }

                var tasks = gameIds.Select(id => client.LoadGame(id, true)).ToList();
                var gameDetailsList = await Task.WhenAll(tasks);
                var gameDetailsById = gameDetailsList.Where(g => g != null).ToDictionary(g => g.GameId);

                if (details)
                {
                    foreach (var game in games)
                    {
                        if (gameDetailsById.ContainsKey(game.GameId))
                        {
                            var gameDetails = gameDetailsById[game.GameId];
                            game.Description = gameDetails.Description;
                            game.Mechanics = gameDetails.Mechanics;
                            game.BGGRating = gameDetails.BGGRating;
                            game.Artists = gameDetails.Artists;
                            game.Publishers = gameDetails.Publishers;
                            game.Designers = gameDetails.Designers;
                        }
                    }
                }

                if (grouped)
                {
                    Regex expansionCommentExpression = new Regex(@"%Expands:(.*\w+.*)\[(\d+)\]", RegexOptions.Compiled);
                    foreach (var expansion in expansions)
                    {
                        if (gameDetailsById.ContainsKey(expansion.GameId))
                        {
                            var expansionDetails = gameDetailsById[expansion.GameId];
                            if (expansionDetails != null)
                            {
                                var expandsLinks = new List<BoardGameLink>(expansionDetails.Expands ?? new List<BoardGameLink>());
                                if (expansion.UserComment.Contains("%Expands:"))
                                {
                                    var match = expansionCommentExpression.Match(expansion.UserComment);
                                    if (match.Success)
                                    {
                                        var name = match.Groups[1].Value.Trim();
                                        var id = int.Parse(match.Groups[2].Value.Trim());
                                        expandsLinks.Add(new BoardGameLink
                                        {
                                            GameId = id,
                                            Name = name
                                        });
                                        expansion.UserComment = expansionCommentExpression.Replace(expansion.UserComment, "").Trim();
                                    }
                                }
                                foreach (var link in expandsLinks)
                                {
                                    if (gamesById.ContainsKey(link.GameId))
                                    {
                                        var game = gamesById[link.GameId];
                                        if (game.IsExpansion)
                                        {
                                            continue;
                                        }

                                        if (game.Expansions == null)
                                        {
                                            game.Expansions = new List<CollectionItem>();
                                        }
                                        game.Expansions.Add(expansion.Clone());
                                    }
                                }
                            }
                        }
                    }

                    // filter expansions out of the top level result set
                    games = games.Where(g => !g.IsExpansion);
                }
            }

            Regex removeArticles = new Regex(@"^the\ |a\ |an\ ");

            games = games.OrderBy(g => removeArticles.Replace(g.Name.ToLower(), ""));

            var result = games.ToList();
            Cache.Default.Set(Cache.CollectionKey(username, grouped, details), result, DateTimeOffset.Now.AddSeconds(15));

            return result;
        }
    }
}