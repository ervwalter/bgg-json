using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Web;

namespace BoardGameGeekJsonApi
{
    public static class Cache
    {
        public static MemoryCache Default = new MemoryCache("CustomCache");

        public static string CollectionKey(string username, bool grouped, bool details)
        {
            return "collection:" + (grouped ? "grouped:" : "ungrouped:") + (details ? "detailed:" : "basic:") + username;
        }

        public static string ThingKey(int id)
        {
            return "thing:" + id.ToString();
        }

        public static string PlaysKey(string username)
        {
            return "plays:" + username;
        }

        public static string LongThingKey(int id)
        {
            return "longthing:" + id.ToString();
        }


    }
}