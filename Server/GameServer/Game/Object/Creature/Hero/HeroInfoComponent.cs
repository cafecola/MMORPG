using Google.Protobuf.Protocol;
using Server.Data;

namespace GameServer.Game
{
	public class HeroInfoComponent
	{
		// 남한테 보낼 때 사용하는 정보
		public HeroInfo HeroInfo { get; set; } = new HeroInfo();
		// 스스로한테 보낼 때 사용하는 정보
		public MyHeroInfo MyHeroInfo { get; set; } = new MyHeroInfo();

		public Hero Owner { get; private set; }

		public HeroInfoComponent(Hero owner)
		{
			Owner = owner;
		}

		public void Init(HeroDb heroDb)
		{
			// HeroInfo
			HeroInfo.Level = heroDb.Level;
			HeroInfo.Name = heroDb.Name;
			HeroInfo.Gender = heroDb.Gender;
			HeroInfo.ClassType = heroDb.ClassType;

			MyHeroInfo.MapId = heroDb.MapId;
			MyHeroInfo.HeroInfo = HeroInfo;
			MyHeroInfo.Exp = heroDb.Exp;

			MyHeroInfo.CurrencyInfo = new CurrencyInfo()
			{
				Gold = heroDb.Gold,
				Dia = heroDb.Dia,
			};
		}

		#region Level System

		public void AddExp(int amount)
		{
			if (IsMaxLevel())
				return;

			Exp += amount;

			bool levelUp = ReCalculateLevel();
			if (levelUp)
				Owner.RefreshStat();
		}

		private bool ReCalculateLevel()
		{
			bool levelUp = false;

			while (true)
			{
				if (IsMaxLevel())
					break;

				if (Exp < GetExpToNextLevel(Level))
					break;

				Exp = Math.Max(0, Exp - GetExpToNextLevel(Level));
				Level++;
				levelUp = true;
			}

			return levelUp;
		}

		public bool CanLevelUp()
		{
			return Exp >= GetExpToNextLevel(Level);
		}

		public int GetExpToNextLevel(int level)
		{
			if (DataManager.BaseStatDict.TryGetValue(level, out BaseStatData data))
				return data.Exp;

			return 100;
		}

		public bool IsMaxLevel()
		{
			return Level == DataManager.BaseStatDict.Count;
		}

		#endregion

		#region Helpers
		public int Level
		{
			get { return MyHeroInfo.HeroInfo.Level; }
			set { MyHeroInfo.HeroInfo.Level = value; }
		}

		public int Gold
		{
			get { return MyHeroInfo.CurrencyInfo.Gold; }
			set { MyHeroInfo.CurrencyInfo.Gold = value; }
		}

		public int Dia
		{
			get { return MyHeroInfo.CurrencyInfo.Dia; }
			set { MyHeroInfo.CurrencyInfo.Dia = value; }
		}

		public int Exp
		{
			get { return MyHeroInfo.Exp; }
			set { MyHeroInfo.Exp = value; }
		}

		public string Name
		{
			get { return HeroInfo.Name; }
			set { HeroInfo.Name = value; }
		}

		public int MapId
		{
			get { return MyHeroInfo.MapId; }
			set { MyHeroInfo.MapId = value; }
		}
		#endregion
	}
}
