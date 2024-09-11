using Server.Data;
using Google.Protobuf.Protocol;

namespace GameServer.Game
{
    public interface IEffectPolicy
    {
        void Apply(Creature owner, Creature caster, EffectData effectData);
        void Revert(Creature owner, EffectData effectData);
    }

    public class DummyPolicy : IEffectPolicy
    {
		public void Apply(Creature owner, Creature caster, EffectData effectData)
		{
		}

		public void Revert(Creature owner, EffectData effectData)
		{
		}
	}

    public class DamageEffectPolicy : IEffectPolicy
	{
        public void Apply(Creature owner, Creature caster, EffectData effectData)
        {
            if (owner == null)
                return;

			float damage = caster.StatComp.GetTotalStat(EStatType.Attack) * effectData.DamageValue;

			if (effectData.DamageValue > 0)
				owner.OnDamaged(caster, damage);
		}

        public void Revert(Creature owner, EffectData effectData)
        {
        }
    }

	public class BuffStatEffectPolicy : IEffectPolicy
	{
		public void Apply(Creature owner, Creature caster, EffectData effectData)
		{
			if (owner == null)
				return;

			foreach (StatValuePair pair in effectData.StatValues)
			{
                float value = pair.AddValue;
                float prevValue = owner.StatComp.GetTotalStat(pair.StatType);
                float finalValue = prevValue + value;
                owner.StatComp.SetTotalStat(pair.StatType, finalValue);
            }
		}

		public void Revert(Creature owner, EffectData effectData)
		{
			if (owner == null)
				return;

            foreach (StatValuePair pair in effectData.StatValues)
            {
                float value = pair.AddValue;
                float prevValue = owner.StatComp.GetTotalStat(pair.StatType);
                float finalValue = prevValue - value;
                owner.StatComp.SetTotalStat(pair.StatType, finalValue);
            }
		}
	}

	public class BuffStunPolicy : IEffectPolicy
	{
		public void Apply(Creature owner, Creature caster, EffectData effectData)
		{
			if (owner == null)
				return;

			owner.IsStunned = true;
		}

		public void Revert(Creature owner, EffectData effectData)
		{
			if (owner == null)
				return;

			owner.IsStunned = false;
		}
	}

    public class HealEffectPolicy : IEffectPolicy
    {
        public void Apply(Creature owner, Creature caster, EffectData effectData)
        {
			foreach (StatValuePair pair in effectData.StatValues)
			{
				owner.AddStat(pair.StatType, (int)pair.AddValue, EFontType.Heal);
			}
        }

        public void Revert(Creature owner, EffectData effectData)
        {
        }
    }
}
