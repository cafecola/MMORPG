using Google.Protobuf.Protocol;

namespace GameServer
{
	public class VisionCubeComponent
	{
		public Hero Owner { get; private set; }
		public HashSet<BaseObject> PreviousObjects { get; private set; } = [];
		public IJob UpdateJob;

		public VisionCubeComponent(Hero owner)
		{
			Owner = owner;
		}

		public HashSet<BaseObject> GatherObjects()
		{
			if (Owner == null || Owner.Room == null)
				return null;

			HashSet<BaseObject> objects = new HashSet<BaseObject>();

			List<Zone> zones = Owner.Room.GetAdjacentZones(Owner.CellPos);

			Vector2Int cellPos = Owner.CellPos;
			foreach (Zone zone in zones)
			{
				foreach (Hero hero in zone.Heroes)
				{
					int dx = hero.CellPos.x - cellPos.x;
					int dy = hero.CellPos.y - cellPos.y;
					if (Math.Abs(dx) > GameRoom.VisionCells)
						continue;
					if (Math.Abs(dy) > GameRoom.VisionCells)
						continue;
					objects.Add(hero);
				}

                foreach (Monster monster in zone.Monsters)
                {
                    int dx = monster.CellPos.x - cellPos.x;
                    int dy = monster.CellPos.y - cellPos.y;
                    if (Math.Abs(dx) > GameRoom.VisionCells)
                        continue;
                    if (Math.Abs(dy) > GameRoom.VisionCells)
                        continue;

                    objects.Add(monster);
                }

                foreach (BaseObject obj in zone.Objects)
                {
                    int dx = obj.CellPos.x - cellPos.x;
                    int dy = obj.CellPos.y - cellPos.y;
                    if (Math.Abs(dx) > GameRoom.VisionCells)
                        continue;
                    if (Math.Abs(dy) > GameRoom.VisionCells)
                        continue;

                    objects.Add(obj);
                }
            }

			return objects;
		}

		public void Update()
		{
			if (Owner == null || Owner.Room == null)
				return;

			HashSet<BaseObject> currentObjects = GatherObjects();

			// 기존엔 없었는데 새로 생긴 Object Spawn 처리
			List<BaseObject> added = currentObjects.Except(PreviousObjects).ToList();
			if (added.Count > 0)
			{
				S_Spawn spawnPacket = new S_Spawn();

				foreach (BaseObject obj in added)
				{
					if (obj.ObjectType == EGameObjectType.Hero)
					{
						Hero hero = (Hero)obj;
						HeroInfo info = new HeroInfo(); // TODO CHECK
						info.MergeFrom(hero.HeroInfoComp.HeroInfo);
						spawnPacket.Heroes.Add(info);
					}
					else if (obj.ObjectType == EGameObjectType.Monster)
					{ 
						Monster monster = (Monster)obj;
						CreatureInfo info = new CreatureInfo();
						info.MergeFrom(monster.CreatureInfo);
						spawnPacket.Creatures.Add(info);
					}
                    else
                    {
                        ObjectInfo info = new ObjectInfo();
                        info.MergeFrom(obj.ObjectInfo);
                        spawnPacket.Objects.Add(info);
                    }
                }

				Owner.Session?.Send(spawnPacket);
			}

			// 기존엔 있었는데 사라진 Object Despawn 처리
			List<BaseObject> removed = PreviousObjects.Except(currentObjects).ToList();
			if (removed.Count > 0)
			{
				S_Despawn despawnPacket = new S_Despawn();

				foreach (BaseObject obj in removed)
				{
					despawnPacket.ObjectIds.Add(obj.ObjectId);
				}

				Owner.Session?.Send(despawnPacket);
			}

			// 교체
			PreviousObjects = currentObjects;

			UpdateJob = Owner.Room.PushAfter(100, Update);
		}

		public void Clear()
		{
			PreviousObjects.Clear();
		}
	}
}
