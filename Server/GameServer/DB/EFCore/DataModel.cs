using Google.Protobuf.Protocol;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameServer
{
    [Table("Hero")]
    public class HeroDb
    {
        // PK
        public int HeroDbId { get; set; }
        public long AccountDbId { get; set; }
        public DateTime CreatedDate { get; private set; }
        public EHeroGender Gender { get; set; }
        public EHeroClass ClassType { get; set; }
        public string Name { get; set; }
        public int Level { get; set; }
        public int Hp { get; set; }
        public int Mp { get; set; }
        public int Exp { get; set; }
        public int MapId { get; set; }
        public int PosX { get; set; }
        public int PosY { get; set; }
        public int Gold { get; set; }
        public int Dia {  get; set; }
        public ICollection<ItemDb> Items { get; set; } = new List<ItemDb>();
    }

    [Table("Item")]
    public class ItemDb
    {
        // PK
        public long ItemDbId { get; set; }
        public int TemplateId { get; set; }
        public EItemSlotType EquipSlot { get; set; }
        public int Count { get; set; }
		
        // FK
        public int OwnerDbId { get; set; }
        public HeroDb OwnerDb { get; set; }
    }
}
