using GameServer.Game;
using Google.Protobuf.Protocol;

namespace GameServer
{
	public class Zone
	{
		public int IndexX { get; private set; }
		public int IndexY { get; private set; }

		public HashSet<Hero> Heroes { get; set; } = new HashSet<Hero>();
        public HashSet<Monster> Monsters { get; set; } = new HashSet<Monster>();
        public HashSet<BaseObject> Objects { get; set; } = new HashSet<BaseObject>();

        public Zone(int x, int y)
		{
			IndexX = x;
			IndexY = y;
		}

		public void Remove(BaseObject obj)
		{
			EGameObjectType type = ObjectManager.GetObjectTypeFromId(obj.ObjectId);

			switch (type)
			{
				case EGameObjectType.Hero:
					Heroes.Remove((Hero)obj);
					break;
                case EGameObjectType.Monster:
                    Monsters.Remove((Monster)obj);
                    break;
				case EGameObjectType.Npc:
					Objects.Remove((Npc)obj);
					break;
            }
		}
	}
}
