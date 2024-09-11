
namespace GameServer
{
    public partial class GameRoom : JobSerializer
    {
        public void HandleEquipItem(Hero myHero, long itemDbId)
        {
            if (myHero.IsValid() == false) 
                return;

            myHero.Inven.EquipItem(itemDbId);
        }

        public void HandleUnEquipItem(Hero myHero, long itemDbId)
        {
            if (myHero.IsValid() == false)
                return;

            myHero.Inven.UnEquipItem(itemDbId);
        }

        public void HandleUseItem(Hero myHero, long itemDbId)
        {
            if (myHero.IsValid() == false)
                return;

            myHero.Inven.HandleUseItem(itemDbId);
        }

        public void HandleDeleteItem(Hero myHero, long itemDbId)
        {
            if (myHero.IsValid() == false)
                return;

            myHero.Inven.HandleDeleteItem(itemDbId);
        }
    }
}
