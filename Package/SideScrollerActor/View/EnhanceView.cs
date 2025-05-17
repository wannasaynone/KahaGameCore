using System.Collections.Generic;
using KahaGameCore.GameEvent;
using KahaGameCore.Package.SideScrollerActor.InGameEvent;
using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.View
{
    public class EnhanceView : MonoBehaviour
    {
        public class MonsterButtonSetting
        {
            public string guid;
            public string name;
            public string iconPath;
            public int cost;
            public int health;
            public int attack;
            public int defense;
            public float attackSpeed;
            public float moveSpeed;
        }

        public class ViewSetting
        {
            public List<MonsterButtonSetting> monsters;
            public List<MonsterButtonSetting> party;
            public MonsterButtonSetting hero;
            public CurrencySetting currencySetting;
        }

        public class CurrencySetting
        {
            public int darkCurrency;
            public int lightCurrency;
        }

        [SerializeField] private MonsterButton monsterButtonPrefab;
        [SerializeField] private Transform partyMonsterButtonParent;
        [SerializeField] private Transform infoMonsterButtonParent;
        [SerializeField] private GameObject selectHeroFrameRoot;
        [SerializeField] private TMPro.TextMeshProUGUI darkCurrencyText;
        [SerializeField] private TMPro.TextMeshProUGUI lightCurrencyText;
        [SerializeField] private GameObject levelUpButton;
        [SerializeField] private GameObject setToPartyButton;
        [SerializeField] private GameObject infoButton;

        private List<MonsterButton> allClonedMonsterButtons = new List<MonsterButton>();
        private List<MonsterButton> clonedMonsterButtons_party = new List<MonsterButton>();
        private List<MonsterButtonSetting> partyMonsters;


        private string selectedMonsterSaveGuid_party;
        private string selectedMonsterSaveGuid_info;

        public void Initialize(ViewSetting setting)
        {
            selectedMonsterSaveGuid_party = null;
            selectedMonsterSaveGuid_info = null;

            Refresh(setting);

            selectHeroFrameRoot.SetActive(false);

            for (int i = 0; i < allClonedMonsterButtons.Count; i++)
            {
                allClonedMonsterButtons[i].EnableSelectFrame(false);
            }

            darkCurrencyText.text = setting.currencySetting.darkCurrency.ToString("N0");
            lightCurrencyText.text = setting.currencySetting.lightCurrency.ToString("N0");

            levelUpButton.SetActive(false);
            setToPartyButton.SetActive(false);
            infoButton.SetActive(false);
        }

        public void Refresh(ViewSetting setting)
        {
            partyMonsters = new List<MonsterButtonSetting>(setting.party);

            for (int i = 0; i < allClonedMonsterButtons.Count; i++)
            {
                allClonedMonsterButtons[i].OnClick -= MonsterButton_OnClick;
                Destroy(allClonedMonsterButtons[i].gameObject);
            }

            allClonedMonsterButtons.Clear();

            for (int i = 0; i < partyMonsters.Count; i++)
            {
                MonsterButton monsterButton = Instantiate(monsterButtonPrefab, partyMonsterButtonParent);
                monsterButton.transform.localScale = Vector3.one;
                monsterButton.Bind(new MonsterButton.MonsterButtonSetting
                {
                    monsterName = partyMonsters[i].name,
                    monsterGuid = partyMonsters[i].guid,
                    monsterIconPath = partyMonsters[i].iconPath
                });
                monsterButton.OnClick += MonsterButton_OnClick;
                allClonedMonsterButtons.Add(monsterButton);
                clonedMonsterButtons_party.Add(monsterButton);
            }

            for (int i = 0; i < setting.monsters.Count; i++)
            {
                MonsterButton monsterButton = Instantiate(monsterButtonPrefab, infoMonsterButtonParent);
                monsterButton.transform.localScale = Vector3.one;
                monsterButton.Bind(new MonsterButton.MonsterButtonSetting
                {
                    monsterName = setting.monsters[i].name,
                    monsterGuid = setting.monsters[i].guid,
                    monsterIconPath = setting.monsters[i].iconPath
                });
                monsterButton.OnClick += MonsterButton_OnClick;
                allClonedMonsterButtons.Add(monsterButton);

                if (setting.monsters[i].guid == selectedMonsterSaveGuid_info || setting.monsters[i].guid == selectedMonsterSaveGuid_party)
                {
                    monsterButton.Button_OnClick();
                }
            }

            if (string.IsNullOrEmpty(selectedMonsterSaveGuid_party) && string.IsNullOrEmpty(selectedMonsterSaveGuid_info))
            {
                Button_SelectHero();
            }
        }

        private void MonsterButton_OnClick(string saveGuid)
        {
            if (string.IsNullOrEmpty(saveGuid))
            {
                // TODO: add hint?
                return;
            }

            selectHeroFrameRoot.SetActive(false);

            if (clonedMonsterButtons_party.Find(x => x.IsSame(saveGuid)) != null)
            {
                selectedMonsterSaveGuid_party = saveGuid;
                selectedMonsterSaveGuid_info = null;
            }
            else
            {
                selectedMonsterSaveGuid_info = saveGuid;
                selectedMonsterSaveGuid_party = null;
            }

            levelUpButton.SetActive(true);
            setToPartyButton.SetActive(true);
            infoButton.SetActive(true);

            for (int i = 0; i < allClonedMonsterButtons.Count; i++)
            {
                allClonedMonsterButtons[i].EnableSelectFrame(allClonedMonsterButtons[i].IsSame(saveGuid));
            }
        }

        public void Button_LevelUp()
        {
            if (!string.IsNullOrEmpty(selectedMonsterSaveGuid_party))
            {
                EventBus.Publish(new EnhanceView_OnLevelUpButtonPressed() { selectedMonsterGuid = selectedMonsterSaveGuid_party });
            }
            else if (!string.IsNullOrEmpty(selectedMonsterSaveGuid_info))
            {
                EventBus.Publish(new EnhanceView_OnLevelUpButtonPressed() { selectedMonsterGuid = selectedMonsterSaveGuid_info });
            }
            else
            {
                Debug.LogError("No selected monster");
            }
        }

        public void Button_Next()
        {
            EventBus.Publish(new EnhanceView_OnNextButtonClicked());
        }

        public void Button_SelectHero()
        {
            selectedMonsterSaveGuid_party = null;
            selectedMonsterSaveGuid_info = null;
            selectHeroFrameRoot.SetActive(true);

            levelUpButton.SetActive(false);
            setToPartyButton.SetActive(false);
            infoButton.SetActive(true);

            for (int i = 0; i < allClonedMonsterButtons.Count; i++)
            {
                allClonedMonsterButtons[i].EnableSelectFrame(false);
            }
        }

        public void Button_Info()
        {
            if (!string.IsNullOrEmpty(selectedMonsterSaveGuid_info))
            {
                EventBus.Publish(new EnhanceView_OnInfoButtonPressed() { selectedMonsterGuid = selectedMonsterSaveGuid_info });
            }
            else if (!string.IsNullOrEmpty(selectedMonsterSaveGuid_party))
            {
                EventBus.Publish(new EnhanceView_OnInfoButtonPressed() { selectedMonsterGuid = selectedMonsterSaveGuid_party });
            }
            else
            {
                EventBus.Publish(new EnhanceView_OnHeroInfoButtonPressed());
            }
        }
    }
}