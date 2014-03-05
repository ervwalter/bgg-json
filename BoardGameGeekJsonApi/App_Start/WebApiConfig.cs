using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Cors;
using WebApiContrib.Formatting.Jsonp;

namespace BoardGameGeekJsonApi
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Formatters.Remove(config.Formatters.XmlFormatter);
            config.Formatters.Remove(config.Formatters.FormUrlEncodedFormatter);

            config.Formatters.Insert(0, new JsonpMediaTypeFormatter(config.Formatters.JsonFormatter));

            config.Formatters.JsonFormatter.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            config.Formatters.JsonFormatter.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            config.Formatters.JsonFormatter.Indent = true;

            var cors = new EnableCorsAttribute("*", "*", "*");
            config.EnableCors(cors);

        }
    }
}
