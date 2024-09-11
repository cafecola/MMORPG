using GameServer.Game;
using Google.Protobuf.Protocol;
using Server;
using Server.Data;
using Server.Game;

namespace GameServer
{
    public class Hero : Creature
    {
        public HeroData HeroData { get; set; }
        
        public override CreatureData Data 
        {
            get { return HeroData; }
        }

        public ClientSession Session { get; set; }
        public VisionCubeComponent Vision { get; protected set; }
        public InventoryComponent Inven { get; private set; }
        public HeroInfoComponent HeroInfoComp { get; private set; }

        // DB상의 고유번호
        public int HeroDbId { get; set; }
        
        private IJob _respawnJob;

        public Hero()
        {
            ObjectType = EGameObjectType.Hero;

            Vision = new VisionCubeComponent(this);
            Inven = new InventoryComponent(this);
            HeroInfoComp = new HeroInfoComponent(this);
		}

        public void Init(HeroDb heroDb)
        {
            HeroDbId = heroDb.HeroDbId;

            // Pos
            ObjectInfo.PosInfo.State = EObjectState.Idle;
            ObjectInfo.PosInfo.PosX = heroDb.PosX;
            ObjectInfo.PosInfo.PosY = heroDb.PosY;

            // MyHeroInfo.
            HeroInfoComp.Init(heroDb);
			HeroInfoComp.HeroInfo.CreatureInfo = CreatureInfo;
			HeroInfoComp.MyHeroInfo.HeroInfo.CreatureInfo.TotalStatInfo = StatComp.TotalStat;

			InitializeHeroData(heroDb);
            InitializeSkills();
            InitializeItems(heroDb);
        }

        public void RefreshStat()
        {
            EffectComp.Clear();

			// BaseStat, TotalStat
			StatComp.InitStat(HeroInfoComp.Level);
         
            // 장비아이템 refresh
            Inven.ApplyEquipmentEffects();

            SendRefreshStat();
        }

        #region Init
      
        private void InitializeHeroData(HeroDb heroDb)
        {
            TemplateId = ObjectManager.GetTemplateIdFromId(ObjectId);

            if (DataManager.HeroDict.TryGetValue(TemplateId, out HeroData heroData))
            {
                HeroData = heroData;
                StatComp.BaseStat.MergeFrom(heroData.Stat);
				StatComp.InitStat(heroDb.Level, false);
            }

			// DB 저장된 HP/MP가 없다면 full로 채운다.
			StatComp.Hp = heroDb.Hp == -1 ? StatComp.BaseStat.MaxHp : heroDb.Hp;
			StatComp.Mp = heroDb.Mp == -1 ? StatComp.BaseStat.MaxMp : heroDb.Mp;
        }

        private void InitializeSkills()
        {
            if (HeroData == null)
                return;

            foreach (var skillData in HeroData.SkillMap.Values)
            {
                SkillComp.RegisterSkill(skillData.TemplateId);
            }
        }

        private void InitializeItems(HeroDb heroDb)
        {
            Inven.Init(heroDb.Items.ToList());

            // 장착한 아이템 이펙트 적용
            Inven.ApplyEquipmentEffects();
        }

        public override void Update()
        {
            base.Update();
            
            StatComp.TickRegenStatJob();
        }

        public override void Reset()
        {
            base.Reset();
            // 장착한 아이템 이펙트 적용
            Inven.ApplyEquipmentEffects();
        }
        #endregion

        #region Battle
        public void ReserveRebirth()
        {
            GameRoom room = Room;

            _respawnJob = room?.PushAfter(3000, () =>
            {
				room.EnterGame(this, cellPos: CellPos);
            });
        }

        public override void CancelJobs()
        {
            base.CancelJobs();

            if (_respawnJob != null)
            {
				_respawnJob.Cancel = true;
				_respawnJob = null;
            }                
        }
        #endregion

        #region Item
        public void AddItemBonusStat(EquipmentData data)
        {
            // MaxHp
            float prev = StatComp.GetTotalStat(EStatType.MaxHp);
			StatComp.SetTotalStat(EStatType.MaxHp, prev + data.MaxHpBonus);
			// Attack
			StatComp.Attack += data.AttackBonus;
			// Defence
			StatComp.Defence += data.DefenceBonus;
        }

        public void RemoveItemBonusStat(EquipmentData data)
        {
            // MaxHp
            float prev = StatComp.GetTotalStat(EStatType.MaxHp);
			StatComp.SetTotalStat(EStatType.MaxHp, prev - data.MaxHpBonus);
			// Attack
			StatComp.Attack -= data.AttackBonus;
			// Defence
			StatComp.Defence -= data.DefenceBonus;
        }

        public void RewardExpAndGold(RewardTableData dropTable)
        {
            HeroInfoComp.AddExp(dropTable.RewardExp);
			HeroInfoComp.Gold += dropTable.RewardGold;
            
            S_RewardValue packet = new S_RewardValue()
            {
                Exp = dropTable.RewardExp,
                Gold = dropTable.RewardGold,
            };
            Session?.Send(packet);
        }
		#endregion

		#region Packet

		public void SendRefreshStat()
		{
			S_RefreshStat changeStat = new S_RefreshStat();
			changeStat.TotalStatInfo = StatComp.TotalStat;
			Session?.Send(changeStat);
		}

		#endregion
	}
}
