using Google.Protobuf.Protocol;
using Server.Data;
using Server.Game;

namespace GameServer.Game
{
	public class ProjectileSkill : Skill
	{
        public ProjectileSkill(int templateId, Creature owner) : base(templateId, owner)
        {

        }

		public override bool CanUseSkill(int targetId)
		{
			if (CheckCooltimeAndState() == false)
				return false;

			if (CheckTargetAndRange(targetId) == false)
				return false;
			if (SkillData.ProjectileData == null)
				return false;
			if(Owner.StatComp.Mp < SkillData.Cost)
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
			Creature target = GetUseSkillTarget(Owner, SkillData, targetId);
			if (target == null) 
				return;
            Projectile projectile = ObjectManager.Instance.Spawn<Projectile>(SkillData.ProjectileData.TemplateId);
			if (projectile == null)
				return;

			if (SkillData.Cost > 0)
                Owner.AddStat(EStatType.Mp, -SkillData.Cost, EFontType.Cost); 
			
			projectile.Init(SkillData, target);

			projectile.Owner = Owner;
            projectile.ProjectileInfo.OwnerId = Owner.ObjectId;

            projectile.PosInfo.State = EObjectState.Move;
			projectile.PosInfo.MergeFrom(Owner.PosInfo);

			// 애니메이션 이벤트타임에 맞게 투사체 생성
			Vector2Int spawnPos = new Vector2Int(Owner.PosInfo.PosX, Owner.PosInfo.PosY);
			room.PushAfter((int)(SkillData.DelayTime * 1000), () =>
			{
				room.EnterGame(projectile, spawnPos, false);
			});

			BroadcastSkill(target);
		}
	}
}
