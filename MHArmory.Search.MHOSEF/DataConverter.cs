using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MHArmory.Core.DataStructures;
using MHArmory.Search.Contracts;

namespace MHArmory.Search.MHOSEF
{
    class ConvertedArmor
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public IList<ConvertedSlot> Slots { get; set; }
        public IList<ConvertedSkill> Skills { get; set; }
    }

    class ConvertedJewel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Slot { get; set; }
        public IList<ConvertedSkill> Skills { get; set; }
    }

    class ConvertedSlot
    {
        public int Rank { get; set; }
    }

    class ConvertedSkill
    {
        public int Id { get; set; }
        public int Level { get; set; }
        public int Skill { get; set; }
        public string SkillName { get; set; }
    }

    class ConvertedData
    {
        public IList<ConvertedArmor> Armors { get; set; }
        public IList<ConvertedArmor> Charms { get; set; }
        public IList<ConvertedJewel> Jewels { get; set; }
    }

    internal class DataConverter
    {
        public ConvertedData ConvertData(ISolverData solverData)
        {
            var convertedData = new ConvertedData();
            convertedData.Armors = ConvertArmors(solverData.AllHeads, solverData.AllChests, solverData.AllGloves, solverData.AllWaists, solverData.AllLegs);
            convertedData.Charms = ConvertArmors(solverData.AllCharms);
            convertedData.Jewels = ConvertJewels(solverData.AllJewels);
            return convertedData;
        }

        private IList<ConvertedJewel> ConvertJewels(IEnumerable<SolverDataJewelModel> jewels)
        {
            return jewels.Select(x => x.Jewel).Select(ConvertJewel).ToList();
        }

        public int GetEquipmentId(IEquipment equipment)
        {
            return equipment.Id << 16 | (int)equipment.Type;
        }

        public IList<ConvertedArmor> ConvertArmors(params IEnumerable<ISolverDataEquipmentModel>[] models)
        {
            var convertedArmorPieces = models
                .SelectMany(x => x)
                .Where(x => x.IsSelected)
                .Select(x => x.Equipment)
                .Select(ConvertArmor)
                .ToList();
            return convertedArmorPieces;
        }

        private ConvertedJewel ConvertJewel(IJewel jewel)
        {
            var convertedArmor = new ConvertedJewel();
            convertedArmor.Id = jewel.Id;
            convertedArmor.Name = jewel.Name["EN"];
            convertedArmor.Skills = ConvertSkills(jewel.Abilities);
            convertedArmor.Slot = jewel.SlotSize;
            return convertedArmor;
        }

        private ConvertedArmor ConvertArmor(IEquipment armorPiece)
        {
            var convertedArmor = new ConvertedArmor();
            convertedArmor.Id = GetEquipmentId(armorPiece);
            convertedArmor.Name = armorPiece.Name["EN"];
            convertedArmor.Skills = ConvertSkills(armorPiece.Abilities);
            convertedArmor.Slots = ConvertSlots(armorPiece.Slots);
            convertedArmor.Type = armorPiece.Type.ToString().ToLowerInvariant();
            return convertedArmor;
        }

        public IList<ConvertedSlot> ConvertSlots(int[] slots)
        {
            var convertedSlots = new List<ConvertedSlot>();
            foreach (int slot in slots)
            {
                var convertedSlot = new ConvertedSlot();
                convertedSlot.Rank = slot;
                convertedSlots.Add(convertedSlot);
            }
            return convertedSlots;
        }

        public IList<ConvertedSkill> ConvertSkills(IEnumerable<IAbility> abilities)
        {
            var convertedSkills = new List<ConvertedSkill>();
            foreach (IAbility ability in abilities)
            {
                var convertedSkill = new ConvertedSkill();
                convertedSkill.Id = ability.Id;
                convertedSkill.Level = ability.Level;
                convertedSkill.Skill = ability.Skill.Id;
                convertedSkill.SkillName = ability.Skill.Name["EN"];
                convertedSkills.Add(convertedSkill);
            }
            return convertedSkills;
        }
    }
}
