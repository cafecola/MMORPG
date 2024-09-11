using Google.Protobuf.Protocol;

namespace GameServer
{
	public partial class GameRoom : JobSerializer
    {
        public void HandleMove(Hero hero, C_Move movePacket)
        {
            if (hero == null)
                return;
            if (hero.State == EObjectState.Dead)
                return;
            if (hero.IsStunned)
                return;

            PositionInfo movePosInfo = movePacket.PosInfo;
            ObjectInfo info = hero.ObjectInfo;

            // TODO: 거리 검증등

            if (Map.CanGo(hero, new Vector2Int(movePosInfo.PosX, movePosInfo.PosY)) == false)
                return;

            info.PosInfo.State = movePosInfo.State;
            info.PosInfo.MoveDir = movePosInfo.MoveDir;
            Map.ApplyMove(hero, new Vector2Int(movePosInfo.PosX, movePosInfo.PosY));

            hero.BroadcastMove();
        }

        public void UseSkill(Creature owner, int templateId, int targetId)
        {
            if (owner == null)
                return;

            owner.SkillComp.UseSkill(templateId, targetId);
        }

        public void OnDead(BaseObject gameObject, BaseObject attacker)
        {
            if (gameObject.ObjectType == EGameObjectType.Projectile)
                return;
            if (gameObject.State == EObjectState.Dead)
                return;

            gameObject.State = EObjectState.Dead;

            if (gameObject.ObjectType == EGameObjectType.Hero)
            {
                // TODO : 마을에서 리스폰

                Hero hero = gameObject as Hero;
                hero.Reset();
                hero.ReserveRebirth();

                LeaveGame(gameObject.ObjectId, ELeaveType.Dead);

                return;
            }
            else if (gameObject.ObjectType == EGameObjectType.Monster)
            {
                gameObject.Room.SpawningPool.Respawn(gameObject);
            }
        }
    }
}
