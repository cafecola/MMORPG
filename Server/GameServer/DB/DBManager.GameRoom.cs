using Microsoft.EntityFrameworkCore;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.Game;

namespace GameServer
{
	/// <summary>
	/// 게임 로직에서 완료 콜백을 받아 이어서 처리하는 경우
	/// </summary>
    public partial class DBManager : JobSerializer
    {
		// GameThread
		public static void RewardHero(Hero hero, RewardData rewardData)
        {
            if (hero == null || rewardData == null || hero.Room == null || hero.Inven == null)
                return;
			if (hero.Inven.IsInventoryFull())
				return;

			if (rewardData.Count > rewardData.Item.MaxStack)
				return;

			int itemTemplateId = rewardData.Item.TemplateId;
			int remainingAddCount = 1;

			ItemDb newItemDb = null;
			ItemDb stackItemDb = null;
            int stackCount = 0;

			if (rewardData.Item.Stackable)
			{
				remainingAddCount = rewardData.Count;
				
				Item stackItem = hero.Inven.GetAnyInventoryItemByCondition(stackItem => stackItem.TemplateId == itemTemplateId && stackItem.GetAvailableStackCount() > 0);
				if (stackItem != null)
				{
					stackCount = Math.Min(remainingAddCount, stackItem.GetAvailableStackCount());

					// 아이템 수량 증가.
					stackItemDb = new ItemDb
					{
						ItemDbId = stackItem.ItemDbId,
						Count = stackItem.Count + stackCount,
					};

					// 카운트 소모.
					remainingAddCount -= stackCount;

					// 먼저 적용하고 클라로 전송.
					hero.Inven.AddCount(stackItem.ItemDbId, stackCount, sendToClient: false);
				}
			}

			// 새로 생성.
			if (remainingAddCount > 0)
			{
				newItemDb = new ItemDb
				{
					TemplateId = rewardData.Item.TemplateId,
					EquipSlot = EItemSlotType.Inventory,
					Count = remainingAddCount,
					OwnerDbId = hero.HeroDbId,
				};
			}
			
			Push(hero.HeroDbId, () => RewardHero_Step2(hero, rewardData, newItemDb, stackItemDb, stackCount));
        }

		// DBThread
		private static void RewardHero_Step2(Hero hero, RewardData rewardData, ItemDb newItemDb, ItemDb stackItemDb, int stackCount)
        {
			using (GameDbContext db = new())
			{
                if (newItemDb != null)
                    db.Items.Add(newItemDb);

				if (stackItemDb != null)
				{
					db.Entry(stackItemDb).State = EntityState.Unchanged;
					db.Entry(stackItemDb).Property(nameof(ItemDb.Count)).IsModified = true;
				}

				bool success = db.SaveChangesEx();
				if (success)
				{
					hero.Room?.Push(RewardHero_Step3, hero, rewardData, newItemDb, stackItemDb, stackCount);
				}
			}
		}

        // GameThread
        private static void RewardHero_Step3(Hero hero, RewardData rewardData, ItemDb newItemDb, ItemDb stackItemDb, int stackCount)
		{
			if (newItemDb != null)
			{
				Item newItem = Item.MakeItem(newItemDb);
				hero.Inven.Add(newItem, sendToClient: true);
			}
		}
	}
}
