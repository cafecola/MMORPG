using GameServer.Game;
using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server;
using Server.Data;
using Server.Game;

namespace GameServer
{
	public partial class EmptyRoom : GameRoom
	{
		public override IJob PushAfter(int tickAfter, IJob job)
		{
			// Do Nothing
			return null;
		}

		public override void Push(IJob job)
		{
			// Do Nothing
		}

		public override void EnterGame(BaseObject obj, Vector2Int cellPos, bool respawn = false)
        {
            // Do Nothing
		}

		public override void LeaveGame(int objectId, ELeaveType leaveType = ELeaveType.None)
        {
			// Do Nothing
		}

		public override void Broadcast(Vector2Int pos, IMessage packet)
		{
			// Do Nothing
		}
	}

	public partial class GameRoom : JobSerializer, IEquatable<GameRoom>
    {
		public const int VisionCells = 15;
		public static EmptyRoom S_EmptyRoom { get; } = new EmptyRoom();
        public int RoomId { get; set; }
        public int TemplateId { get; set; }
        public RoomData RoomData { get; set; }
		public MapComponent Map { get; private set; } = new MapComponent();
        public SpawningPoolComponent SpawningPool { get; private set; } = new SpawningPoolComponent();

        // GameRoom 공간을 Zone 단위로 균일하게 세분화
        public Zone[,] Zones { get; private set; } // 인근 오브젝트를 빠르게 찾기 위한 캐시.
        public int ZoneCells { get; private set; } // 하나의 존을 구성하는 셀 개수

        private Dictionary<int, Hero> _heroes = [];
        private Dictionary<int, Monster> _monsters = [];
        private Dictionary<int, Npc> _npcs = [];
        private Dictionary<int, Projectile> _projectiles = [];

        public void Init(RoomData roomData, int zoneCells)
        {
            RoomData = roomData;
            TemplateId = roomData.TemplateId;

            Map.LoadMap(roomData.PrefabName);

            // Zone
			// 10
			// 1~10 칸 = 1존
			// 11~20칸 = 2존
			// 21~30칸 = 3존
            ZoneCells = zoneCells;

            int countX = (Map.SizeX + zoneCells - 1) / zoneCells;
            int countY = (Map.SizeY + zoneCells - 1) / zoneCells;
            Zones = new Zone[countX, countY];
            for (int x = 0; x < countX; x++)
            {
                for (int y = 0; y < countY; y++)
                {
                    Zones[x, y] = new Zone(x, y);
                }
            }

            SpawningPool.Init(this);
            Push(SpawningPool.Update);
        }

		/// <summary>
		/// 누군가 주기적으로 호출해줘야 한다
		/// </summary>
        public void Update()
        {
            Flush();
        }

        public virtual void EnterGame(BaseObject obj, Vector2Int cellPos, bool respawn = false)
        {
            if (obj == null)
                return;
            if (obj.Room != null && obj.Room != this)
				return;

            EGameObjectType type = ObjectManager.GetObjectTypeFromId(obj.ObjectId);

            if (type == EGameObjectType.Hero)
            {
                Hero hero = (Hero)obj;

                if (_heroes.ContainsKey(obj.ObjectId))
                    return;

                // 오브젝트 추가.
                _heroes.Add(obj.ObjectId, hero);
                hero.Room = this;
                hero.HeroInfoComp.MyHeroInfo.MapId = RoomId;

                // 좌표 설정.
                FindAndSetCellPos(obj, cellPos);

                // 맵에 실제 적용하고 충돌 그리드 갱신.
                Map.ApplyMove(hero, hero.CellPos);

                // 캐싱된 존에도 해당 정보 추가.
                GetZone(hero.CellPos).Heroes.Add(hero);

                // Tick Start.
                hero.State = EObjectState.Idle;
                hero.Update();

                // 입장한 사람한테 보내는 패킷.
                {
                    S_EnterGame enterPacket = new S_EnterGame();
                    enterPacket.MyHeroInfo = hero.HeroInfoComp.MyHeroInfo;
                    enterPacket.Respawn = respawn;

                    foreach (var info in hero.Inven.GetAllItemInfos())
                    {
                        enterPacket.Items.Add(info);
                    }

                    // skill
                    List<SkillCoolTime> cooltimes = hero.SkillComp.GetRemainingTicks();
                    foreach (SkillCoolTime cooltime in cooltimes)
                        enterPacket.Cooltimes.Add(cooltime);

                    hero.Session?.Send(enterPacket);
                }

                // 다른 사람들한테 입장 알려주기.
                S_Spawn spawnPacket = new S_Spawn();
                spawnPacket.Heroes.Add(hero.HeroInfoComp.HeroInfo);
                Broadcast(obj.CellPos, spawnPacket);

                // AOI Tick Start.
                hero.Vision?.Update();
            }
            else if (type == EGameObjectType.Monster)
            {
                Monster monster = (Monster)obj;

                // 오브젝트 추가.
                _monsters.Add(obj.ObjectId, monster);
                monster.Room = this;

                FindAndSetCellPos(obj, cellPos);

                Map.ApplyMove(monster, monster.CellPos);

                GetZone(monster.CellPos).Monsters.Add(monster);

                // Tick Start.
                monster.State = EObjectState.Idle;
                monster.SpawnPosition = cellPos;
                monster.Update();

                // 다른 사람들한테 입장 알려주기.
                S_Spawn spawnPacket = new S_Spawn();
                spawnPacket.Creatures.Add(monster.CreatureInfo);
                Broadcast(obj.CellPos, spawnPacket);
            }
            else if (type == EGameObjectType.Projectile)
            {
                Projectile projectile = (Projectile)obj;

				// 오브젝트 추가.
                _projectiles.Add(obj.ObjectId, projectile);
                projectile.Room = this;

                // 좌표 설정.
                projectile.CellPos = cellPos;

                // Tick Start.
                projectile.State = EObjectState.Move;
                projectile.Update();

                // 다른 사람들한테 알림.
                S_Spawn spawnPacket = new S_Spawn();
                spawnPacket.Projectiles.Add(projectile.ProjectileInfo);
                Broadcast(projectile.CellPos, spawnPacket);
            }
            else if (type == EGameObjectType.Npc)
            {
                Npc npc = (Npc)obj;

                // 오브젝트 추가.
                _npcs.Add(obj.ObjectId, npc);
                npc.Room = this;

                // 좌표 설정.
                FindAndSetCellPos(obj, cellPos);

                Map.ApplyMove(npc, npc.CellPos);

                GetZone(npc.CellPos).Objects.Add(npc);

                // State 설정
                npc.State = EObjectState.Idle;

            }
        }

        public virtual void LeaveGame(int objectId, ELeaveType leaveType = ELeaveType.None)
        {
			EGameObjectType type = ObjectManager.GetObjectTypeFromId(objectId);

            if (type == EGameObjectType.Hero)
            {
                if (_heroes.TryGetValue(objectId, out Hero hero) == false)
                    return;

				if (hero.Room != null && hero.Room != this)
					return;

				Map.ApplyLeave(hero);

                // 오브젝트 제거
                _heroes.Remove(objectId);
                hero.Room = GameRoom.S_EmptyRoom;

                {
                    S_LeaveGame leavePacket = new S_LeaveGame();
                    leavePacket.LeaveType = leaveType;
                    hero.Session?.Send(leavePacket);
                }

                S_Despawn despawnPacket = new S_Despawn();
                despawnPacket.ObjectIds.Add(objectId);
                Broadcast(hero.CellPos, despawnPacket);

                // AOI 정리.
                hero.Vision?.Clear();

                // DB에 좌표 등 정보 저장.
                DBManager.SaveHeroDbNoti(hero);

                // 예약한 job 취소
                if (leaveType == ELeaveType.Disconnected)
                {
                    hero.CancelJobs();
                }

            }
            else if (type == EGameObjectType.Monster)
            {
                if (_monsters.TryGetValue(objectId, out Monster monster) == false)
                    return;

                // 충돌 그리드 갱신.
                Map.ApplyLeave(monster);

                // 오브젝트 제거.
                _monsters.Remove(objectId);
                monster.Room = GameRoom.S_EmptyRoom;

                S_Despawn despawnPacket = new();
                despawnPacket.ObjectIds.Add(objectId);
                Broadcast(monster.CellPos, despawnPacket);

            }
            else if (type == EGameObjectType.Projectile)
            {
                if (_projectiles.TryGetValue(objectId, out Projectile projectile) == false)
                    return;

                // 오브젝트 제거.
                _projectiles.Remove(objectId);
                projectile.Room = GameRoom.S_EmptyRoom;
            }
            else if (type == EGameObjectType.Npc)
            {
                if (_npcs.TryGetValue(objectId, out Npc npc) == false)
                    return;

                //오브젝트 제거.
                _npcs.Remove(objectId);
                npc.Room = GameRoom.S_EmptyRoom;

                S_Despawn despawnPacket = new();
                despawnPacket.ObjectIds.Add(objectId);
                Broadcast(npc.CellPos, despawnPacket);
            }
            else
            {
                return;
            }
        }

        public Zone GetZone(Vector2Int cellPos)
        {
            int x = (cellPos.x - Map.MinX) / ZoneCells;
            int y = (Map.MaxY - cellPos.y) / ZoneCells;

            return GetZone(x, y);
        }

        public Zone GetZone(int indexX, int indexY)
        {
            if (indexX < 0 || indexX >= Zones.GetLength(0))
                return null;
            if (indexY < 0 || indexY >= Zones.GetLength(1))
                return null;

            return Zones[indexX, indexY];
        }

        public virtual void Broadcast(Vector2Int pos, IMessage packet)
        {
            List<Zone> zones = GetAdjacentZones(pos);
            if (zones.Count == 0)
                return;

            byte[] packetBuffer = ClientSession.MakeSendBuffer(packet);

            foreach (Hero hero in zones.SelectMany(z => z.Heroes))
            {
                int dx = hero.CellPos.x - pos.x;
                int dy = hero.CellPos.y - pos.y;
                if (Math.Abs(dx) > GameRoom.VisionCells)
                    continue;
                if (Math.Abs(dy) > GameRoom.VisionCells)
                    continue;

                hero.Session?.Send(packetBuffer);
            }
        }

        public List<Zone> GetAdjacentZones(Vector2Int cellPos, int cells = GameRoom.VisionCells)
        {
            HashSet<Zone> zones = new HashSet<Zone>();

            int maxY = cellPos.y + cells;
            int minY = cellPos.y - cells;
            int maxX = cellPos.x + cells;
            int minX = cellPos.x - cells;

            // 좌측 상단
            Vector2Int leftTop = new Vector2Int(minX, maxY);
            int minIndexY = (Map.MaxY - leftTop.y) / ZoneCells;
            int minIndexX = (leftTop.x - Map.MinX) / ZoneCells;

            // 우측 하단
            Vector2Int rightBot = new Vector2Int(maxX, minY);
            int maxIndexY = (Map.MaxY - rightBot.y) / ZoneCells;
            int maxIndexX = (rightBot.x - Map.MinX) / ZoneCells;

            for (int x = minIndexX; x <= maxIndexX; x++)
            {
                for (int y = minIndexY; y <= maxIndexY; y++)
                {
                    Zone zone = GetZone(x, y);
                    if (zone == null)
                        continue;

                    zones.Add(zone);
                }
            }

            return zones.ToList();
        }

        public Vector2Int? GetNearbyPosition(BaseObject obj, Vector2Int pivot)
        {
            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

            queue.Enqueue(pivot);
            visited.Add(pivot);

            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();

                if (Map.CanGo(obj, current))
                    return current;

                List<Vector2Int> neighbors =
				[
					new(current.x - 1, current.y),
                    new(current.x + 1, current.y),
                    new(current.x, current.y - 1),
                    new(current.x, current.y + 1)
                ];

                neighbors.Shuffle();

                foreach (Vector2Int neighbor in neighbors)
                {
                    if (!visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }

            return null;
        }

        public Hero FindAnyHero(Func<BaseObject, bool> condition)
        {
            foreach (Hero hero in _heroes.Values)
            {
                if (condition.Invoke(hero))
                    return hero;
            }

            return null;
        }

        public T FindAdjacent<T>(Vector2Int pos, Func<T, bool> condition = null, int cells = GameRoom.VisionCells) where T : BaseObject
        {
            List<Zone> zones = GetAdjacentZones(pos, cells);

            if (typeof(T) == typeof(Hero))
            {
                foreach (Hero p in zones.SelectMany(z => z.Heroes))
                {
                    int dx = p.CellPos.x - pos.x;
                    int dy = p.CellPos.y - pos.y;
                    if (Math.Abs(dx) > GameRoom.VisionCells)
                        continue;
                    if (Math.Abs(dy) > GameRoom.VisionCells)
                        continue;
                    if (condition == null || condition.Invoke(p as T) == false)
                        continue;

                    return p as T;
                }
            }
            else if (typeof(T) == typeof(Monster))
            {
                foreach (Monster m in zones.SelectMany(z => z.Monsters))
                {
                    int dx = m.CellPos.x - pos.x;
                    int dy = m.CellPos.y - pos.y;
                    if (Math.Abs(dx) > GameRoom.VisionCells)
                        continue;
                    if (Math.Abs(dy) > GameRoom.VisionCells)
                        continue;
                    if (condition == null || condition.Invoke(m as T) == false)
                        continue;

                    return m as T;
                }
            }

            return null;
        }

        public List<Hero> FindAdjacentHeroes(Vector2Int pos, Func<Hero, bool> condition = null, int cells = GameRoom.VisionCells)
        {
            List<Hero> objs = new List<Hero>();
            List<Zone> zones = GetAdjacentZones(pos, cells);

            foreach (Hero hero in zones.SelectMany(z => z.Heroes))
            {
                int dx = hero.CellPos.x - pos.x;
                int dy = hero.CellPos.y - pos.y;
                if (Math.Abs(dx) > GameRoom.VisionCells)
                    continue;
                if (Math.Abs(dy) > GameRoom.VisionCells)
                    continue;
                if (condition == null || condition.Invoke(hero) == false)
                    continue;

                objs.Add(hero);
            }

            return objs;
        }

        public List<Monster> FindAdjacentMonsters(Vector2Int pos, Func<Monster, bool> condition = null, int cells = GameRoom.VisionCells)
        {
            List<Monster> objs = new List<Monster>();
            List<Zone> zones = GetAdjacentZones(pos, cells);

            foreach (Monster monster in zones.SelectMany(z => z.Monsters))
            {
                int dx = monster.CellPos.x - pos.x;
                int dy = monster.CellPos.y - pos.y;
                if (Math.Abs(dx) > GameRoom.VisionCells)
                    continue;
                if (Math.Abs(dy) > GameRoom.VisionCells)
                    continue;
                if (condition == null || condition.Invoke(monster) == false)
                    continue;

                objs.Add(monster);
            }

            return objs;
        }

        public List<Creature> FindAdjacentCreatures(Vector2Int pos, Func<Creature, bool> condition = null, int cells = GameRoom.VisionCells)
        {
            List<Creature> objs = new List<Creature>();
            objs.AddRange(FindAdjacentHeroes(pos, condition, cells));
            objs.AddRange(FindAdjacentMonsters(pos, condition, cells));
            return objs;
        }

        public Hero GetHeroById(int id)
        {
            _heroes.TryGetValue(id, out Hero hero);
            return hero;
        }

        public Monster GetMonsterById(int id)
        {
            _monsters.TryGetValue(id, out Monster monster);
            return monster;
        }

        public Creature GetCreatureById(int id)
        {
            Hero p = GetHeroById(id);
            if (p != null)
                return p as Creature;

            Monster m = GetMonsterById(id);
            if (m != null)
                return m as Creature;

            return null;
        }

        private void FindAndSetCellPos(BaseObject obj, Vector2Int pos)
        {
            if (Map.CanGo(obj, pos, checkObjects: true))
                obj.CellPos = pos;
            else
            {
                Vector2Int? nearby = GetNearbyPosition(obj, pos);
                if (nearby.HasValue)
                { 
                    obj.CellPos = nearby.Value;
                }
            }
        }

		#region IEquatable
		public static bool operator ==(GameRoom a, GameRoom b)
		{
			if (ReferenceEquals(a, b))
				return true;

			if (ReferenceEquals(a, null) && ReferenceEquals(b, S_EmptyRoom))
                return true;

			if (ReferenceEquals(a, S_EmptyRoom) && ReferenceEquals(b, null))
				return true;

			return a.Equals(b);
		}

		public static bool operator !=(GameRoom a, GameRoom b)
		{
			return !(a == b);
		}

		public override bool Equals(object obj)
		{
			return Equals(obj as GameRoom);
		}

		public bool Equals(GameRoom other)
		{
			if (ReferenceEquals(other, null))
				return false;
			if (ReferenceEquals(this, other))
				return true;

			return RoomId.Equals(other.RoomId) && TemplateId.Equals(other.TemplateId);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return RoomId.GetHashCode();
			}
		}
		#endregion
	}
}
