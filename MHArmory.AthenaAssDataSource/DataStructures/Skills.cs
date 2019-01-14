using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MHArmory.Core;
using MHArmory.Core.DataStructures;

namespace MHArmory.AthenaAssDataSource.DataStructures
{
    internal class SkillPrimitiveLowLevel
    {
        internal string Name = null;
        [Name("Max Level")]
        internal int MaxLevel = 0;
    }

    internal class SkillPrimitiveHighLevel
    {
        internal Dictionary<string, string> Name;
        internal int MaxLevel = 0;
    }

    internal class Skill : ISkill
    {
        public Skill(int id, Dictionary<string, string> description, SkillPrimitiveHighLevel skillPrimitive)
        {
            Id = id;
            Name = skillPrimitive.Name;
            Description = description;
            MaxLevel = skillPrimitive.MaxLevel;
            Abilities = Enumerable.Range(1, skillPrimitive.MaxLevel)
                .Select(i => new Ability(this, i, CreateAbilityDescriptions(Name, i)))
                .ToArray();
        }

        public Skill(int id, Dictionary<string, string> name, Dictionary<string, string> description, IAbility ability)
        {
            Id = id;
            Name = name;
            Description = description;
            MaxLevel = ability.Skill.MaxLevel;
            Abilities = new IAbility[] { ability };
        }

        public int Id { get; }
        public Dictionary<string, string> Name { get; }
        public Dictionary<string, string> Description { get; private set; }
        public int MaxLevel { get; }
        public IAbility[] Abilities { get; }

        public void UpdateDescription(Dictionary<string, string> description)
        {
            Description = description;
        }

        public override string ToString()
        {
            return $"{Localization.GetDefault(Name)} ({Abilities.Length} abilities)";
        }

        private static Dictionary<string, string> CreateAbilityDescriptions(Dictionary<string, string> names, int level)
        {
            return names.ToDictionary(kv1 => kv1.Key, kv2 => $"{kv2.Value} level {level}");
        }
    }
}
