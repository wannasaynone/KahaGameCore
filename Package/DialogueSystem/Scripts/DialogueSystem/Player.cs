using System.Collections.Generic;

namespace KahaGameCore.Package.DialogueSystem
{
    public class Player
    {
        public static event System.Action<int> OnDialogueRead;

        [System.Serializable]
        public class OwingItemData
        {
            public int id;
            public int count;
        }

        public int day;
        public int coin;
        public float x;
        public float y;
        public int currentMapIndex;
        public List<OwingItemData> saveableOwingItems = new List<OwingItemData>();
        public List<int> saveableReadDialogueIDs = new List<int>();
        public List<string> saveableTags = new List<string>();

        public void SetDataToSaveField()
        {
            saveableOwingItems = new List<OwingItemData>(owingItems);
            saveableReadDialogueIDs = new List<int>(readDialogueIDs);
            saveableTags = new List<string>(tags);
        }

        public void LoadDataFromSaveField()
        {
            owingItems.Clear();
            owingItems.AddRange(saveableOwingItems);
            readDialogueIDs.Clear();
            readDialogueIDs.AddRange(saveableReadDialogueIDs);
            tags.Clear();
            tags.AddRange(saveableTags);
        }

        private readonly List<OwingItemData> owingItems = new List<OwingItemData>();
        private readonly List<int> readDialogueIDs = new List<int>();
        private readonly List<string> tags = new List<string>();

        public List<OwingItemData> GetOwingItemDatas()
        {
            return new List<OwingItemData>(owingItems);
        }

        public void AddItem(int id, int count)
        {
            OwingItemData cur = owingItems.Find(x => x.id == id);
            if (cur != null)
            {
                cur.count += count;
            }
            else
            {
                OwingItemData owingItem = new OwingItemData
                {
                    id = id,
                    count = count
                };
                owingItems.Add(owingItem);
            }
        }

        public void RemoveItem(int id, int count)
        {
            OwingItemData cur = owingItems.Find(x => x.id == id);
            if (cur != null)
            {
                cur.count -= count;
                if (cur.count <= 0)
                {
                    owingItems.Remove(cur);
                }
            }
        }

        public bool HasItem(int id)
        {
            OwingItemData cur = owingItems.Find(x => x.id == id);
            return cur != null && cur.count > 0;
        }

        public void ReadDialogue(int id)
        {
            if (!readDialogueIDs.Contains(id))
            {
                readDialogueIDs.Add(id);
            }
            OnDialogueRead?.Invoke(id);
        }

        public bool HasReadDialogue(int id)
        {
            return readDialogueIDs.Contains(id);
        }

        public void AddTag(string tag)
        {
            if (!tags.Contains(tag))
                tags.Add(tag);
        }

        public bool HasTag(params string[] tags)
        {
            foreach (string tag in tags)
                if (!this.tags.Contains(tag))
                    return false;

            return true;
        }

        public void RemoveTag(string tag)
        {
            tags.Remove(tag);
        }
    }
}