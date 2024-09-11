using Google.Protobuf.Protocol;
using ServerCore;
using System.Numerics;

namespace GameServer
{
    /// <summary>
    /// 지형과 오브젝트 충돌 및 길찾기 처리
    /// </summary>
    public class MapComponent
    {
        public int MinX { get; set; }
        public int MaxX { get; set; }
        public int MinY { get; set; }
        public int MaxY { get; set; }

        public int SizeX { get { return MaxX - MinX + 1; } }
        public int SizeY { get { return MaxY - MinY + 1; } }

        /// <summary>
        /// 맵 자체에 충돌이 있는지(벽, 장애물) 체크하기 위함
        /// </summary>
        private bool[,] _mapCollision;

        /// <summary>
        /// 특정 위치에 오브젝트가 있는지 체크하기 위함
        /// </summary>
        private BaseObject[,] _objCollision;

		private List<Vector2Int> _delta =
		[
            //EMoveDir의 순서와 맞춤
            new(1, 1), // U
		    new(-1, -1), // D
		    new(-1, 1), // L
		    new(1, -1), // R
		    new(0, 1), // UL
		    new(1, 0), // UR
		    new(-1, 0), // DL
		    new(0, -1), // DR
		];

        public bool CanGo(BaseObject self, Vector2Int cellPos, bool checkObjects = true)
        {
            int extraCells = 0;
            if (self != null)
                extraCells = self.ExtraCells;

            for (int dx = -extraCells; dx <= extraCells; dx++)
            {
                for (int dy = -extraCells; dy <= extraCells; dy++)
                {
                    Vector2Int checkPos = new Vector2Int(cellPos.x + dx, cellPos.y + dy);
                    if (CanGo_Internal(self, checkPos, checkObjects) == false) // CellPos
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        bool CanGo_Internal(BaseObject self, Vector2Int cellPos, bool checkObjects = true)
        {
            if (cellPos.x < MinX || cellPos.x > MaxX)
                return false;
            if (cellPos.y < MinY || cellPos.y > MaxY)
                return false;

            int x = cellPos.x - MinX;
            int y = MaxY - cellPos.y;

            // 충돌 영역이면 못간다
            if (_mapCollision[x, y])
                return false;

            if (checkObjects)
            {
                // 다른 오브젝트가 있다
                if (_objCollision[x, y] != null && _objCollision[x, y] != self)
                    return false;
            }

            return true;
        }

        public BaseObject FindObjectAt(Vector2Int cellPos)
        {
            if (cellPos.x < MinX || cellPos.x > MaxX)
                return null;
            if (cellPos.y < MinY || cellPos.y > MaxY)
                return null;

            int x = cellPos.x - MinX;
            int y = MaxY - cellPos.y;

            return _objCollision[x, y];
        }

        public bool ApplyLeave(BaseObject obj)
        {
            if (obj.Room == null)
                return false;
            if (obj.Room.Map != this)
                return false;

            PositionInfo posInfo = obj.PosInfo;
            if (posInfo.PosX < MinX || posInfo.PosX > MaxX)
                return false;
            if (posInfo.PosY < MinY || posInfo.PosY > MaxY)
                return false;

            // Zone
            Zone zone = obj.Room.GetZone(obj.CellPos);
            zone.Remove(obj);

            EGameObjectType type = obj.ObjectType;

            {
                int x = posInfo.PosX - MinX;
                int y = MaxY - posInfo.PosY;

                int extraCells = obj.ExtraCells;
                for (int dx = -extraCells; dx <= extraCells; dx++)
                {
                    for (int dy = -extraCells; dy <= extraCells; dy++)
                    {
                        if (_objCollision[x + dx, y + dy] == obj)
                            _objCollision[x + dx, y + dy] = null;
                    }
                }
            }

            return true;
        }

        public bool ApplyMove(BaseObject obj, Vector2Int dest, bool checkObjects = true, bool collision = true)
        {
            if (obj == null)
                return false;
            if (obj.Room == null)
                return false;
            if (obj.Room.Map != this)
                return false;

            int extraCells = obj.ExtraCells;

            if (CanGo(obj, dest, checkObjects) == false)
                return false;

            if (collision)
            {
                // 기존 좌표 제거
                {
                    int x = obj.PosInfo.PosX - MinX;
                    int y = MaxY - obj.PosInfo.PosY;

                    for (int dx = -extraCells; dx <= extraCells; dx++)
                    {
                        for (int dy = -extraCells; dy <= extraCells; dy++)
                        {
                            if (_objCollision[x + dx, y + dy] == obj)
                                _objCollision[x + dx, y + dy] = null;
                        }
                    }
                }
                // 새로운 좌표 추가
                {
                    int x = dest.x - MinX;
                    int y = MaxY - dest.y;

                    for (int dx = -extraCells; dx <= extraCells; dx++)
                    {
                        for (int dy = -extraCells; dy <= extraCells; dy++)
                        {
                            _objCollision[x + dx, y + dy] = obj;
                        }
                    }
                }
            }

            UpdateZone(obj, dest);

            // 실제 셀 좌표 이동
            Vector2Int dir = dest - obj.CellPos;
            obj.PosInfo.MoveDir = GetMoveDirection(dir);
            obj.PosInfo.PosX = dest.x;
            obj.PosInfo.PosY = dest.y;

            return true;
        }

        public void UpdateZone(BaseObject obj, Vector2Int dest)
        {
            // Zone
            EGameObjectType type = ObjectManager.GetObjectTypeFromId(obj.ObjectId);
            if (type == EGameObjectType.Hero)
            {
                Hero hero = (Hero)obj;
                Zone now = obj.Room.GetZone(obj.CellPos);
                Zone after = obj.Room.GetZone(dest);
                if (now != after)
                {
                    now.Heroes.Remove(hero);
                    after.Heroes.Add(hero);
                }
            }
            else if (type == EGameObjectType.Monster)
            {
                Monster monster = (Monster)obj;
                Zone now = obj.Room.GetZone(obj.CellPos);
                Zone after = obj.Room.GetZone(dest);
                if (now != after)
                {
                    now.Monsters.Remove(monster);
                    after.Monsters.Add(monster);
                }
            }
        }

        public void LoadMap(string mapName)
        {
            string collisionName = $"{mapName}Collision";

            // Collision 데이터가 들어있는 Map 파일
            string text = File.ReadAllText($"{ConfigManager.Config.dataPath}/MapData/{collisionName}.txt");
            StringReader reader = new StringReader(text);

            MinX = int.Parse(reader.ReadLine());
            MaxX = int.Parse(reader.ReadLine());
            MinY = int.Parse(reader.ReadLine());
            MaxY = int.Parse(reader.ReadLine());

            int xCount = MaxX - MinX + 1;
            int yCount = MaxY - MinY + 1;
            _mapCollision = new bool[xCount, yCount];
            _objCollision = new BaseObject[xCount, yCount];

            for (int y = 0; y < yCount; y++)
            {
                string line = reader.ReadLine();
                for (int x = 0; x < xCount; x++)
                {
                    switch (line[x])
                    {
                        case Define.MAP_TOOL_WALL:
                            _mapCollision[x, y] = true;
                            break;
                        default:
                            _mapCollision[x, y] = false;
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// 월드 좌표를 셀 좌표로 변환
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public Vector2Int WorldToCell(float x, float y)
        {
            return WorldToCell(x, y);
        }

        public Vector2Int WorldToCell(Vector2 worldPos)
        {
            // 아이소메트릭 변환 수식
            int cellX = (int)Math.Floor(worldPos.X / Define.TILE_WIDTH + worldPos.Y / Define.TILE_HEIGHT);
            int cellY = (int)Math.Floor(worldPos.Y / Define.TILE_HEIGHT - worldPos.X / Define.TILE_WIDTH);
            
            return new Vector2Int(cellX, cellY);
        }

        /// <summary>
        /// 셀 좌표를 월드 좌표로 변환
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public Vector2 CellToWorld(int x, int y)
        {
            return CellToWorld(new Vector2Int(x, y));
        }

        public Vector2 CellToWorld(Vector2Int cellPos)
        {
            float worldX = (cellPos.x - cellPos.y) * Define.TILE_WIDTH * 0.5f;
            float worldY = (cellPos.x + cellPos.y) * Define.TILE_HEIGHT * 0.5f;

            return new Vector2(worldX, worldY);
        }

        public EMoveDir GetMoveDirection(Vector2Int normalizedDir)
        {
            if (normalizedDir == Vector2Int.zero)
                return EMoveDir.None;

            for (int i = 0; i < _delta.Count; i++)
            {
                if (_delta[i] == normalizedDir)
                    return (EMoveDir)i + 1;
            }

            return EMoveDir.None;
        }

        #region A* PathFinding
        public struct PQNode : IComparable<PQNode>
        {
            public int H; // Heuristic
            public Vector2Int CellPos;
            public int Depth;

            public int CompareTo(PQNode other)
            {
                if (H == other.H)
                    return 0;
                return H < other.H ? 1 : -1;
            }
        }

        public List<Vector2Int> FindPath(BaseObject self, Vector2Int startCellPos, Vector2Int destCellPos, bool checkObjects = true, int maxDepth = 10)
        {
            // best
            Dictionary<Vector2Int, int> best = new Dictionary<Vector2Int, int>();
            // 경로 추적 용도.
            Dictionary<Vector2Int, Vector2Int> parent = new Dictionary<Vector2Int, Vector2Int>();
            
            PriorityQueue<PQNode> pq = new PriorityQueue<PQNode>(); // OpenList

            Vector2Int pos = startCellPos;
            Vector2Int dest = destCellPos;

            Vector2Int closestCellPos = startCellPos;
            int closestH = (dest - pos).sqrMagnitude;

            // 시작점 발견
            {
                int h = (dest - pos).sqrMagnitude;
                pq.Push(new PQNode() { H = h, CellPos = pos, Depth = 1 });
                parent[pos] = pos;
                best[pos] = h;
            }

            while (pq.Count > 0)
            {
                PQNode node = pq.Pop();
                pos = node.CellPos;

                if (pos == dest)
                    break;
                
                if (node.Depth >= maxDepth)
                    break;

                // 이동할 수 있는 좌표인지 확인해서 처리.
                foreach (Vector2Int delta in _delta)
                {
                    Vector2Int next = pos + delta;

                    if (CanGo(self, next, checkObjects) == false)
                        continue;

                    int h = (dest - next).sqrMagnitude;

                    if (best.ContainsKey(next) == false)
                        best[next] = int.MaxValue;

                    if (best[next] <= h)
                        continue;

                    best[next] = h;

                    pq.Push(new PQNode() { H = h, CellPos = next, Depth = node.Depth + 1 });
                    parent[next] = pos;

                    // 목적지까지는 못 가더라도, best 후보 기억.
                    if (closestH > h)
                    {
                        closestH = h;
                        closestCellPos = next;
                    }
                }
            }

            // 제일 가까운 지점이라도 찾음.
            if (parent.ContainsKey(dest) == false)
                return CalcCellPathFromParent(parent, closestCellPos);

            return CalcCellPathFromParent(parent, dest);
        }

        List<Vector2Int> CalcCellPathFromParent(Dictionary<Vector2Int, Vector2Int> parent, Vector2Int dest)
        {
            List<Vector2Int> cells = new List<Vector2Int>();

            if (parent.ContainsKey(dest) == false)
                return cells;

            Vector2Int now = dest;

            while (parent[now] != now)
            {
                cells.Add(now);
                now = parent[now];
            }

            cells.Add(now);
            cells.Reverse();

            return cells;
        }
        #endregion
    }
}
