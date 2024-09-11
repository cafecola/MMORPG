using Microsoft.EntityFrameworkCore;
using Google.Protobuf.Protocol;

namespace GameServer
{
	public partial class DBManager : JobSerializer
	{
		#region Network 쓰레드에서 직접 호출
		public static List<HeroDb> LoadHeroDb(long accountDbId)
		{
			using (GameDbContext db = new())
			{
                List<HeroDb> heroDbs = db.Heroes
                    .Where(h => h.AccountDbId == accountDbId)
                    .Include(h => h.Items)
                    .ToList();

				return heroDbs;
			}
		}

		public static HeroDb CreateHeroDb(long accountDbId, C_CreateHeroReq reqPacket)
		{
			using (GameDbContext db = new())
			{
				HeroDb heroDb = db.Heroes.Where(h => h.Name == reqPacket.Name).FirstOrDefault();
				if (heroDb != null)
					return null;

				heroDb = new HeroDb()
				{
					AccountDbId = accountDbId,
					Name = reqPacket.Name,
					Gender = reqPacket.Gender,
					ClassType = reqPacket.ClassType,
					Level = 1,
					MapId = 1,
					Hp = -1,
					Mp = -1,
				};

				db.Heroes.Add(heroDb);

				if (db.SaveChangesEx())
					return heroDb;

				return null;
			}
		}

		public static bool DeleteHeroDb(int heroDbId)
		{
			using (GameDbContext db = new())
			{
				HeroDb heroDb = db.Heroes.Where(h => h.HeroDbId == heroDbId).FirstOrDefault();
				if (heroDb == null)
					return false;

				db.Heroes.Remove(heroDb);

				if (db.SaveChangesEx())
					return true;
			}

			return true;
		}
		#endregion
	}
}
