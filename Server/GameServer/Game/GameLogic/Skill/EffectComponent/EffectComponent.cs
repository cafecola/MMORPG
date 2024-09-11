using Google.Protobuf.Protocol;
using Server.Data;

namespace GameServer.Game
{
    public class EffectComponent
    {
		private static int _effectIdGenerator = 1;

		public static int GenerateEffectId()
		{
            return Interlocked.Increment(ref _effectIdGenerator);
		}

		public static readonly Dictionary<EEffectType, IEffectPolicy> _policies = new()
        {
            { EEffectType.Damage, new DamageEffectPolicy() },
            { EEffectType.Heal, new HealEffectPolicy() },
            { EEffectType.BuffStat, new BuffStatEffectPolicy() },
            { EEffectType.BuffLifeSteal, new DummyPolicy() },
            { EEffectType.BuffStun, new BuffStunPolicy() },
		};

		Dictionary<int/*buffId*/, Effect> _effects = [];
		public Creature Owner { get; private set; }

        public EffectComponent(Creature owner)
        {
            Owner = owner;
        }

        public static Effect SpawnEffect(int effectId, int templateId, Creature owner, Creature caster, IEffectPolicy policy)
        {
            Effect effect = new Effect(effectId, templateId, owner, caster, policy);
            return effect;
        }

        public void ApplyEffect(int templateId, Creature caster)
        {
            if (DataManager.EffectDict.TryGetValue(templateId, out EffectData effectData) == false)
                return;

            ApplyEffect(effectData, caster);
        }

        public void ApplyEffect(EffectData effectData, Creature caster, bool send = true)
        {
            if (effectData == null)
                return;

            switch (effectData.DurationPolicy)
            {
                case EDurationPolicy.Instant:
                    ApplyInstantEffect(effectData, caster);
                    break;
                case EDurationPolicy.Duration:
                    ApplyDurationEffect(effectData, caster, send);
                    break;
                case EDurationPolicy.Infinite:
                    ApplyInfiniteEffect(effectData, caster, send);
                    break;
            }
        }

        void ApplyInstantEffect(EffectData effectData, Creature caster)
        {
            if (effectData == null)
                return;

            if (_policies.TryGetValue(effectData.EffectType, out IEffectPolicy policy) == false)
                return;

			// 이펙트 적용.
			policy.Apply(Owner, caster, effectData);
		}

        void ApplyDurationEffect(EffectData effectData, Creature caster, bool send)
        {
			if (effectData == null)
				return;
            if (effectData.Duration == 0)
                return;
		    if (_policies.TryGetValue(effectData.EffectType, out IEffectPolicy policy) == false)
				return;
            
            // 이펙트 생성.
			int effectId = GenerateEffectId();
            Effect effect = SpawnEffect(effectId, effectData.TemplateId, Owner, caster, policy);
			_effects.Add(effectId, effect);

            // 이펙트 적용.
			effect.Apply();

            // 모두에게 알림.
            if (send)
                SendApply(effect);

			// 이펙트 소멸 예약.
			Owner.Room?.PushAfter((int)(effectData.Duration * 1000), () => { RemoveEffect(effect); });
		}

		void ApplyInfiniteEffect(EffectData effectData, Creature caster, bool send)
        {
			if (effectData == null)
				return;

			if (_policies.TryGetValue(effectData.EffectType, out IEffectPolicy policy) == false)
				return;

			// 이펙트 생성.
			int effectId = GenerateEffectId();
			Effect effect = SpawnEffect(effectId, effectData.TemplateId, Owner, caster, policy);
			_effects.Add(effectId, effect);

			// 이펙트 적용.
			effect.Apply();

			// 모두에게 알림.
			if (send)
                SendApply(effect);
        }

		/// <summary>
		/// 아이템마다 고유한 이펙트를 가지고 있기때문에 templateId로 검색.
		/// </summary>
		/// <param name="templateId"></param>
		/// <param name="send"></param>
        public void RemoveItemEffect(int templateId, bool send = true)
        {
            Effect effect = _effects.Values.FirstOrDefault(e => e.EffectData.TemplateId == templateId);
            if (effect != null)
                RemoveEffect(effect, send);
        }

        public void RemoveEffect(int effectId, bool send = true)
		{
            if (_effects.TryGetValue(effectId, out Effect effect))
                RemoveEffect(effect, send);
		}

		public void RemoveEffect(Effect effect, bool send = true)
        {
			_effects.Remove(effect.EffectId);
            
            effect.Revert();					

			if (send)
                SendRemove(effect);
        }

        public void Clear()
        {
            foreach (Effect effect in _effects.Values.ToList())
                RemoveEffect(effect, false);

            _effects.Clear();
        }

        private void SendApply(Effect effect)
        {
            S_ApplyEffect packet = new S_ApplyEffect();
            packet.ObjectId = Owner.ObjectId;
            packet.EffectTemplateId = effect.EffectData.TemplateId;
            packet.EffectId = effect.EffectId;
            packet.RemainingTicks = effect.GetRemainingLifetimeInTicks();
            packet.StateFlag = Owner.CreatureInfo.StateFlag;

            Owner.Room?.Broadcast(Owner.CellPos, packet);

            if (Owner.ObjectType == EGameObjectType.Hero)
            {
                (Owner as Hero).SendRefreshStat();
            }
        }

        private void SendRemove(Effect effect)
        {
            S_RemoveEffect packet = new S_RemoveEffect();
            packet.ObjectId = Owner.ObjectId;
            packet.EffectId = effect.EffectId;
            packet.StateFlag = Owner.CreatureInfo.StateFlag;

            Owner.Room?.Broadcast(Owner.CellPos, packet);

            if (Owner.ObjectType == EGameObjectType.Hero)
            {
                (Owner as Hero).SendRefreshStat();
            }
        }
    }
}
