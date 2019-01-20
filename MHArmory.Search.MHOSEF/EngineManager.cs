using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JavaScriptEngineSwitcher.Core;
using JavaScriptEngineSwitcher.V8;

namespace MHArmory.Search.MHOSEF
{
    internal class EngineManager
    {
        public static EngineManager Instance { get; } = new EngineManager();

        private EngineManager()
        {
            var v8Factory = new V8JsEngineFactory();
            JsEngineSwitcher.Current.EngineFactories.Add(v8Factory);
            JsEngineSwitcher.Current.DefaultEngineName = v8Factory.EngineName;
        }

        public IJsEngine GetEngine()
        {
            IJsEngine engine = JsEngineSwitcher.Current.CreateDefaultEngine();
            return engine;
        }
    }
}
