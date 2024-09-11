using Google.Protobuf.Protocol;
using Server.Data;
using GameServer;

namespace Server.Game
{
	public class Consumable : Item
	{
		public ConsumableData ConsumableData { get; private set; }
		public EffectData EffectData { get; private set; }
		public EConsumableGroupType ConsumableGroupType { get { return ConsumableData.ConsumableGroupType; } }
		public int CoolTime { get { return ConsumableData.CoolTime; } }

		private long _nextUseTick = 0;

		public Consumable(int templateId) : base(templateId)
		{
			Init();
		}

		void Init()
		{
			if (TemplateData == null)
				return;

			if (TemplateData.Type != EItemType.Consumable)
				return;

			ConsumableData = (ConsumableData)TemplateData;
			{
				EffectData = ConsumableData.EffectData;
			}
		}

		public void UseItem(Hero owner, int useCount, bool sendToClient = false)
		{
			if (owner == null)
				return;

			int remainingCooltime = owner.Inven.UpdateCooltime(ItemDbId);

			// 클라에 패킷 전송. (차감 전에 쿨타임 갱신)
			if (sendToClient)
				SendUseItemPacket(owner, remainingCooltime);

			// 아이템 수량 차감.
			AddCount(owner, -useCount, sendToClient);

			// 아이템 효과 적용.
			if (EffectData != null)
				owner.EffectComp.ApplyEffect(EffectData, owner);
		}

		#region 소모품/쿨타임

		public override bool CanUseItem(InventoryComponent inventory)
		{
			if (Count <= 0)
				return false;

			// 쿨타임 확인.
			if (GetRemainingCooltimeInTicks(inventory) > 0)
				return false;

			return true;
		}

		public int UpdateCooltime(InventoryComponent inventory)
		{
			int cooltimeTick = (1000 * ConsumableData.CoolTime);
			long nextUseTick = Utils.TickCount + cooltimeTick;

			// 그룹 쿨타임 적용.
			if (ConsumableGroupType != EConsumableGroupType.None)
				inventory.ItemGroupCooltimeDic[ConsumableGroupType] = nextUseTick;
			else
				_nextUseTick = nextUseTick;

			return cooltimeTick;
		}

		public int GetRemainingCooltimeInTicks(InventoryComponent inventory)
		{
			// 그룹 쿨타임 적용.
			if (ConsumableGroupType != EConsumableGroupType.None)
				return inventory.GetRemainingItemGroupCooltimeInTicks(ConsumableGroupType);

			return (int)Math.Max(0, _nextUseTick - Utils.TickCount);
		}
		#endregion

		#region SendPacket

		public void SendUseItemPacket(Hero owner, int remainingTicks)
		{
			S_UseItem pkt = new S_UseItem();
			pkt.ItemDbId = ItemDbId;
			pkt.RemainingTicks = remainingTicks;

			owner.Session?.Send(pkt);
		}

		#endregion
	}
}
