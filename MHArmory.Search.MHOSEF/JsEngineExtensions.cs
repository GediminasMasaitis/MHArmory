using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JavaScriptEngineSwitcher.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace MHArmory.Search.MHOSEF
{
    internal static class JsEngineExtensions
    {
        public static void EmbedByJson(this IJsEngine engine, string name, object value)
        {
            string json = JsonConvert.SerializeObject(value, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });
            string escapedJson = json.Replace("'", "\\'");
            string command = $"{name} = JSON.parse('{escapedJson}');";
            engine.Execute(command);
        }
    }
}
