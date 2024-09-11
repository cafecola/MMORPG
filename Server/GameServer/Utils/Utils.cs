using System.Net.Sockets;
using System.Net;
using Google.Protobuf.Protocol;

namespace GameServer
{
    public static class Utils
    {
        public static long TickCount { get { return System.Environment.TickCount64; } }

		public static Dictionary<EItemSubType, EItemSlotType> SubTypeToEquipTypeMap = new()
		{
			{ EItemSubType.Mainweapon,  EItemSlotType.Mainweapon },
			{ EItemSubType.Subweapon,   EItemSlotType.Subweapon} ,
			{ EItemSubType.Helmet,      EItemSlotType.Helmet },
			{ EItemSubType.Chest,       EItemSlotType.Chest },
			{ EItemSubType.Leg,         EItemSlotType.Leg },
			{ EItemSubType.Shoes,       EItemSlotType.Shoes },
			{ EItemSubType.Gloves,      EItemSlotType.Gloves },
			{ EItemSubType.Shoulder,    EItemSlotType.Shoulder },
			{ EItemSubType.Ring,        EItemSlotType.Ring },
			{ EItemSubType.Amulet,      EItemSlotType.Amulet },
		};

        public static IPAddress GetLocalIP()
        {
            var ipHost = Dns.GetHostEntry(Dns.GetHostName());

            foreach (IPAddress ip in ipHost.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip;
                }
            }

            return IPAddress.Loopback;
        }

		public static EItemSlotType GetEquipSlotType(EItemSubType subType)
        {
            if (SubTypeToEquipTypeMap.TryGetValue(subType, out EItemSlotType value))
                return value;

            return EItemSlotType.None;
        }

        public static T RandomElementByWeight<T>(this IEnumerable<T> sequence, Func<T, float> weightSelector)
        {
            float totalWeight = sequence.Sum(weightSelector);
            double itemWeightIndex = new Random().NextDouble() * Define.RANDOM_WEIGHT_SCALE;
            float currentWeightIndex = 0;

            foreach (var item in from weightedItem in sequence select new { Value = weightedItem, Weight = weightSelector(weightedItem) })
            {
                currentWeightIndex += item.Weight;

                if (currentWeightIndex >= itemWeightIndex)
                    return item.Value;

            }
            return default(T);
        }

        public static int GetDistance(Vector2Int a, Vector2Int b)
        {
            return Math.Max(Math.Abs(a.x - b.x), Math.Abs(a.y - b.y));
        }
    }
}
