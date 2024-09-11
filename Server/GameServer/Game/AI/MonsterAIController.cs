using Google.Protobuf.Protocol;
using Server.Data;

namespace GameServer.Game
{
    public class MonsterAIController : FSMController<Monster, Creature>
    {
		protected override Vector2Int GetSpawnPos() { return Owner.SpawnPosition; }
		protected override int GetSpawnRange() { return Owner.SpawnRange; }

		public MonsterAIController(Monster owner) : base(owner)
        {
            DataManager.MonsterDict.TryGetValue(owner.TemplateId, out MonsterData monsterData);

            _searchCellDist = monsterData.SearchCellDist;
            _chaseCellDist = monsterData.ChaseCellDist;
            _patrolCellDist = monsterData.PatrolCellDist;

            SkillData skillData = monsterData.SkillMap[ESkillSlot.Main];
        }

        public override void SetState(EObjectState state)
        {
            base.SetState(state);

            // TODO: 틱 조절.
            switch (state)
            {
                case EObjectState.Idle:
                    UpdateTick = 1000;
                    break;
                case EObjectState.Move:
                    float speed = Owner.StatComp.MoveSpeed;
                    float distance = Owner.GetActualDistance();
                    float time = distance / speed;
                    UpdateTick = (int)(time * 1000);
                    break;
                case EObjectState.Skill:
                    // TODO: 현재스킬의 쿨타임
                    UpdateTick = (int)(Owner.MonsterData.SkillMap[ESkillSlot.Main].Cooltime * 1000);
                    break;
                case EObjectState.Dead:
                    UpdateTick = 1000;
                    break;
            }
        }

        public override void OnDamaged(BaseObject attacker, float damage)
        {
            if (Owner.State == EObjectState.Dead)
                return;

            base.OnDamaged(attacker, damage);
		}

		protected override Creature FindTarget()
        {
            if (Owner.Room == null)
                return null;

			// 어그로 수치 높은 순서대로 확인.
			List<int> attackerIds = Owner.Aggro.GetTopAttackers();
			foreach (int attackerId in attackerIds)
			{
				Creature target = Owner.Room.GetCreatureById(attackerId);
				if (IsValidTarget(target))
					return target;
			}

			// 비선공 몬스터는 주도적으로 대상을 찾지 않는다.
			if (Owner.MonsterData.IsAggressive == false)
				return null;

			// 대상의 후보군을 모두 구한 다음, 근접한 순서대로 체크.
			List<Hero> heroes = Owner.Room.FindAdjacentHeroes(Owner.CellPos, hero =>
			{
				if (hero.IsValid() == false)
					return false;

				return hero.GetDistance(Owner) <= _searchCellDist;
			});

			heroes.Sort((a, b) => { return a.GetDistance(Owner).CompareTo(b.GetDistance(Owner)); });

			foreach (Hero hero in heroes)
			{
				if (IsValidTarget(hero))
					return hero;
			}

			return null;
		}

        bool IsValidTarget(Creature creature)
        {
            // 적인지 확인.
            if (Owner.IsEnemy(creature) == false)
                return false;

			// 사용할 스킬 거리에 있으면 선택.
			int dist = creature.GetDistance(Owner);
			int skillRange = Owner.SkillComp.GetNextUseSkillDistance(creature.ObjectId);			
			if (dist <= skillRange)
				return true;

			// 가는 경로가 있으면 선택.
			List<Vector2Int> path = Owner.Room?.Map.FindPath(Owner, Owner.CellPos, creature.CellPos);
			if (path != null && path.Count >= 2 && path.Count <= _chaseCellDist)
				return true;

            return false;
		}

        public override void Reset()
        {
            base.Reset();
            _target = null;
            _patrolDest = null;
        }
    }
}
