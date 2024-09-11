using GameServer;
using Google.Protobuf.Protocol;

namespace Server.Data
{
    /// <summary>
    /// 파싱에 제외할 어트리뷰트 정의 
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class ExcludeFieldAttribute : Attribute
    {
    }

    #region BaseData

    public class BaseData
    {
        public int TemplateId;
    }

    public class CreatureData : BaseData
    {
        public int MaxHp;
        public int HpRegen;
        public int MaxMp;
        public int MpRegen;
        public int Attack;
        public int Def;
        public int Dodge;
        public int AtkSpeed;
        public int MoveSpeed;
        public float CriRate;
        public float CriDamage;
        public int Str;
        public int Dex;
        public int Int;
        public int Con;
        public int Wis;

        [ExcludeField]
        public StatInfo Stat;
        public virtual bool Validate()
        {
            return true;
        }
    }

    public class ItemData : BaseData
    {
        public EItemType Type;
        public EItemSubType SubType;
        public EItemGrade Grade;
        public int MaxStack;

        [ExcludeField]
        public bool Stackable;
    }

    public class NpcData
    {
        public int TemplateId;
        public string Name;
        public ENpcType NpcType;
        public int ExtraCells;
        public int Range;

        public int OwnerRoomId { get; private set; }
        public int SpawnPosX { get; private set; }
        public int SpawnPosY { get; private set; }

        [ExcludeField]
        public PositionInfo SpawnPosInfo;
    }
    #endregion

    #region BaseStat

    public class BaseStatData
    {
        public int Level;
        public int Attack;
        public int MaxHp;
        public int MaxMp;
        public int HpRegen;
        public int MpRegen;
        public int Def;
        public int Dodge;
        public int AtkSpeed;
        public int MoveSpeed;
        public float CriRate;
        public float CriDamage;
        public int Str;
        public int Dex;
        public int Int;
        public int Con;
        public int Wis;
        public int Exp;
    }

    [Serializable]
    public class BaseStatDataLoader : IDataLoader<int, BaseStatData>
    {
        public List<BaseStatData> baseStatDatas = new List<BaseStatData>();

        public Dictionary<int, BaseStatData> MakeDictionary()
        {
            Dictionary<int, BaseStatData> dict = new Dictionary<int, BaseStatData>();
            foreach (BaseStatData stat in baseStatDatas)
                dict.Add(stat.Level, stat);

            return dict;
        }

        public bool Validate()
        {
            return true;
        }
    }

    #endregion

    #region Hero

    public class HeroData : CreatureData
    {
        public EHeroClass HeroClass;
        public List<int> SkillDataIds;

        [ExcludeField]
        public List<SkillData> SkillDatas = new List<SkillData>();
        [ExcludeField]
        public Dictionary<ESkillSlot, SkillData> SkillMap = new Dictionary<ESkillSlot, SkillData>();
    }

    [Serializable]
    public class HeroDataLoader : IDataLoader<int, HeroData>
    {
        public List<HeroData> heroes = new List<HeroData>();

        public Dictionary<int, HeroData> MakeDictionary()
        {
            Dictionary<int, HeroData> dict = new Dictionary<int, HeroData>();
            foreach (HeroData hero in heroes)
                dict.Add(hero.TemplateId, hero);

            return dict;
        }

        public bool Validate()
        {
            bool validate = true;

            foreach (var hero in heroes)
            {
                hero.Stat = new StatInfo()
                {
                    MaxHp = hero.MaxHp,
                    HpRegen = hero.HpRegen,
                    MaxMp = hero.MaxMp,
                    MpRegen = hero.MpRegen,
                    Attack = hero.Attack,
                    Defence = hero.Def,
                    Dodge = hero.Dodge,
                    AttackSpeed = hero.AtkSpeed,
                    MoveSpeed = hero.MoveSpeed,
                    CriRate = hero.CriRate,
                    CriDamage = hero.CriDamage,
                    Str = hero.Str,
                    Dex = hero.Dex,
                    Int = hero.Int,
                    Con = hero.Con,
                    Wis = hero.Wis
                };

                for (int i = 0; i < hero.SkillDataIds.Count; i++)
                {
                    if (DataManager.SkillDict.TryGetValue(hero.SkillDataIds[i], out SkillData skillData))
                    {
                        hero.SkillDatas.Add(skillData);

                        // SkillMap에 추가
                        if (i + 1 < Enum.GetValues(typeof(ESkillSlot)).Length)
                        {
                            hero.SkillMap[(ESkillSlot)i + 1] = skillData;
                        }
                    }
                }
            }

            return validate;
        }
    }

    #endregion

    #region Monster

    public class MonsterData : CreatureData
    {
        public bool IsBoss = false;
        public bool IsAggressive;
        public int ExtraCells;
        public List<int> SkillDataIds;

        //AI
        public int SearchCellDist;
        public int ChaseCellDist;
        public int PatrolCellDist;

        //Drop
        public int RewardTableId;

        [ExcludeField]
        public List<SkillData> SkillDatas = new List<SkillData>();
        [ExcludeField]
        public Dictionary<ESkillSlot, SkillData> SkillMap = new Dictionary<ESkillSlot, SkillData>();
        [ExcludeField]
        public RewardTableData RewardTable;
    }

    [Serializable]
    public class MonsterDataLoader : IDataLoader<int, MonsterData>
    {
        public List<MonsterData> monsters = new List<MonsterData>();

        public Dictionary<int, MonsterData> MakeDictionary()
        {
            Dictionary<int, MonsterData> dict = new Dictionary<int, MonsterData>();
            foreach (MonsterData monster in monsters)
            {
                dict.Add(monster.TemplateId, monster);
            }


            return dict;
        }

        public bool Validate()
        {
            bool validate = true;
            foreach (var monster in monsters)
            {
                monster.Stat = new StatInfo()
                {
                    MaxHp = monster.MaxHp,
                    HpRegen = monster.HpRegen,
                    MaxMp = monster.MaxMp,
                    MpRegen = monster.MpRegen,
                    Attack = monster.Attack,
                    Defence = monster.Def,
                    Dodge = monster.Dodge,
                    AttackSpeed = monster.AtkSpeed,
                    MoveSpeed = monster.MoveSpeed,
                    CriRate = monster.CriRate,
                    CriDamage = monster.CriDamage,
                    Str = monster.Str,
                    Dex = monster.Dex,
                    Int = monster.Int,
                    Con = monster.Con,
                    Wis = monster.Wis
                };

                for (int i = 0; i < monster.SkillDataIds.Count; i++)
                {
                    if (DataManager.SkillDict.TryGetValue(monster.SkillDataIds[i], out SkillData skillData))
                    {
                        monster.SkillDatas.Add(skillData);

                        if (i + 1 < Enum.GetValues(typeof(ESkillSlot)).Length)
                        {
                            monster.SkillMap[(ESkillSlot)i + 1] = skillData;
                        }
                    }
                }

                DataManager.RewardTableDict.TryGetValue(monster.RewardTableId, out monster.RewardTable);
            }
            return validate;
        }
    }

    #endregion

    #region Equipment

    public class EquipmentData : ItemData
    {
        public EItemSlotType SlotType;
        public bool canTrade;
        public bool canDelete;
        public bool canStorable;
        public int MaxHpBonus;
        public int AttackBonus;
        public int DefenceBonus;
        public int EffectDataId;
        public int SafeEnhancementLevel;
        public int NextLevelItemDataId;

        [ExcludeField]
        public int BaseItemDataId;
        [ExcludeField]
        public EffectData EffectData;
        [ExcludeField]
        public ItemData NextLevelItem;
    }

    [Serializable]
    public class EquipmentDataLoader : IDataLoader<int, EquipmentData>
    {
        public List<EquipmentData> items = new List<EquipmentData>();

        public Dictionary<int, EquipmentData> MakeDictionary()
        {
            Dictionary<int, EquipmentData> dict = new Dictionary<int, EquipmentData>();
            foreach (EquipmentData item in items)
                dict.Add(item.TemplateId, item);

            return dict;
        }

        public bool Validate()
        {
            bool validate = true;

            foreach (var equipmentData in items)
            {
                DataManager.EffectDict.TryGetValue(equipmentData.EffectDataId, out equipmentData.EffectData);
                DataManager.ItemDict.TryGetValue(equipmentData.NextLevelItemDataId, out equipmentData.NextLevelItem);

                equipmentData.BaseItemDataId = FindBaseId(equipmentData);
            }


            return validate;
        }

        private Dictionary<int, int> baseItemMemo = new Dictionary<int, int>();
        private int FindBaseId(EquipmentData data)
        {
            if (baseItemMemo.ContainsKey(data.TemplateId))
            {
                return baseItemMemo[data.TemplateId];
            }

            if (DataManager.ItemDict.TryGetValue(data.TemplateId - 1, out ItemData prev))
            {
                EquipmentData prevItem = prev as EquipmentData;

                if (prevItem == null || prevItem.NextLevelItemDataId == 0)
                {
                    baseItemMemo[data.TemplateId] = data.TemplateId;
                }
                else
                {
                    baseItemMemo[data.TemplateId] = FindBaseId(prevItem);
                }
            }
            else
            {
                baseItemMemo[data.TemplateId] = data.TemplateId;
            }

            return baseItemMemo[data.TemplateId];
        }
    }

    #endregion

    #region Consumable

    public class ConsumableData : ItemData
    {
        public int EffectId;
        public int CoolTime;
        public EConsumableGroupType ConsumableGroupType;

        [ExcludeField]
        public EffectData EffectData;
    }

    [Serializable]
    public class ConsumableDataLoader : IDataLoader<int, ConsumableData>
    {
        public List<ConsumableData> items = new List<ConsumableData>();

        public Dictionary<int, ConsumableData> MakeDictionary()
        {
            Dictionary<int, ConsumableData> dict = new Dictionary<int, ConsumableData>();
            foreach (ConsumableData item in items)
                dict.Add(item.TemplateId, item);

            return dict;
        }

        public bool Validate()
        {
            bool validate = true;

            foreach (ConsumableData item in items)
            {
                item.Stackable = true;
                DataManager.EffectDict.TryGetValue(item.EffectId, out item.EffectData);
            }

            return validate;
        }
    }

    #endregion

    #region RewardTableData

    [Serializable]
    public class RewardTableData
    {
        public int TemplateId;
        public int RewardGold;
        public int RewardExp;
        public List<int> RewardDataIds;

        [ExcludeField]
        public List<RewardData> Rewards;
    }

    [Serializable]
    public class RewardTableDataLoader : IDataLoader<int, RewardTableData>
    {
        public List<RewardTableData> rewardTables = new List<RewardTableData>();

        public Dictionary<int, RewardTableData> MakeDictionary()
        {
            Dictionary<int, RewardTableData> dict = new Dictionary<int, RewardTableData>();
            foreach (RewardTableData rewardTableData in rewardTables)
            {
                dict.Add(rewardTableData.TemplateId, rewardTableData);
            }

            return dict;
        }

        public bool Validate()
        {
            foreach (var rewardTable in rewardTables)
            {
                rewardTable.Rewards = new List<RewardData>();
                foreach (var id in rewardTable.RewardDataIds)
                {
                    RewardData reward;
                    if (DataManager.RewardDict.TryGetValue(id, out reward))
                    {
                        rewardTable.Rewards.Add(reward);
                    }
                }
            }
            return true;
        }
    }

    #endregion

    #region Reward

    public class RewardData
    {
        public int TemplateId;
        public int ItemTemplateId;
        public int Probability; // 10000분율
        public int Count;

        [ExcludeField]
        public ItemData Item;
    }

    [Serializable]
    public class RewardDataLoader : IDataLoader<int, RewardData>
    {
        public List<RewardData> rewards = new List<RewardData>();

        public Dictionary<int, RewardData> MakeDictionary()
        {
            Dictionary<int, RewardData> dict = new Dictionary<int, RewardData>();
            foreach (RewardData rewardData in rewards)
            {
                dict.Add(rewardData.TemplateId, rewardData);
            }

            return dict;
        }

        public bool Validate()
        {
            foreach (var reward in rewards)
            {
                DataManager.ItemDict.TryGetValue(reward.ItemTemplateId, out reward.Item);
            }

            return true;
        }
    }

    #endregion

    #region EffectData

    public struct StatValuePair
    {
        public EStatType StatType;
        public float AddValue;
    }

    public class EffectData : BaseData
    {
        public string SoundLabel;

        public EEffectType EffectType;
        // 즉발 vs 주기 vs 영구
        public EDurationPolicy DurationPolicy;
        public float Duration;
        // EFFECT_TYPE_DAMAGE
        public float DamageValue;
        // EFFECT_TYPE_BUFF_STAT
        public List<EStatType> StatType;
        public List<float> AddValue;
        // EFFECT_TYPE_BUFF_LIFE_STEAL
        public float LifeStealValue;
        // EFFECT_TYPE_BUFF_LIFE_STUN
        public float StunValue;

        [ExcludeField]
        public List<StatValuePair> StatValues = new List<StatValuePair>();
    }

    [Serializable]
    public class EffectDataLoader : IDataLoader<int, EffectData>
    {
        public List<EffectData> datas = new List<EffectData>();

        public Dictionary<int, EffectData> MakeDictionary()
        {
            Dictionary<int, EffectData> dict = new Dictionary<int, EffectData>();
            foreach (EffectData data in datas)
                dict.Add(data.TemplateId, data);

            return dict;
        }

        public bool Validate()
        {
            bool validate = true;

            foreach (var data in datas)
            {
                data.StatValues = new List<StatValuePair>();
                for (int i = 0; i < data.StatType.Count; i++)
                {
                    data.StatValues.Add(new StatValuePair()
                    {
                        StatType = data.StatType[i],
                        AddValue = data.AddValue[i],
                    });
                }
            }
            return validate;
        }
    }

    #endregion

    #region Projectile
    public class ProjectileData : BaseData
    {
        public float Duration;
        public float Range;
        public float Speed;
        public float Count;
    }

    [Serializable]
    public class ProjectileDataLoader : IDataLoader<int, ProjectileData>
    {
        public List<ProjectileData> datas = new List<ProjectileData>();

        public Dictionary<int, ProjectileData> MakeDictionary()
        {
            Dictionary<int, ProjectileData> dict = new Dictionary<int, ProjectileData>();
            foreach (ProjectileData data in datas)
                dict.Add(data.TemplateId, data);

            return dict;
        }

        public bool Validate()
        {
            bool validate = true;
            return validate;
        }
    }
    #endregion

    #region Skill

    public class SkillData : BaseData
    {
        public ESkillType SkillType;
        public EItemGrade SkillGrade;
        public float Cooltime;
        public int SkillRange;
        public string AnimName;
        public int Cost;

        /// <summary>
        /// 애니메이션 싱크를 위한 딜레이.
        /// </summary>
        public float DelayTime; // EventTime

        /// <summary>
        /// 투사체를 날릴 경우. 
        /// </summary>
        public int ProjectileId;

        /// <summary>
        /// 스킬시전 대상
        /// </summary>
        public EUseSkillTargetType UseSkillTargetType;

        /// <summary>
        /// 대상 범위 (0이면 단일 스킬)
        /// </summary>
        public int GatherTargetRange;
        public string GatherTargetPrefabName; // AoE

        /// <summary>
        /// 피아식별
        /// </summary>
        public ETargetFriendType TargetFriendType;

        /// <summary>
        /// 어떤 효과인지?
        /// </summary>
        public int EffectDataId;

        /// <summary>
        /// 다음 레벨 스킬.
        /// </summary>
        public int NextLevelSkillId;

        [ExcludeField]
        public SkillData NextLevelSkill;
        [ExcludeField]
        public ProjectileData ProjectileData;
        [ExcludeField]
        public EffectData EffectData;
        [ExcludeField]
        public bool IsSingleTarget;
    }

    [Serializable]
    public class SkillDataLoader : IDataLoader<int, SkillData>
    {
        public List<SkillData> skills = new List<SkillData>();

        public Dictionary<int, SkillData> MakeDictionary()
        {
            Dictionary<int, SkillData> dict = new Dictionary<int, SkillData>();
            foreach (SkillData skillData in skills)
                dict.Add(skillData.TemplateId, skillData);

            return dict;
        }

        public bool Validate()
        {
            bool validate = true;

            foreach (var skill in skills)
            {
                DataManager.SkillDict.TryGetValue(skill.NextLevelSkillId, out skill.NextLevelSkill);
                DataManager.ProjectileDict.TryGetValue(skill.ProjectileId, out skill.ProjectileData);
                DataManager.EffectDict.TryGetValue(skill.EffectDataId, out skill.EffectData);
                skill.IsSingleTarget = skill.GatherTargetRange == 0;
            }
            return validate;
        }
    }

    #endregion

    #region Respawn
    [Serializable]
    public class RespawnData
    {
        public int TemplateId;
        public int Count;
        public int MonsterDataId;
        public ERespawnType RespawnType;
        public int RespawnTime;
        public int PivotPosX;
        public int PivotPosY;
        public int SpawnRange;
    }

    public class RespawnDataLoader : IDataLoader<int, RespawnData>
    {
        public List<RespawnData> respawnDatas = new List<RespawnData>();

        public Dictionary<int, RespawnData> MakeDictionary()
        {
            Dictionary<int, RespawnData> dict = new Dictionary<int, RespawnData>();
            foreach (RespawnData respawnData in respawnDatas)
            {
                dict.Add(respawnData.TemplateId, respawnData);
            }
            return dict;
        }

        public bool Validate()
        {
            return true;
        }
    }
    #endregion

    #region SpawningPool

    [Serializable]
    public class SpawningPoolData
    {
        public int RoomId;
        public List<RespawnData> RespawnDatas;
    }

    [Serializable]
    public class SpawningPoolDataLoader : IDataLoader<int, SpawningPoolData>
    {
        public List<SpawningPoolData> spawningPools = new List<SpawningPoolData>();

        public Dictionary<int, SpawningPoolData> MakeDictionary()
        {
            Dictionary<int, SpawningPoolData> dict = new Dictionary<int, SpawningPoolData>();
            foreach (SpawningPoolData spawningPool in spawningPools)
            {
                dict.Add(spawningPool.RoomId, spawningPool);
            }
            return dict;
        }

        public bool Validate()
        {
            return true;
        }
    }

    #endregion

    #region Room
    public class RoomData
    {
        public int TemplateId;
        public string PrefabName;
        public SpawningPoolData SpawningPoolData;
        public List<NpcData> Npcs;

    }

    [Serializable]
    public class RoomDataLoader : IDataLoader<int, RoomData>
    {
        public List<RoomData> spawningPools = new List<RoomData>();

        public Dictionary<int, RoomData> MakeDictionary()
        {
            Dictionary<int, RoomData> dict = new Dictionary<int, RoomData>();
            foreach (RoomData spawningPool in spawningPools)
            {
                dict.Add(spawningPool.TemplateId, spawningPool);
            }
            return dict;
        }

        public bool Validate()
        {
            return true;
        }
    }

    #endregion

    #region Portal
    public class PortalData : NpcData
    {
        public int DestPotalId;
        [ExcludeField]
        public PortalData DestPortal;
    }

    [Serializable]
    public class PortalDataLoader : IDataLoader<int, PortalData>
    {
        public List<PortalData> portals = new List<PortalData>();

        public Dictionary<int, PortalData> MakeDictionary()
        {
            Dictionary<int, PortalData> dict = new Dictionary<int, PortalData>();
            foreach (PortalData portal in portals)
            {
                dict.Add(portal.TemplateId, portal);
            }
            return dict;
        }

        public bool Validate()
        {
            foreach (PortalData portal in portals)
            {
                if (DataManager.PortalDict.TryGetValue(portal.DestPotalId, out PortalData portalData))
                    portal.DestPortal = portalData;

                portal.SpawnPosInfo = new PositionInfo();
                portal.SpawnPosInfo.RoomId = portal.OwnerRoomId;
                portal.SpawnPosInfo.PosX = portal.SpawnPosX;
                portal.SpawnPosInfo.PosY = portal.SpawnPosY;
            }
            return true;
        }
    }
    #endregion

    #region Quest
    [Serializable]
    public class QuestData
    {
        public int TemplateId;
        public EQuestType Type;
        public List<int> TaskIds;
        public int Level;
        public int RewardTableId;

        [ExcludeField]
        public List<QuestTaskData> QuestTasks = new List<QuestTaskData>();
        [ExcludeField]
        public RewardTableData RewardTableData = new RewardTableData();
    }

    public class QuestDataLoader : IDataLoader<int, QuestData>
    {
        public List<QuestData> quests = new List<QuestData>();

        public Dictionary<int, QuestData> MakeDictionary()
        {
            Dictionary<int, QuestData> dict = new Dictionary<int, QuestData>();
            foreach (QuestData questData in quests)
                dict.Add(questData.TemplateId, questData);

            return dict;
        }

        public bool Validate()
        {
            bool validate = true;

            // QuestTasks
            foreach (var questData in quests)
            {
                foreach (var taskId in questData.TaskIds)
                {
                    if (DataManager.QuestTaskDict.TryGetValue(taskId, out QuestTaskData questTaskData) == false)
                    {
                        validate = false;
                        continue;
                    }
                    questData.QuestTasks.Add(questTaskData);
                }

                if (DataManager.RewardTableDict.TryGetValue(questData.RewardTableId, out questData.RewardTableData) == false)
                {
                    validate = false;
                }
            }
            return validate;
        }
    }

    public class QuestTaskData
    {
        public int TemplateId;
        public EQuestTaskType TaskType;

        public List<int> ObjectiveDataIds;
        public List<int> ObjectiveCounts;
        public int DialogueId;

        [ExcludeField]
        public PositionInfo TeleportPos;
        [ExcludeField]
        public List<QuestObjective> QuestObjectives = new List<QuestObjective>();
    }

    public class QuestTaskDataLoader : IDataLoader<int, QuestTaskData>
    {
        public List<QuestTaskData> tasks = new List<QuestTaskData>();

        public Dictionary<int, QuestTaskData> MakeDictionary()
        {
            Dictionary<int, QuestTaskData> dict = new Dictionary<int, QuestTaskData>();
            foreach (QuestTaskData questData in tasks)
                dict.Add(questData.TemplateId, questData);

            return dict;
        }

        public bool Validate()
        {
            bool validate = true;

            foreach (var task in tasks)
            {

                for (int i = 0; i < task.ObjectiveDataIds.Count; i++)
                {
                    QuestObjective questObjective = new QuestObjective();
                    questObjective.ObjectiveDataId = task.ObjectiveDataIds[i];
                    questObjective.ObjectiveCount = task.ObjectiveCounts[i];
                    task.QuestObjectives.Add(questObjective);
                }

                // TODO: 텔레포트 위치 찾기
                task.TeleportPos = new PositionInfo();
                switch (task.TaskType)
                {
                    case EQuestTaskType.None:
                        break;
                    case EQuestTaskType.Killtarget:
                        break;
                    case EQuestTaskType.Collection:
                        break;
                    case EQuestTaskType.TalkToNpc:
                        break;
                }

            }
            return validate;
        }
    }


    #endregion
}
