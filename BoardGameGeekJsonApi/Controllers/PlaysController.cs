using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Caching;
using System.Threading.Tasks;
using System.Web.Http;
using WebApi.OutputCache.V2;

namespace BoardGameGeekJsonApi.Controllers
{
    public class PlaysController : ApiController
    {
        [Route("plays/{username}")]
        [CacheOutput(ClientTimeSpan = 30)]
        public async Task<List<PlayItem>> Get(string username)
        {
            var cachedResult = Cache.Default.Get(Cache.PlaysKey(username)) as List<PlayItem>;
            if (cachedResult != null)
            {
                Debug.WriteLine("Served Plays from Cache.");
                return cachedResult;
            }

            var client = new BoardGameGeekClient();

            var plays = await client.LoadLastPlays(username);
            var gameIds = new HashSet<int>(plays.Select(g => g.GameId));
            var tasks = gameIds.Select(id => client.LoadGame(id, true)).ToList();
            var gameDetails = await Task.WhenAll(tasks);
            var gameDetailsById = gameDetails.ToDictionary(g => g.GameId);

            var response = (from p in plays
                            orderby p.PlayDate descending
                            let g = gameDetailsById[p.GameId]
                            select new PlayItem
                           {
                               GameId = p.GameId,
                               Name = p.Name,
                               Image = g.Image,
                               Thumbnail = g.Thumbnail,
                               PlayDate = p.PlayDate,
                               NumPlays = p.NumPlays
                           }).ToList();
            Cache.Default.Set(Cache.PlaysKey(username), response, DateTimeOffset.Now.AddSeconds(15));

            return response;
        }

    }
}