using Google.Protobuf.Protocol;
using Server.Data;
using GameServer;

namespace Server.Game
{
	public class Equipment : Item
    {
        public EItemSubType EquipType { get; private set; }
        public EffectData EffectData { get; private set; }
		public EquipmentData EquipmentData { get; private set; }
        public int Damage { get; private set; }
        public int Defence { get; private set; }
        public int Speed { get; private set; }

        public Equipment(int templateId) : base(templateId)
        {
            Init();
        }

        void Init()
        {
            if (TemplateData == null)
                return;

            if (TemplateData.Type != EItemType.Equipment)
                return;

            EquipmentData = (EquipmentData)TemplateData;
            {
                EquipType = EquipmentData.SubType;
                EffectData = EquipmentData.EffectData;
            }
        }

		/// <summary>
		/// 아이템 장착했을 때의 슬롯 번호.
		/// </summary>
		/// <returns></returns>
		public EItemSlotType GetEquipSlotType()
		{
			return Utils.GetEquipSlotType(SubType);
		}

		public void Equip(InventoryComponent inventory)
        {
			if (IsInInventory() == false)
				return;

			Hero owner = inventory.Owner;
			if (owner == null)
				return;

            EItemSlotType equipSlotType = GetEquipSlotType();

			// 같은 부위에 이미 장착 아이템이 있다면 일단 벗는다.
			if (inventory.EquippedItems.TryGetValue(equipSlotType, out Equipment prev))
            {
                if (prev == this)
                    return;

                prev.UnEquip(inventory);
			}

			inventory.InventoryItems.Remove(ItemDbId);

			// 장착 아이템에 추가.
			inventory.EquippedItems[equipSlotType] = this;

			// 슬롯 갱신.
			ItemSlotType = equipSlotType;

			DBManager.EquipItemNoti(owner, this);

			// 아이템 보너스 적용
			owner.AddItemBonusStat(EquipmentData);

			// 장착한 아이템 이펙트 적용.
			if (EffectData != null)
				owner.EffectComp.ApplyEffect(EffectData, owner);

			// 패킷전송.
			SendChangeItemSlotPacket(owner);
            owner.SendRefreshStat();
        }

        public void UnEquip(InventoryComponent inventory)
        {
			if (IsEquipped() == false)
                return;

			Hero owner = inventory.Owner;
			if (owner == null)
				return;

			EItemSlotType equipSlotType = GetEquipSlotType();
            if (equipSlotType != ItemSlotType)
                return;

			// 장착 아이템에서 제거.
			inventory.EquippedItems.Remove(equipSlotType);

            // 인벤토리에 추가.
			inventory.InventoryItems.Add(ItemDbId, this);

            // 슬롯 갱신.
			ItemSlotType = EItemSlotType.Inventory;

			// DB에 Noti.
			DBManager.EquipItemNoti(owner, this);

            // 아이템 보너스 삭제
            owner.RemoveItemBonusStat(EquipmentData);

            // 기존 아이템 이펙트 제거.
            if (EffectData != null)
				owner.EffectComp.RemoveItemEffect(EffectData.TemplateId);

			// 패킷전송.
			SendChangeItemSlotPacket(owner);
			owner.SendRefreshStat();
		}
    }
}
