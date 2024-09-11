using Google.Protobuf.Protocol;
using Server.Data;

namespace GameServer.Game
{
	public class NormalSkill : Skill
	{
        public NormalSkill(int templateId, Creature owner) : base(templateId, owner)
        {

        }

		public override bool CanUseSkill(int targetId)
		{
			if (CheckCooltimeAndState() == false)
				return false;
			if (CheckTargetAndRange(targetId) == false)
				return false;
			if(Owner.StatComp.Mp <SkillData.Cost)
				return false;

			return true;
		}

		public override void UseSkill(int targetId)
		{
			if (CanUseSkill(targetId) == false)
				return;
			GameRoom room = Owner.Room;
			if (room == null)
				return;
			if (SkillData.Cost > 0)
				Owner.AddStat(EStatType.Mp, -SkillData.Cost, EFontType.Cost);

            Creature target = GetUseSkillTarget(Owner, SkillData, targetId);

			// 이펙트(효과 및 데미지) 적용
			List<Creature> targets = GatherSkillEffectTargets(Owner, SkillData, target);			
			foreach (Creature t in targets)
			{
				room?.PushAfter((int)(SkillData.DelayTime * 1000), AddEffect, t, Owner, SkillData.EffectData);
			}

			BroadcastSkill(target);
		}
	}
}
