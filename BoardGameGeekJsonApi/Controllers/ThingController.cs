using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using WebApi.OutputCache.V2;

namespace BoardGameGeekJsonApi.Controllers
{
    public class ThingController : ApiController
    {
        [Route("thing/{id}")]
        [CacheOutput(ClientTimeSpan = 30)]
        public async Task<GameDetails> Get(int id)
        {
            var cachedResult = Cache.Default.Get(Cache.ThingKey(id)) as GameDetails;
            if (cachedResult != null)
            {
                Debug.WriteLine("Served Thing from Cache.");
                return cachedResult;
            }

            BoardGameGeekClient client = new BoardGameGeekClient();
            
            var thing = await client.LoadGame(id, false);

            Cache.Default.Set(Cache.ThingKey(id), thing, DateTimeOffset.Now.AddSeconds(15));
            return thing;
        }
    }
}
