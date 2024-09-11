
namespace GameServer.Game
{
    public class QuestComponent
    {
        public Dictionary<int, Quest> AllQuests = new Dictionary<int, Quest>();
        public Hero Owner { get; private set; }

        public QuestComponent(Hero owner)
        {
            Owner = owner;
        }

        public void Init(HeroDb heroDb)
        {

        }

    }
}
