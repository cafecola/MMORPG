using Server.Data;

namespace GameServer.Game
{
    public class PortalInteraction : INpcInteraction
    {
        private Npc _owner;
        private PortalData _portalData;

        public void SetInfo(Npc owner)
        {
            _owner = owner;

            if (DataManager.PortalDict.TryGetValue(_owner.TemplateId, out _portalData) == false)
                return;
        }

        public void HandleInteraction(Hero myHero)
        {
            if (_portalData == null)
                return;

            myHero.Teleport(_portalData.DestPortal.SpawnPosInfo);
        }

        public bool CanInteract(Hero myHero)
        {
            // 거리 판정.
            if (_portalData.Range < myHero.GetDistance(_owner))
                return false; 
            
            return true;
        }
    }
}
