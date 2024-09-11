using GameServer.Game;
using Google.Protobuf.Protocol;
using Server.Data;

namespace GameServer
{
	public class Monster : Creature
	{
        public MonsterData MonsterData { get; set; }
        public override CreatureData Data
        {
            get { return MonsterData; }
        }
        public bool Boss { get; private set; }
        public Vector2Int SpawnPosition { get; set; }
        public int SpawnRange { get; set; }

        public MonsterAIController AI { get; private set; }
        public AggroComponent Aggro { get; private set; }

		public Monster()
        {
            ObjectType = EGameObjectType.Monster;
            SkillComp = new SkillComponent(this);
        }

        public void Init(int templateId)
        {
			if (DataManager.MonsterDict.TryGetValue(templateId, out MonsterData monsterData) == false)
				return;

			TemplateId = templateId;
            AI = new MonsterAIController(this);
            Aggro = new AggroComponent();

            MonsterData = monsterData;

			StatComp.BaseStat.MergeFrom(monsterData.Stat);
			StatComp.BaseStat.Hp = StatComp.BaseStat.MaxHp;

			StatComp.TotalStat.MergeFrom(StatComp.BaseStat);
			CreatureInfo.TotalStatInfo = StatComp.TotalStat;

            State = EObjectState.Idle;
            Boss = monsterData.IsBoss;
            ExtraCells = monsterData.ExtraCells;

            foreach (var skillData in monsterData.SkillMap.Values)
            {
                SkillComp.RegisterSkill(skillData.TemplateId);
            }
        }

        public override bool IsEnemy(BaseObject target)
        {
            if (base.IsEnemy(target) == false)
                return false;

            if (target.ObjectType == EGameObjectType.Hero)
                return true;

            return false;
        }

        public override void Update()
        {
            base.Update();

            AI.Update();
        }

        public override bool OnDamaged(BaseObject attacker, float damage)
        {
            if (Room == null)
                return false;

            if (State == EObjectState.Dead)
                return false;

            // 어그로 매니저에 전달.
            if (attacker.ObjectType == EGameObjectType.Hero)
                Aggro.OnDamaged(attacker.ObjectId, damage);

            // AI 매니저에 전달.
			AI.OnDamaged(attacker, damage);

            return base.OnDamaged(attacker, damage);
        }

        public override void OnDead(BaseObject attacker)
        {
            GiveRewardToTopAttacker();

            // AI 매니저에 전달.
			AI.OnDead(attacker);

			base.OnDead(attacker);            
        }

		private void GiveRewardToTopAttacker()
        {
			// 어그로 수치가 가장 높고, 같은 방에 있는 영웅한테 준다.
			List<int> attackerIds = Aggro.GetTopAttackers();
			foreach (int attackerId in attackerIds)
			{
				Hero hero = Room.GetHeroById(attackerId);
				if (hero != null)
				{
					GiveReward(hero);
                    return;
				}
			}
		}

        private void GiveReward(Hero hero)
        {
            if (hero.Inven.IsInventoryFull() == false)
            {
                RewardData rewardData = GetRandomReward();
                if (rewardData != null)
                    DBManager.RewardHero(hero, rewardData);
            }

            // 나머지
            if (MonsterData.RewardTable != null)
                hero.RewardExpAndGold(MonsterData.RewardTable);
        }

        public override void Reset()
        {
            base.Reset();

            AI.Reset();
        }

        private RewardData GetRandomReward()
        {
            if (MonsterData.RewardTable == null)
                return null;
            if (MonsterData.RewardTable.Rewards == null)
                return null;
            if (MonsterData.RewardTable.Rewards.Count <= 0)
                return null;

            return MonsterData.RewardTable.Rewards.RandomElementByWeight(e => e.Probability);
        }

    }
}
