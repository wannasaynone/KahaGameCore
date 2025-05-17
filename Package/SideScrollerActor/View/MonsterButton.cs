using UnityEngine;
using UnityEngine.UI;

namespace KahaGameCore.Package.SideScrollerActor.View
{
    public class MonsterButton : MonoBehaviour
    {
        public class MonsterButtonSetting
        {
            public string monsterName;
            public string monsterIconPath;
            public string monsterGuid;
        }

        public event System.Action<string> OnClick;

        [SerializeField] private GameObject selectFrameRoot;
        [SerializeField] private Image monsterIcon;

        private string referenceSaveGuid;

        public void Bind(MonsterButtonSetting MonsterButtonSetting)
        {
            referenceSaveGuid = MonsterButtonSetting.monsterGuid;
            monsterIcon.sprite = Resources.Load<Sprite>(MonsterButtonSetting.monsterIconPath);
            if (string.IsNullOrEmpty(referenceSaveGuid))
            {
                monsterIcon.color = Color.gray;
            }
            else
            {
                monsterIcon.color = Color.white;
            }
        }

        public void EnableSelectFrame(bool enable)
        {
            selectFrameRoot.SetActive(enable);
        }

        public bool IsSame(string saveGuid)
        {
            return referenceSaveGuid == saveGuid;
        }

        public void Button_OnClick()
        {
            OnClick?.Invoke(referenceSaveGuid);
        }
    }
}