using Google.Protobuf.Protocol;
using Server.Data;

namespace GameServer.Game
{
	public abstract class Skill
	{
		public Creature Owner { get; protected set; }
        public int TemplateId { get; protected set; }

        public SkillData SkillData { get; protected set; }

		// 쿨타임 관리용도
		public long NextUseTick { get; protected set; } = 0;

        public Skill(int templateId, Creature owner)
        {
            TemplateId = templateId;
            Owner = owner;

            DataManager.SkillDict.TryGetValue(TemplateId, out SkillData skillData);
			SkillData = skillData;
        }

		public abstract bool CanUseSkill(int targetId);
		public abstract void UseSkill(int targetId);

		public int GetSkillRange(int targetId)
		{
			Creature target = Owner.Room.GetCreatureById(targetId);
			if (target == null)
				return 0;

			return SkillData.SkillRange + target.ExtraCells + Owner.ExtraCells;

        }

        #region 쿨타임 관리
        public long GetRemainingCooltimeInTicks()
		{
			return Math.Max(0, (NextUseTick - Utils.TickCount));
		}

		public float GetRemainingCooltimeInSeconds()
		{
			return GetRemainingCooltimeInTicks() / 1000.0f;
		}

		public void UpdateCooltime()
		{
			NextUseTick = Utils.TickCount + (long)(1000 * SkillData.Cooltime);
		}
		#endregion

		#region 스킬 사용
		public bool CheckCooltimeAndState()
		{
			if (CheckCooltime() == false)
				return false;
			if (Owner.Room == null)
				return false;
			if (Owner.State == EObjectState.Dead)
				return false;
			if (Owner.IsStunned)
				return false;

			return true;
		}

		public bool CheckCooltime()
		{
			return GetRemainingCooltimeInTicks() == 0;
		}

		public bool CheckTargetAndRange(int targetId)
		{
			Creature target = GetUseSkillTarget(Owner, SkillData, targetId);
            if (target == null)
				return false;
			int dist = Owner.GetDistance(target);
			if (dist > GetSkillRange(targetId))
				return false;

			return true;
		}

		public static bool IsValidUseSkillTargetType(Creature owner, Creature target, EUseSkillTargetType targetType)
		{
			switch (targetType)
			{
				case EUseSkillTargetType.Self:
					return owner == target;
				case EUseSkillTargetType.Other:
					return owner != target;
			}

			return true;
		}

		public static bool IsValidTargetFriendType(Creature owner, Creature target, ETargetFriendType targetType)
		{
			switch (targetType)
			{
				case ETargetFriendType.Friend:
					return owner.IsFriend(target);
				case ETargetFriendType.Enemy:
					return owner.IsEnemy(target);
			}

			return true;
		}

		public static Creature GetUseSkillTarget(Creature owner, SkillData skillData, int targetId)
		{
			if (owner.Room == null)
				return null;

            if (skillData.UseSkillTargetType == EUseSkillTargetType.Self)
            {
                //SelfCenter 범위기는 target검사 X
                if (skillData.IsSingleTarget == false)
                    return owner;
            }

            Creature target = owner.Room.GetCreatureById(targetId);

			if (IsValidUseSkillTargetType(owner, target, skillData.UseSkillTargetType) == false)
				return null;

			if (IsValidTargetFriendType(owner, target, skillData.TargetFriendType) == false)
				return null;

			return target;
		}

		public static List<Creature> GatherSkillEffectTargets(Creature owner, SkillData skillData, Creature target)
		{
			List<Creature> targets = new List<Creature>();

			if (owner.Room == null)
				return targets;


            if (skillData.IsSingleTarget)
			{
				if (IsValidTargetFriendType(owner, target, skillData.TargetFriendType))
					targets.Add(target);
			}
			else
			{
                bool isSelfTarget = skillData.UseSkillTargetType == EUseSkillTargetType.Self;
                Vector2Int pivot = isSelfTarget ? owner.CellPos : target.CellPos;

                targets = owner.Room.FindAdjacentCreatures(pivot, (c) =>
				{
					if (IsValidTargetFriendType(owner, c, skillData.TargetFriendType) == false)
						return false;

                    int dist = isSelfTarget ? owner.GetDistance(c) : target.GetDistance(c);

                    if (dist > skillData.GatherTargetRange)
						return false;

					return true;
				});
			}

			return targets;
		}

		protected static void AddEffect(Creature target, Creature caster, EffectData effectData)
		{
			target.EffectComp.ApplyEffect(effectData.TemplateId, caster);
		}

		protected void BroadcastSkill(Creature target)
		{
			if (Owner.Room == null)
				return;

			// TODO: 스킬 사용 Broadcast (꼭 상태 변화가 필요할까?)
			Owner.ObjectInfo.PosInfo.State = EObjectState.Skill;

			S_Skill skillPacket = new S_Skill()
			{
				ObjectId = Owner.ObjectInfo.ObjectId,
				TemplateId = SkillData.TemplateId,
				TargetId = target.ObjectId,
			};

			Owner.Room.Broadcast(Owner.CellPos, skillPacket);
		}
		#endregion
	}
}
