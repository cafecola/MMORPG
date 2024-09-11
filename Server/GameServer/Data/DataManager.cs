using Server.Data;

namespace GameServer
{
    public interface IValidate
    {
        bool Validate();
    }

    public interface IDataLoader<Key, Value> : IValidate
	{
		Dictionary<Key, Value> MakeDictionary();
	}

	public class DataManager
	{
        private static HashSet<IValidate> _loaders = new HashSet<IValidate>();

        public static Dictionary<int, BaseStatData> BaseStatDict { get; private set; }
        public static Dictionary<int, HeroData> HeroDict { get; private set; }
        public static Dictionary<int, MonsterData> MonsterDict { get; private set; }
		public static Dictionary<int, SkillData> SkillDict { get; private set; }
		public static Dictionary<int, EffectData> EffectDict { get; private set; }
        public static Dictionary<int, ProjectileData> ProjectileDict { get; private set; }
        public static Dictionary<int, RewardData> RewardDict { get; private set; }
        public static Dictionary<int, RewardTableData> RewardTableDict { get; private set; }
        public static Dictionary<int, RespawnData> RespawnDict { get; private set; }
        public static Dictionary<int, SpawningPoolData> SpawningPoolDict { get; private set; }
        public static Dictionary<int, RoomData> RoomDict { get; private set; }
        public static Dictionary<int, NpcData> NpcDict { get; private set; } = [];
        public static Dictionary<int, PortalData> PortalDict { get; private set; }
        public static Dictionary<int, ItemData> ItemDict { get; private set; } = [];
        public static Dictionary<int, EquipmentData> EquipmentDict { get; private set; }
        public static Dictionary<int, ConsumableData> ConsumableDict { get; private set; }
        public static Dictionary<int, QuestData> QuestDict { get; private set; }
        public static Dictionary<int, QuestTaskData> QuestTaskDict { get; private set; }

        public static void LoadData()
		{
            BaseStatDict = LoadJsonData<BaseStatDataLoader, int, BaseStatData>("BaseStatData").MakeDictionary();
            HeroDict = LoadJsonData<HeroDataLoader, int, HeroData>("HeroData").MakeDictionary();
            MonsterDict = LoadJsonData<MonsterDataLoader, int, MonsterData>("MonsterData").MakeDictionary();
			EffectDict = LoadJsonData<EffectDataLoader, int, EffectData>("EffectData").MakeDictionary();
			SkillDict = LoadJsonData<SkillDataLoader, int, SkillData>("SkillData").MakeDictionary();
            ProjectileDict = LoadJsonData<ProjectileDataLoader, int, ProjectileData>("ProjectileData").MakeDictionary();
            RewardDict = LoadJsonData<RewardDataLoader, int, RewardData>("RewardData").MakeDictionary();
            RewardTableDict = LoadJsonData<RewardTableDataLoader, int, RewardTableData>("RewardTableData").MakeDictionary();
            RespawnDict = LoadJsonData<RespawnDataLoader, int, RespawnData>("RespawnData").MakeDictionary();
            SpawningPoolDict = LoadJsonData<SpawningPoolDataLoader, int, SpawningPoolData>("SpawningPoolData").MakeDictionary();
            RoomDict = LoadJsonData<RoomDataLoader, int, RoomData>("RoomData").MakeDictionary();

            #region ItemData
            EquipmentDict = LoadJsonData<EquipmentDataLoader, int, EquipmentData>("EquipmentData").MakeDictionary();
            ConsumableDict = LoadJsonData<ConsumableDataLoader, int, ConsumableData>("ConsumableData").MakeDictionary();

            ItemDict.Clear();

            foreach (var item in EquipmentDict)
                ItemDict.Add(item.Key, item.Value);

            foreach (var item in ConsumableDict)
                ItemDict.Add(item.Key, item.Value);
            #endregion

            #region NpcData
            PortalDict = LoadJsonData<PortalDataLoader, int, PortalData>("PortalData").MakeDictionary();

            NpcDict.Clear();
            foreach (var portal in PortalDict)
            {
                NpcDict.Add(portal.Key, portal.Value);
            }
            #endregion

            #region Quest
            QuestDict = LoadJsonData<QuestDataLoader, int, QuestData>("QuestData").MakeDictionary();
            QuestTaskDict = LoadJsonData<QuestTaskDataLoader, int, QuestTaskData>("QuestTaskData").MakeDictionary();
            #endregion

            Validate();
        }

        private static Loader LoadJsonData<Loader, Key, Value>(string path) where Loader : IDataLoader<Key, Value>
		{
			string text = File.ReadAllText($"{ConfigManager.Config.dataPath}/JsonData/{path}.json");
            Loader loader = Newtonsoft.Json.JsonConvert.DeserializeObject<Loader>(text);
            _loaders.Add(loader);

            return loader;
		}

		private static Loader LoadJsonData<Loader>(string path)
		{
			string text = File.ReadAllText($"{ConfigManager.Config.dataPath}/JsonData/{path}.json");
            return Newtonsoft.Json.JsonConvert.DeserializeObject<Loader>(text);
		}

        private static bool Validate()
        {
            bool success = true;

            foreach (var loader in _loaders)
            {
                if (loader.Validate() == false)
                    success = false;
            }

            _loaders.Clear();

            return success;
        }
    }
}
