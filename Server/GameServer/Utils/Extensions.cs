using Google.Protobuf.Protocol;

namespace GameServer
{
	public static class Extensions
	{
		public static bool SaveChangesEx(this GameDbContext db)
		{
			try
			{
				db.SaveChanges();
				return true;
			}
			catch
			{
				return false;
			}
		}

        public static bool IsValid(this BaseObject bc)
        {
            if (bc == null)
                return false;

			if (bc.Room == null)
				return false;
			
            switch (bc.ObjectType)
            {
                case EGameObjectType.Monster:
                case EGameObjectType.Hero:
                    return ((Creature)bc).State != EObjectState.Dead;
            }
            return true;
        }

        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                Random _rand = new Random();
                int k = _rand.Next(0, n + 1);
                (list[k], list[n]) = (list[n], list[k]);//swap
            }
        }
    }
}
