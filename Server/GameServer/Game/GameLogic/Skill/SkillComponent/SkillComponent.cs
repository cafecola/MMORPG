using Google.Protobuf.Protocol;
using Server.Data;


namespace GameServer.Game
{
    public class SkillComponent
    {
        public Creature Owner { get; private set; }
        public EGameObjectType OwnerType { get { return Owner.ObjectType; } }
       
        private Dictionary<int/*templateId*/, Skill> _skills = [];

        public SkillComponent(Creature owner)
        {
            Owner = owner;
        }

        #region 스킬 사용

        public Skill GetMainSkill()
        {
            return _skills.Values.First();
        }

		public Skill GetSkill(int templateId)
		{
			if (_skills.TryGetValue(templateId, out Skill skill))
				return skill;

			return null;
		}

        public List<Skill> GetAllSkills()
        {
            return _skills.Values.ToList();
        }

        public bool CanUseSkill(int templateId)
		{
			Skill skill = GetSkill(templateId);
			if (skill == null)
				return false;

			return skill.CheckCooltimeAndState();
		}

		public void UseSkill(int templateId, int targetId)
        {
			Skill skill = GetSkill(templateId);
			if (skill == null)
				return;

			skill.UseSkill(targetId);

		}
        #endregion

        #region 스킬 사용 확인
        
        public int GetNextUseSkillDistance(int targetId)
        {
            // 다음에 사용할 스킬 거리.
            Skill skill = GetNextUseSkill(targetId);
            if (skill != null)
                return skill.GetSkillRange(targetId);

            // 모든 스킬이 쿨 돌고 있으면 기본 스킬 사거리로.
            Skill mainSkill = Owner.SkillComp.GetMainSkill();
            if (mainSkill != null)
                return mainSkill.GetSkillRange(targetId);

            return 0;
        }

        public Skill GetNextUseSkill(int targetId)
        {
            List<Skill> skills = Owner.SkillComp.GetAllSkills();
            foreach (Skill skill in skills)
            {
                if (skill.CanUseSkill(targetId))
                    return skill;
            }

            Skill mainSkill = Owner.SkillComp.GetMainSkill();
            if (mainSkill.CanUseSkill(targetId))
                return mainSkill;

            return null;
        }

        #endregion
        
        #region 스킬 등록 & 쿨타임 관리
        public bool RegisterSkill(int templateId)
        {
            if (_skills.ContainsKey(templateId))
                return false;
            if (DataManager.SkillDict.TryGetValue(templateId, out SkillData skillData) == false)
                return false;

            Skill skill = null;
            if (skillData.ProjectileData != null)
				skill = new ProjectileSkill(templateId, Owner); 
            else
				skill = new NormalSkill(templateId, Owner);

            _skills.Add(templateId, skill);
            return true;
        }

        public bool CheckCooltime(int templateId)
        {
            Skill skill = GetSkill(templateId);
            if (skill == null)
                return false;

            return skill.CheckCooltimeAndState();
        }

        public List<SkillCoolTime> GetRemainingTicks()
        {
            List<SkillCoolTime> cooltimes = new List<SkillCoolTime>();

            foreach (Skill skill in _skills.Values)
            {
                cooltimes.Add(new SkillCoolTime()
                {
                    SkillId = skill.TemplateId,
                    RemainingTicks = (int)skill.GetRemainingCooltimeInTicks()
				});
            }

            return cooltimes;
        }

        public void UpdateCooltime(int templateId)
        {
			Skill skill = GetSkill(templateId);
            if (skill == null)
                return;

            skill.UpdateCooltime();
		}
		#endregion

		public void Clear()
        {
            _skills.Clear();
        }

    }
}
