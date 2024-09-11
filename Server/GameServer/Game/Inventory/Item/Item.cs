using Google.Protobuf.Protocol;
using Server.Data;
using GameServer;

namespace Server.Game
{
    public class Item
    {
        public ItemInfo Info { get; } = new ItemInfo();

		public long ItemDbId
		{
			get { return Info.ItemDbId; }
			set { Info.ItemDbId = value; }
		}

		public int TemplateId
        {
            get { return Info.TemplateId; }
            set { Info.TemplateId = value; }
        }

        public EItemSlotType ItemSlotType
        {
            get { return Info.ItemSlotType; }
            set { Info.ItemSlotType = value; }
        }

		public int Count
		{
			get { return Info.Count; }
			set { Info.Count = value; }
		}

		public int OwnerDbId { get; set; }

		public EItemType ItemType { get { return TemplateData.Type; } }
        public EItemSubType SubType { get { return TemplateData.SubType; } }
        public int MaxStack { get { return TemplateData.MaxStack; } }

        public Data.ItemData TemplateData { get { return DataManager.ItemDict[TemplateId]; }}

        protected Item(int templateId)
        {
            TemplateId = templateId;
            ItemSlotType = Utils.GetEquipSlotType(TemplateData.SubType);
        }

        public static Item MakeItem(ItemDb itemDb)
        {
            int templateId = itemDb.TemplateId;
			if (DataManager.ItemDict.TryGetValue(templateId, out ItemData itemData) == false)
                return null;

            Item item = null;

			switch (itemData.Type)
			{
				case EItemType.Equipment:
					item = new Equipment(templateId);
					break;
				case EItemType.Consumable:
                    item = new Consumable(templateId);
					break;
			}

			if (item != null)
			{
                item.ItemDbId = itemDb.ItemDbId;
                item.OwnerDbId = itemDb.OwnerDbId;
                item.ItemSlotType = itemDb.EquipSlot;
				item.Count = itemDb.Count;
            }

            return item;
        }

		public static EItemStatus GetItemStatus(EItemSlotType itemSlotType)
		{
			if (EItemSlotType.None < itemSlotType && itemSlotType < EItemSlotType.EquipmentMax)
				return EItemStatus.Equipped;

			if (itemSlotType == EItemSlotType.Inventory)
				return EItemStatus.Inventory;

			if (itemSlotType == EItemSlotType.Warehouse)
				return EItemStatus.Warehouse;

			return EItemStatus.None;
		}

		public EItemStatus GetItemStatus() { return GetItemStatus(ItemSlotType); }
		public bool IsEquipped() {  return GetItemStatus() == EItemStatus.Equipped; }
        public bool IsInInventory() { return GetItemStatus() == EItemStatus.Inventory; }
        public bool IsInWarehouse() { return GetItemStatus() == EItemStatus.Warehouse; }
        public int GetAvailableStackCount() { return Math.Max(0, MaxStack - Count); }

		public virtual bool CanUseItem(InventoryComponent inventory) { return false; }

		public void AddCount(Hero owner, int addCount, bool sendToClient = false)
        {
			if (owner == null)
				return;

            if (addCount == 0)
				return;

            Count = Math.Clamp(Count + addCount, 0, MaxStack);

			if (Count == 0)
			{
				owner.Inven.Remove(this, sendToClient);
				return;
			}

			if (sendToClient)
				SendUpdatePacket(owner);
		}

		#region SendPacket

		public void SendAddPacket(Hero owner)
		{
			S_AddItem packet = new S_AddItem();
			{
				ItemInfo itemInfo = new ItemInfo();
				itemInfo.MergeFrom(Info);
				packet.Item = itemInfo;
			}

			owner.Session?.Send(packet);
		}

		public void SendDeletePacket(Hero owner)
		{
			S_DeleteItem packet = new S_DeleteItem();
			packet.ItemDbId = ItemDbId;

			owner.Session?.Send(packet);
		}

		public void SendUpdatePacket(Hero owner, EUpdateItemReason reason = EUpdateItemReason.None)
		{
			S_UpdateItem packet = new S_UpdateItem();
			{
				ItemInfo itemInfo = new ItemInfo();
				itemInfo.MergeFrom(Info);
				packet.Item = itemInfo;
			}
			packet.Reason = reason;

			owner.Session?.Send(packet);
		}

		public void SendChangeItemSlotPacket(Hero owner)
		{
			S_ChangeItemSlot packet = new S_ChangeItemSlot();
			packet.ItemDbId = ItemDbId;
			packet.ItemSlotType = ItemSlotType;

			owner.Session?.Send(packet);
		}

		#endregion
	}
}
