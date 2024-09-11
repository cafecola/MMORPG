
namespace GameServer
{
    // Unity 맵의 Cell 좌표
    public struct Vector2Int
    {
        public int x;
        public int y;

        public Vector2Int(int x, int y) { this.x = x; this.y = y; }

        public static Vector2Int up { get { return new Vector2Int(1, 1); } }
        public static Vector2Int down { get { return new Vector2Int(-1, -1); } }
        public static Vector2Int left { get { return new Vector2Int(-1, 1); } }
        public static Vector2Int right { get { return new Vector2Int(1, -1); } }

        public static Vector2Int upLeft { get { return new Vector2Int(0, 1); } }
        public static Vector2Int upRight { get { return new Vector2Int(1, 0); } }
        public static Vector2Int downLeft { get { return new Vector2Int(-1, 0); } }
        public static Vector2Int downRight { get { return new Vector2Int(0, -1); } }

        public static Vector2Int zero { get { return new Vector2Int(0, 0); } }

        public static Vector2Int operator *(Vector2Int v, int c)
        {
            return new Vector2Int(v.x * c, v.y * c);
        }

        public static Vector2Int operator +(Vector2Int a, Vector2Int b)
        {
            return new Vector2Int(a.x + b.x, a.y + b.y);
        }

        public static Vector2Int operator -(Vector2Int a, Vector2Int b)
        {
            return new Vector2Int(a.x - b.x, a.y - b.y);
        }

        public static bool operator ==(Vector2Int lhs, Vector2Int rhs)
        {
            return lhs.x == rhs.x && lhs.y == rhs.y;
        }

        public static bool operator !=(Vector2Int lhs, Vector2Int rhs)
        {
            return !(lhs == rhs);
        }

        public override bool Equals(object obj)
        {
            return (Vector2Int)obj == this;
        }

        public override int GetHashCode()
        {
            long value = (y << 32) | x;
            return value.GetHashCode();
        }

        public float magnitude { get { return (float)Math.Sqrt(sqrMagnitude); } }
        public int sqrMagnitude { get { return (x * x + y * y); } }
        public int cellDistFromZero { get { return Math.Abs(x) + Math.Abs(y); } }
    }
}
