using GameServer.Game;
using Google.Protobuf.Protocol;
using Server.Data;

namespace GameServer
{
    public class Creature : BaseObject
    {
        public virtual CreatureData Data { get; set; }
        public SkillComponent SkillComp { get; protected set; }
        public EffectComponent EffectComp { get; protected set; }
        public StatComponent StatComp { get ; protected set; }

        public CreatureInfo CreatureInfo { get; private set; } = new CreatureInfo();

        bool GetStateFlag(ECreatureStateFlag type)
        {
            return (CreatureInfo.StateFlag & (1 << (int)type)) != 0;
        }

        public void SetStateFlag(ECreatureStateFlag type, bool value)
        {
            if (value)
            {
                CreatureInfo.StateFlag |= (1 << (int)type);
            }
            else
            {
                CreatureInfo.StateFlag &= ~(1 << (int)type);
            }
        }

        public void ClearStateFlags()
        {
            for (int flag = 0; flag < (int)ECreatureStateFlag.MaxCount; flag++)
            {
                SetStateFlag((ECreatureStateFlag)flag, false);
            }
        }

        public bool IsStunned
        {
            get { return GetStateFlag(ECreatureStateFlag.Stun); }
            set { SetStateFlag(ECreatureStateFlag.Stun, value); }
		}

        public Creature()
        {
            CreatureInfo.ObjectInfo = ObjectInfo;

            SkillComp = new SkillComponent(this);
            EffectComp = new EffectComponent(this);
            StatComp = new StatComponent(this);
		}

        public override void Update()
        {
            base.Update();
        }

        public override bool OnDamaged(BaseObject attacker, float damage)
        {
            if (Room == null)
                return false;

            if (State == EObjectState.Dead)
                return false;

            // 데미지 감소
            float finalDamage = Math.Max(damage - StatComp.Defence, 0);
            AddStat(EStatType.Hp, -finalDamage, EFontType.Hit);

            if (StatComp.Hp <= 0)
            {
                OnDead(attacker);
            }

            return true;
        }

        public override void OnDead(BaseObject attacker)
        {
            base.OnDead(attacker);
        }

        public virtual bool IsEnemy(BaseObject target)
        {
            if (target == null)
                return false;
            if (target == this)
                return false;
            if (Room != target.Room)
                return false;
            
            // TODO: PK 처리
            if (ObjectType == target.ObjectType)
                return false;
            else
                return true;
        }

        public virtual bool IsFriend(BaseObject target)
        {
            return IsEnemy(target) == false;
        }

        public void AddStat(EStatType statType, float diff, EFontType fontType, bool sendPacket = true)
        {
            if (diff == 0)
                return;

            if (State == EObjectState.Dead)
                return;

			StatComp.AddTotalStat(statType, diff);

			if (sendPacket == false)
				return;

			S_ChangeOneStat changePacket = new S_ChangeOneStat();
			changePacket.ObjectId = ObjectId;
			changePacket.StatType = statType;
			changePacket.Value = StatComp.GetTotalStat(statType);
			changePacket.Diff = diff;
			changePacket.FontType = fontType;
            
            // 서버에서는 다 보내고 클라에서 조건부로 처리
            Room?.Broadcast(CellPos, changePacket);
		}

        public virtual void Reset()
        {
			StatComp.Hp = Math.Max(0, StatComp.GetTotalStat(EStatType.MaxHp));
            PosInfo.State = EObjectState.Idle;
            
            ClearStateFlags();
            EffectComp.Clear();
            CancelJobs();
        }

        public virtual void CancelJobs()
        {
            StatComp.CancelJobs();
        }
    }
}
