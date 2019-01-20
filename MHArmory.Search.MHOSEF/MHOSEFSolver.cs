using System;
using System.Collections.Generic;
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
        public string Name { get; } = "MHOSEF";

        public string Author { get; } = "Delca";

        public string Description { get; } = "description";

        public int Version { get; } = 1;

        public event Action<double> SearchProgress;

        public async Task<IList<ArmorSetSearchResult>> SearchArmorSets(ISolverData solverData, CancellationToken cancellationToken)
        {
            SearchProgress?.Invoke(0);
            IList<ArmorSetSearchResult> results = new List<ArmorSetSearchResult>();

            string mhosefPath = Path.Combine(Directory.GetCurrentDirectory(), "mhosef/js");
            IList<string> paths = GetJsFilesRecursive(mhosefPath);

            var converter = new DataConverter();
            ConvertedData convertedData = converter.ConvertData(solverData);

            using (IJsEngine engine = EngineManager.Instance.GetEngine())
            {
                engine.Execute("window = this;");

                engine.EmbedHostObject("armours", convertedData.Armors);
                engine.EmbedHostObject("charms", convertedData.Charms);
                engine.EmbedHostObject("jewels", convertedData.Jewels);

                foreach (string path in paths)
                {
                    string js = File.ReadAllText(path);
                    js = js.Replace("window.addEventListener('load', execute);", string.Empty);
                    engine.Execute(js);
                }

                int[][] abilityMap = solverData.DesiredAbilities.Where(x => x.Level > 0).Select(x => new int[]{x.Skill.Id, x.Level}).ToArray();
                string abilityJson = JsonConvert.SerializeObject(abilityMap);

                string result = engine.CallFunction<string>("run", abilityJson);
            }

            return results;
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
