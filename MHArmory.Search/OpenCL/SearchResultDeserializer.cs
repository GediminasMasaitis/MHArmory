using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MHArmory.Core.DataStructures;

namespace MHArmory.Search.OpenCL
{
    internal class SearchResultDeserializer
    {
        public List<ArmorSetSearchResult> Deserialize(ISolverData data, SearchIDMaps maps, SerializedSearchResults serializedResults)
        {
            var equipmentDict = data.AllHeads.Concat(data.AllChests).Concat(data.AllGloves).Concat(data.AllWaists).Concat(data.AllLegs).Concat(data.AllCharms).Select(x => x.Equipment).ToDictionary(x => new Tuple<int, EquipmentType>(x.Id, x.Type));
            var decoDict = data.AllJewels.ToDictionary(x => x.Jewel.Id);

            var results = new List<ArmorSetSearchResult>();

            var ms = new MemoryStream(serializedResults.Data);
            var reader = new BinaryReader(ms);

            for (int i = 0; i < serializedResults.ResultCount; i++)
            {
                var result = new ArmorSetSearchResult();
                result.ArmorPieces = new List<IArmorPiece>();
                result.Jewels = new List<ArmorSetJewelResult>();
                result.IsMatch = true;

                for (int j = 0; j < 5; j++)
                {
                    ushort id = reader.ReadUInt16();
                    var tuple = new Tuple<int, EquipmentType>(id, (EquipmentType) j + 1);
                    var armor = (IArmorPiece)equipmentDict[tuple];
                    result.ArmorPieces.Add(armor);
                }

                ushort charmId = reader.ReadUInt16();
                var charmTuple = new Tuple<int, EquipmentType>(charmId, EquipmentType.Charm);
                result.Charm = (ICharmLevel)equipmentDict[charmTuple];

                byte decorationCount = reader.ReadByte();
                for (int j = 0; j < decorationCount; j++)
                {
                    ushort id = reader.ReadUInt16();
                    byte count = reader.ReadByte();
                    IJewel jewel = decoDict[id].Jewel;
                    var jewelResult = new ArmorSetJewelResult
                    {
                        Jewel = jewel,
                        Count = count
                    };
                    result.Jewels.Add(jewelResult);
                }

                int remainingDecos = 7 * 3 - decorationCount;
                reader.ReadBytes(remainingDecos * 3);
                result.SpareSlots = new int[3];

                results.Add(result);
            }
            return results;
        }
    }
}
