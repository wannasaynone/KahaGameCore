using KahaGameCore.GameData;

namespace KahaGameCore.Package.SideScrollerActor.Data
{
    public class ItemData : IGameData
    {
        public int ID { get; private set; }
        public string Name { get; private set; }
        public string ItemType { get; private set; }
        public string PrefabPath { get; private set; }
        public int NameContextID { get; private set; }
    }
}