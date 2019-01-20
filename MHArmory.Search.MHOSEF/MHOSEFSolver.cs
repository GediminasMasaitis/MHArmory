using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JavaScriptEngineSwitcher.Core;
using MHArmory.Search.Contracts;
using Newtonsoft.Json;

namespace MHArmory.Search.MHOSEF
{
    public class MHOSEFSolver : ISolver
    {
        private DataConverter converter;
        private EngineManager engineManager;
        private ResultParser parser;

        public MHOSEFSolver()
        {
            converter = new DataConverter();
            engineManager = EngineManager.Instance;
            parser = ResultParser.Instance;
        }

        public string Name { get; } = "MHOSEF";

        public string Author { get; } = "Delca";

        public string Description { get; } = "description";

        public int Version { get; } = 1;

        public event Action<double> SearchProgress;


        public async Task<IList<ArmorSetSearchResult>> SearchArmorSets(ISolverData solverData, CancellationToken cancellationToken)
        {
            SearchProgress?.Invoke(0);
            string mhosefPath = Path.Combine(Directory.GetCurrentDirectory(), "mhosef");
            IList<string> paths = GetJsFilesRecursive(mhosefPath);

            ConvertedData convertedData = converter.ConvertData(solverData);

            using (IJsEngine engine = engineManager.GetEngine())
            {
                engine.Execute("window = this;");

                engine.EmbedByJson("armours", convertedData.Armors);
                engine.EmbedByJson("charms", convertedData.Charms);
                engine.EmbedByJson("jewels", convertedData.Jewels);
                engine.EmbedByJson("sets", convertedData.Sets);
                engine.EmbedByJson("skills", convertedData.Skills);
                engine.EmbedByJson("weapons", new object[0]);

                foreach (string path in paths)
                {
                    string js = File.ReadAllText(path);
                    js = js.Replace("window.addEventListener('load', execute);", string.Empty);
                    engine.Execute(js);
                }

                int[][] abilityMap = solverData.DesiredAbilities.Where(x => x.Level > 0).Select(x => new int[]{x.Skill.Id, x.Level}).ToArray();
                string abilityJson = JsonConvert.SerializeObject(abilityMap);

                string resultJson = engine.CallFunction<string>("run", abilityJson);
                IList<ArmorSetSearchResult> results = parser.ParseResults(resultJson, solverData);
                return results;
            }
        }

        private IList<string> GetJsFilesRecursive(string path, IList<string> list = null)
        {
            list = list ?? new List<string>();
            string[] dirs = Directory.GetDirectories(path);
            foreach (string dir in dirs)
            {
                GetJsFilesRecursive(dir, list);
            }
            string[] filePaths = Directory.GetFiles(path);
            foreach (string filePath in filePaths)
            {
                list.Add(filePath);
            }
            return list;
        }
    }
}
