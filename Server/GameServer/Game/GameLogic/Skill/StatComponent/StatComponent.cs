using Google.Protobuf.Protocol;
using Server.Data;

namespace GameServer.Game
{
	public class StatComponent
	{
		public StatInfo BaseStat { get; protected set; } = new StatInfo();
		public StatInfo TotalStat { get; protected set; } = new StatInfo();

		public Creature Owner { get; private set; }

		public StatComponent(Creature owner)
		{
			Owner = owner;
		}

		public void InitStat(int level, bool maxHp = true)
		{
			// 레벨별 baseStat 계산
			if (DataManager.BaseStatDict.TryGetValue(level, out BaseStatData baseStatData))
			{
				BaseStat.Attack = baseStatData.Attack;
				BaseStat.MaxHp = baseStatData.MaxHp;
				BaseStat.MaxMp = baseStatData.MaxMp;
				BaseStat.Hp = baseStatData.MaxHp;
				BaseStat.Mp = baseStatData.MaxMp;
				BaseStat.HpRegen = baseStatData.HpRegen;
				BaseStat.MpRegen = baseStatData.MpRegen;
				BaseStat.Defence = baseStatData.Def;
				BaseStat.Dodge = baseStatData.Dodge;
				BaseStat.AttackSpeed = baseStatData.AtkSpeed;
				BaseStat.MoveSpeed = baseStatData.MoveSpeed;
				BaseStat.CriRate = baseStatData.CriRate;
				BaseStat.CriDamage = baseStatData.CriDamage;
				BaseStat.Str = baseStatData.Str;
				BaseStat.Dex = baseStatData.Dex;
				BaseStat.Int = baseStatData.Int;
				BaseStat.Con = baseStatData.Con;
				BaseStat.Wis = baseStatData.Wis;
			}

			TotalStat.MergeFrom(BaseStat);

			if (maxHp)
			{
				SetTotalStat(EStatType.Hp, TotalStat.MaxHp);
				SetTotalStat(EStatType.Mp, TotalStat.MaxMp);
			}
		}

		public static readonly Dictionary<EStatType, Func<StatInfo, float>> StatGetters = new()
		{
			{ EStatType.MaxHp, (s) => s.MaxHp },
			{ EStatType.Hp, (s) => s.Hp },
			{ EStatType.HpRegen, (s) => s.HpRegen },
			{ EStatType.MaxMp, (s) => s.MaxMp },
			{ EStatType.Mp, (s) => s.Mp },
			{ EStatType.MpRegen, (s) => s.MpRegen },
			{ EStatType.Attack, (s) => s.Attack },
			{ EStatType.Defence, (s) => s.Defence },
			{ EStatType.Dodge, (s) => s.Dodge },
			{ EStatType.AttackSpeed, (s) => s.AttackSpeed },
			{ EStatType.MoveSpeed, (s) => s.MoveSpeed },
			{ EStatType.CriRate, (s) => s.CriRate },
			{ EStatType.CriDamage, (s) => s.CriDamage },
			{ EStatType.Str, (s) => s.Str },
			{ EStatType.Dex, (s) => s.Dex },
			{ EStatType.Int, (s) => s.Int },
			{ EStatType.Con, (s) => s.Con },
			{ EStatType.Wis, (s) => s.Wis }
		};
		public static readonly Dictionary<EStatType, Action<StatInfo, float>> StatSetters = new ()
		{
			{ EStatType.MaxHp, (s, v) => s.MaxHp = v },
			{ EStatType.Hp, (s, v) => s.Hp = v },
			{ EStatType.HpRegen, (s, v) => s.HpRegen= v },
			{ EStatType.MaxMp, (s, v) => s.MaxMp= v },
			{ EStatType.Mp, (s, v) => s.Mp = v },
			{ EStatType.MpRegen, (s, v) => s.MpRegen= v },
			{ EStatType.Attack, (s, v) => s.Attack = v },
			{ EStatType.Defence, (s, v) => s.Defence = v },
			{ EStatType.Dodge, (s, v) => s.Dodge = v },
			{ EStatType.AttackSpeed, (s, v) => s.AttackSpeed = v },
			{ EStatType.MoveSpeed, (s, v) => s.MoveSpeed = v },
			{ EStatType.CriRate, (s, v) => s.CriRate= v },
			{ EStatType.CriDamage, (s, v) => s.CriDamage= v },
			{ EStatType.Str, (s, v) => s.Str = (int)v },
			{ EStatType.Dex, (s, v) => s.Dex = (int)v },
			{ EStatType.Int, (s, v) => s.Int = (int)v },
			{ EStatType.Con, (s, v) => s.Con = (int)v },
			{ EStatType.Wis, (s, v) => s.Wis = (int)v }
		};

		public float GetBaseStat(EStatType statType) { return StatGetters[statType](BaseStat); }
		public float GetTotalStat(EStatType statType) { return StatGetters[statType](TotalStat); }
		public void SetBaseStat(EStatType statType, float value) { StatSetters[statType](BaseStat, value); }

		public void AddTotalStat(EStatType statType, float value)
		{
			float finalValue = GetTotalStat(statType) + value;
			SetTotalStat(statType, finalValue);
		}

		public void SetTotalStat(EStatType statType, float value)
		{
			switch (statType)
			{
				case EStatType.Hp:
					value = Math.Min(value, GetTotalStat(EStatType.MaxHp));
					break;
				case EStatType.Mp:
					value = Math.Min(value, GetTotalStat(EStatType.MaxMp));
					break;
			}

			StatSetters[statType](TotalStat, value);
		}

		#region Regeneration
		protected IJob _hpRegenJob;
		protected IJob _mpRegenJob;

		public void TickRegenStatJob()
		{
			CancelJobs();

			if (Owner.Room != null)
			{
				_hpRegenJob = Owner.Room.PushAfter(8000, RegenHp);
				_mpRegenJob = Owner.Room.PushAfter(5000, RegenMp);
			}
		}

		protected void RegenHp()
		{
			if (Owner.State != EObjectState.Dead && Owner.StatComp.Hp != Owner.StatComp.GetTotalStat(EStatType.MaxHp))
			{
				float regen = Owner.StatComp.GetTotalStat(EStatType.HpRegen);
				Owner.AddStat(EStatType.Hp, regen, EFontType.Heal);
			}

			_hpRegenJob = Owner.Room?.PushAfter(8000, RegenHp);
		}

		protected void RegenMp()
		{
			if (Owner.State != EObjectState.Dead && Owner.StatComp.Mp != Owner.StatComp.GetTotalStat(EStatType.MaxMp))
			{
				float regen = Owner.StatComp.GetTotalStat(EStatType.MpRegen);
				Owner.AddStat(EStatType.Mp, regen, EFontType.Heal);
			}

			_mpRegenJob = Owner.Room?.PushAfter(5000, RegenMp);
		}

		public void CancelJobs()
		{
			if (_hpRegenJob != null)
			{
				_hpRegenJob.Cancel = true;
				_hpRegenJob = null;
			}

			if (_mpRegenJob != null)
			{
				_mpRegenJob.Cancel = true;
				_mpRegenJob = null;
			}


		}
		#endregion

		#region Helpers
		public float Hp
		{
			get { return TotalStat.Hp; }
			set { SetTotalStat(EStatType.Hp, Math.Clamp(value, 0, TotalStat.MaxHp)); }
		}

		public float Mp
		{
			get { return TotalStat.Mp; }
			set { SetTotalStat(EStatType.Mp, Math.Clamp(value, 0, TotalStat.MaxMp)); }
		}

		public float Attack
		{
			get { return TotalStat.Attack; }
			set { SetTotalStat(EStatType.Attack, value); }
		}

		public float Defence
		{
			get { return TotalStat.Defence; }
			set { SetTotalStat(EStatType.Defence, value); }
		}

		public float MoveSpeed
		{
			get { return TotalStat.MoveSpeed; }
		}
		#endregion
	}
}
