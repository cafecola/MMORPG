using GameServer.Game;

namespace GameServer
{
    public partial class GameRoom : JobSerializer
    {
        public void HandleInteractionNpc(Hero myHero, int npcObjectId)
        {
            if (myHero.IsValid() == false) 
                return;

            if (_npcs.TryGetValue(npcObjectId, out Npc npc) == false)
                return;

            if (npc.Interaction.CanInteract(myHero) == false) 
                return;
            
            npc.Interaction.HandleInteraction(myHero); 
        }

    }
}
