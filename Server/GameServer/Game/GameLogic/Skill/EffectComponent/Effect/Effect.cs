using Google.Protobuf.Protocol;
using Server.Data;

namespace GameServer.Game
{
    public class Effect
    {
        public int EffectId { get; private set; }
		public EffectData EffectData { get; private set; }
		public EEffectType EffectType { get { return EffectData.EffectType; } }
        public Creature Owner { get; private set; }
        public Creature Caster { get; private set; }        
        public IEffectPolicy Policy { get; private set; }
		public long DespawnTick { get; private set; }

		public int GetRemainingLifetimeInTicks()
		{
			return (int)Math.Max(0, (DespawnTick - Utils.TickCount));
		}

		public float GetRemainingLifetimeInSeconds()
		{
			return GetRemainingLifetimeInTicks() / 1000.0f;
		}

		public Effect(int effectId, int templateId, Creature owner, Creature caster, IEffectPolicy policy)
        {
            EffectId = effectId;
            DataManager.EffectDict.TryGetValue(templateId, out EffectData effectData);
            EffectData = effectData;
            Owner = owner;
            Caster = caster;
            Policy = policy;

            if (effectData.DurationPolicy == EDurationPolicy.Duration)
                DespawnTick = Utils.TickCount + (long)(1000 * effectData.Duration);
		}

		public virtual void Update() 
        {
        }

		public virtual void Apply()
        {
            Policy?.Apply(Owner, Caster, EffectData);
		}

        public virtual void Revert()
        {
			Policy?.Revert(Owner, EffectData);
        }
    }
}

