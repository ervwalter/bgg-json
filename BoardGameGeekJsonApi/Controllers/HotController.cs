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
    public class HotController : ApiController
    {
        [Route("hot/")]
        [CacheOutput(ClientTimeSpan = 300)]
        public async Task<List<HotGame>> Get()
        {
            var cachedResult = Cache.Default.Get("Hotness") as List<HotGame>;
            if (cachedResult != null)
            {
                Debug.WriteLine("Served Hotness from Cache.");
                return cachedResult;
            }
            
            var client = new BoardGameGeekClient();

            var hotness = (await client.LoadHotness()).ToList();
            Cache.Default.Set("Hotness", hotness, DateTimeOffset.Now.AddSeconds(300));

            return hotness;
        }
    }
}
