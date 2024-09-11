using Microsoft.EntityFrameworkCore;
using Server.Game;

namespace GameServer
{
	/// <summary>
	/// 게임 로직에서 완료 콜백을 받을 필요 없는 경우
	/// </summary>
	public partial class DBManager : JobSerializer
	{
        public static void SaveHeroDbNoti(Hero hero)
		{
			if (hero == null)
				return;

			// DBThread
			Push(hero.HeroDbId, () =>
			{
				using (GameDbContext db = new())
				{
					HeroDb heroDb = db.Heroes.Where(h => h.HeroDbId == hero.HeroDbId).FirstOrDefault();
					if (heroDb == null)
						return;

					heroDb.Level = hero.HeroInfoComp.HeroInfo.Level;
					heroDb.Exp = hero.HeroInfoComp.MyHeroInfo.Exp;
                    heroDb.Hp = (int)hero.StatComp.Hp;
                    heroDb.Mp = (int)hero.StatComp.Mp;
                    heroDb.PosX = hero.PosInfo.PosX;
					heroDb.PosY = hero.PosInfo.PosY;
					heroDb.Gold = hero.HeroInfoComp.MyHeroInfo.CurrencyInfo.Gold;
					heroDb.Dia = hero.HeroInfoComp.MyHeroInfo.CurrencyInfo.Dia;
					heroDb.MapId = hero.HeroInfoComp.MyHeroInfo.MapId;

                    bool success = db.SaveChangesEx();
					if (success == false)
					{
						// TODO: 실패, Kick
					}
				}
			});			
		}

        public static void EquipItemNoti(Hero hero, Item item)
        {
            if (hero == null || item == null)
                return;

            ItemDb itemDb = new()
            {
                ItemDbId = item.ItemDbId,
                EquipSlot = item.ItemSlotType
            };

			// DBThread
			Push(hero.HeroDbId, () =>
            {
                using (GameDbContext db = new())
                {
                    db.Entry(itemDb).State = EntityState.Unchanged;
                    db.Entry(itemDb).Property(nameof(ItemDb.EquipSlot)).IsModified = true;

                    bool success = db.SaveChangesEx();
                    if (success == false)
                    {
						// TODO: 실패, Kick
					}
				}
            });
        }

		public static void DeleteItemNoti(Hero hero, Item item)
		{
			if (hero == null || item == null)
				return;

			if (hero.Inven.GetItemByDbId(item.Info.ItemDbId) == null)
				return;

			// 메모리상에 적용.
			hero.Inven.Remove(item, sendToClient: true);

			// DB에 저장할 데이터 세팅
			ItemDb itemDb = new()
			{
				ItemDbId = item.Info.ItemDbId,
			};

			// DBThread
			Push(hero.HeroDbId, () =>
			{
				using (GameDbContext db = new())
				{
					db.Entry(itemDb).State = EntityState.Deleted;

					bool success = db.SaveChangesEx();
					if (success == false)
					{
						// TODO: 실패, Kick
					}
				}
			});
		}

		public static void UseItemNoti(Hero hero, Item item, int useCount = 1)
		{
			if (hero == null || item == null || hero.Room == null || hero.Inven == null)
				return;
			if (item.Count <= 0)
				return;
			if (hero.Inven.GetInventoryItemByDbId(item.Info.ItemDbId) == null)
				return;
			Consumable consumable = item as Consumable;
			if (consumable == null)
				return;

			// 메모리상에 적용.
			consumable.UseItem(hero, useCount, sendToClient: true);

			// DB에 저장할 데이터 세팅
			ItemDb itemDb = new()
            {
				ItemDbId = item.Info.ItemDbId,
				Count = consumable.Count
			};

			// DBThread
			Push(hero.HeroDbId, () =>
			{
				using (GameDbContext db = new())
				{
					if (itemDb.Count == 0)
					{
						db.Items.Remove(itemDb);
					}
					else
					{
						db.Entry(itemDb).State = EntityState.Unchanged;
						db.Entry(itemDb).Property(nameof(ItemDb.Count)).IsModified = true;
					}

					bool success = db.SaveChangesEx();
					if (success == false)
					{
						// TODO: 실패, Kick
					}
				}
			});
		}
	}
}
