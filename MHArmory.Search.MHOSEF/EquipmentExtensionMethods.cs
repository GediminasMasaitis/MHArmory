using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MHArmory.Core.DataStructures;

namespace MHArmory.Search.MHOSEF
{
    internal static class EquipmentExtensionMethods
    {
        public static int GetHashedId(this IEquipment equipment)
        {
            return equipment.Id << 16 | (int)equipment.Type;
        }
    }
}
