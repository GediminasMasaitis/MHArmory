using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MHArmory.Core.DataStructures;
using MHArmory.Search.Contracts;
using Newtonsoft.Json;

namespace MHArmory.Search.MHOSEF
{
    class ResultParser
    {
        public static ResultParser Instance { get; } = new ResultParser();

        private Regex armorRegex;
        private Regex jewelRegex;

        private ResultParser()
        {
            armorRegex = new Regex(@"^\[(\d+)\]", RegexOptions.Compiled);
            jewelRegex = new Regex(@"^\[(\d+)\].+\* (\d+):", RegexOptions.Compiled);
        }

        public IList<ArmorSetSearchResult> ParseResults(string resultJson, ISolverData solverData)
        {
            string[] strings = JsonConvert.DeserializeObject<string[]>(resultJson);

            var armorPieces = solverData.AllHeads
                .Concat(solverData.AllChests)
                .Concat(solverData.AllGloves)
                .Concat(solverData.AllWaists)
                .Concat(solverData.AllLegs)
                .Select(x => (IArmorPiece)x.Equipment)
                .ToDictionary(x => x.GetHashedId());

            var charms = solverData.AllCharms.Select(x => ((ICharmLevel) x.Equipment).Charm).Distinct().ToDictionary(x => x.Id);
            var jewels = solverData.AllJewels.Select(x => x.Jewel).ToDictionary(x => x.Id);

            var results = strings.Select(x => ParseResult(x, armorPieces, charms, jewels)).ToList();
            return results;
        }

        private ArmorSetSearchResult ParseResult(string resultStr, Dictionary<int, IArmorPiece> armorPieces, IDictionary<int, ICharm> charms, Dictionary<int, IJewel> jewels)
        {
            var reader = new StringReader(resultStr);
            var result = new ArmorSetSearchResult();
            result.IsMatch = true;
            result.ArmorPieces = new List<IArmorPiece>();
            result.Jewels = new List<ArmorSetJewelResult>();
            result.SpareSlots = new int[3];
            ParserMode mode = ParserMode.None;
            while (true)
            {
                string line = reader.ReadLine();
                if (string.IsNullOrEmpty(line))
                {
                    return result;
                }

                if (line == "ITEMS")
                {
                    mode = ParserMode.ReadingItems;
                    continue;
                }
                else if (line == "JEWELS")
                {
                    mode = ParserMode.ReadingJewels;
                    continue;
                }
                else if (line[0] != '[')
                {
                    return result;
                }

                switch (mode)
                {
                    case ParserMode.ReadingItems:
                        Match armorMatch = armorRegex.Match(line);
                        string armorIdStr = armorMatch.Groups[1].Value;
                        int armorId = int.Parse(armorIdStr);
                        if (armorId < 1 << 16)
                        {
                            result.Charm = charms[armorId].Levels.Last();
                        }
                        else
                        {
                            result.ArmorPieces.Add(armorPieces[armorId]);
                        }
                        break;
                    case ParserMode.ReadingJewels:
                        Match jewelMatch = jewelRegex.Match(line);
                        string jewelIdStr = jewelMatch.Groups[1].Value;
                        string jewelCountStr = jewelMatch.Groups[2].Value;
                        int jewelId = int.Parse(jewelIdStr);
                        var jewelResult = new ArmorSetJewelResult();
                        jewelResult.Jewel = jewels[jewelId];
                        jewelResult.Count = int.Parse(jewelCountStr);
                        result.Jewels.Add(jewelResult);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private enum ParserMode
        {
            None,
            ReadingItems,
            ReadingJewels,
        }
    }
}
