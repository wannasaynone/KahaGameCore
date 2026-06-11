using KahaGameCore.UserInterfaceSystem;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace KahaGameCore.Package.GameFlowSystem.DefaultViews.Editor
{
    /// <summary>
    /// 一鍵建置工具：產生 DefaultViews 全部 View prefabs（Assets/Resources/GameFlowUIViews）
    /// 與可直接運行的遊戲場景（Assets/Scenes/GameFlowGame.unity，含 Camera、EventSystem、
    /// 4K Canvas、DialogueView 縮放包覆、DefaultGameLauncher，全部接好）。
    /// 跑完只需準備表格（Inspector 指定 TextAsset 或 Resources/GameData/）即可按 Play。
    /// 可重複執行（會覆寫既有產物）。美術調整建議直接改 prefab；重跑本工具會還原為基礎版面。
    /// </summary>
    public static class DefaultUiBuilder
    {
        private const string PREFAB_FOLDER = "Assets/Resources/GameFlowUIViews";
        private const string SCENE_PATH = "Assets/Scenes/GameFlowGame.unity";
        private const string DIALOGUE_VIEW_PREFAB_PATH = "Assets/KahaGameCore/Package/DialogueSystem/Prefabs/DialogueView.prefab";

        /// <summary>設計解析度（CanvasScaler 參考值）。</summary>
        private static readonly Vector2 referenceResolution = new Vector2(3840f, 2160f);
        /// <summary>KahaGameCore DialogueView prefab 以 1920x1080 設計，需放大至設計解析度。</summary>
        private static readonly Vector2 dialogueViewDesignResolution = new Vector2(1920f, 1080f);

        private static readonly Color panelColor = new Color(0.08f, 0.08f, 0.12f, 0.92f);
        private static readonly Color buttonColor = new Color(0.22f, 0.24f, 0.32f, 1f);

        [MenuItem("KahaGameCore/GameFlowSystem/Build Default UI Prefabs And Scene")]
        public static void BuildAll()
        {
            EnsureFolder(PREFAB_FOLDER);

            string statItemPath = BuildStatValueItemPrefab();
            string actionButtonPath = BuildActionButtonItemPrefab();
            string locationButtonPath = BuildLocationButtonItemPrefab();

            BuildMainMenuViewPrefab();
            BuildGameplayHudViewPrefab(statItemPath);
            BuildActionMenuViewPrefab(actionButtonPath);
            BuildLocationMenuViewPrefab(locationButtonPath);
            BuildHintPopupViewPrefab();
            BuildCreditsViewPrefab();

            BuildGameScene();

            AssetDatabase.SaveAssets();
            Debug.Log("[DefaultUiBuilder] UI prefabs 與 Game 場景建置完成（3840x2160）。");
        }

        #region Item Prefabs

        private static string BuildStatValueItemPrefab()
        {
            GameObject root = CreateUIObject("StatValueItem", null);
            root.GetComponent<RectTransform>().sizeDelta = new Vector2(360f, 80f);
            HorizontalLayoutGroup layout = root.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            TextMeshProUGUI nameText = CreateText("NameText", root.transform, "名稱", 48f, TextAlignmentOptions.MidlineLeft);
            TextMeshProUGUI valueText = CreateText("ValueText", root.transform, "0", 48f, TextAlignmentOptions.MidlineRight);

            StatValueItem item = root.AddComponent<StatValueItem>();
            SetReference(item, "nameText", nameText);
            SetReference(item, "valueText", valueText);

            return SavePrefab(root, "StatValueItem");
        }

        private static string BuildActionButtonItemPrefab()
        {
            GameObject root = CreateButtonObject("ActionButtonItem", null, out Button button);
            root.GetComponent<RectTransform>().sizeDelta = new Vector2(840f, 176f);

            TextMeshProUGUI nameText = CreateText("NameText", root.transform, "行動名稱", 60f, TextAlignmentOptions.MidlineLeft);
            SetRect(nameText.rectTransform, new Vector2(0f, 0.45f), Vector2.one, new Vector2(48f, 0f), new Vector2(-48f, -12f));
            TextMeshProUGUI descriptionText = CreateText("DescriptionText", root.transform, "行動說明", 40f, TextAlignmentOptions.MidlineLeft);
            descriptionText.color = new Color(0.8f, 0.8f, 0.85f, 1f);
            SetRect(descriptionText.rectTransform, Vector2.zero, new Vector2(1f, 0.45f), new Vector2(48f, 12f), new Vector2(-48f, 0f));

            ActionButtonItem item = root.AddComponent<ActionButtonItem>();
            SetReference(item, "button", button);
            SetReference(item, "nameText", nameText);
            SetReference(item, "descriptionText", descriptionText);

            return SavePrefab(root, "ActionButtonItem");
        }

        private static string BuildLocationButtonItemPrefab()
        {
            GameObject root = CreateButtonObject("LocationButtonItem", null, out Button button);
            root.GetComponent<RectTransform>().sizeDelta = new Vector2(840f, 176f);

            TextMeshProUGUI nameText = CreateText("NameText", root.transform, "地點名稱", 60f, TextAlignmentOptions.MidlineLeft);
            SetRect(nameText.rectTransform, new Vector2(0f, 0.45f), Vector2.one, new Vector2(48f, 0f), new Vector2(-48f, -12f));
            TextMeshProUGUI descriptionText = CreateText("DescriptionText", root.transform, "地點說明", 40f, TextAlignmentOptions.MidlineLeft);
            descriptionText.color = new Color(0.8f, 0.8f, 0.85f, 1f);
            SetRect(descriptionText.rectTransform, Vector2.zero, new Vector2(1f, 0.45f), new Vector2(48f, 12f), new Vector2(-48f, 0f));

            LocationButtonItem item = root.AddComponent<LocationButtonItem>();
            SetReference(item, "button", button);
            SetReference(item, "nameText", nameText);
            SetReference(item, "descriptionText", descriptionText);

            return SavePrefab(root, "LocationButtonItem");
        }

        #endregion

        #region View Prefabs

        private static void BuildMainMenuViewPrefab()
        {
            GameObject root = CreateViewRoot("MainMenuView", out CanvasGroup canvasGroup);
            CreateBackground(root.transform, new Color(0.05f, 0.05f, 0.08f, 1f));

            TextMeshProUGUI titleText = CreateText("TitleText", root.transform, "Project II", 168f, TextAlignmentOptions.Center);
            SetRect(titleText.rectTransform, new Vector2(0.5f, 0.65f), new Vector2(0.5f, 0.65f), Vector2.zero, Vector2.zero);
            titleText.rectTransform.sizeDelta = new Vector2(2400f, 320f);

            GameObject startButtonObject = CreateButtonObject("StartButton", root.transform, out Button startButton);
            SetRect(startButtonObject.GetComponent<RectTransform>(), new Vector2(0.5f, 0.32f), new Vector2(0.5f, 0.32f), Vector2.zero, Vector2.zero);
            startButtonObject.GetComponent<RectTransform>().sizeDelta = new Vector2(720f, 192f);
            TextMeshProUGUI startLabel = CreateText("Label", startButtonObject.transform, "開始遊戲", 72f, TextAlignmentOptions.Center);
            StretchFull(startLabel.rectTransform);

            MainMenuView view = root.AddComponent<MainMenuView>();
            SetReference(view, "canvasGroup", canvasGroup);
            SetReference(view, "titleText", titleText);
            SetReference(view, "startButton", startButton);

            SavePrefab(root, "MainMenuView");
        }

        private static void BuildGameplayHudViewPrefab(string statItemPath)
        {
            GameObject root = CreateViewRoot("GameplayHudView", out CanvasGroup canvasGroup);

            GameObject topBar = CreateUIObject("TopBar", root.transform);
            Image topBarImage = topBar.AddComponent<Image>();
            topBarImage.color = new Color(0f, 0f, 0f, 0.55f);
            SetRect(topBar.GetComponent<RectTransform>(), new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -144f), Vector2.zero);

            TextMeshProUGUI dayPhaseText = CreateText("DayPhaseText", topBar.transform, "第 1 天　早晨", 64f, TextAlignmentOptions.MidlineLeft);
            SetRect(dayPhaseText.rectTransform, Vector2.zero, new Vector2(0.35f, 1f), new Vector2(48f, 0f), Vector2.zero);

            GameObject statContainerObject = CreateUIObject("StatContainer", topBar.transform);
            SetRect(statContainerObject.GetComponent<RectTransform>(), new Vector2(0.35f, 0f), Vector2.one, Vector2.zero, new Vector2(-48f, 0f));
            HorizontalLayoutGroup statLayout = statContainerObject.AddComponent<HorizontalLayoutGroup>();
            statLayout.childAlignment = TextAnchor.MiddleRight;
            statLayout.spacing = 64f;
            statLayout.childForceExpandWidth = false;
            statLayout.childForceExpandHeight = true;

            GameObject monologueObject = CreateUIObject("MonologueGroup", root.transform);
            CanvasGroup monologueGroup = monologueObject.AddComponent<CanvasGroup>();
            monologueGroup.blocksRaycasts = false;
            SetRect(monologueObject.GetComponent<RectTransform>(), new Vector2(0.5f, 0.72f), new Vector2(0.5f, 0.72f), Vector2.zero, Vector2.zero);
            monologueObject.GetComponent<RectTransform>().sizeDelta = new Vector2(2200f, 160f);
            Image monologueBackground = monologueObject.AddComponent<Image>();
            monologueBackground.color = new Color(0f, 0f, 0f, 0.45f);
            TextMeshProUGUI monologueText = CreateText("MonologueText", monologueObject.transform, "……", 56f, TextAlignmentOptions.Center);
            StretchFull(monologueText.rectTransform);
            monologueObject.SetActive(false);

            GameplayHudView view = root.AddComponent<GameplayHudView>();
            SetReference(view, "canvasGroup", canvasGroup);
            SetReference(view, "dayPhaseText", dayPhaseText);
            SetReference(view, "statContainer", statContainerObject.GetComponent<RectTransform>());
            SetReference(view, "statItemPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(statItemPath).GetComponent<StatValueItem>());
            SetReference(view, "monologueGroup", monologueGroup);
            SetReference(view, "monologueText", monologueText);

            SavePrefab(root, "GameplayHudView");
        }

        private static void BuildActionMenuViewPrefab(string actionButtonPath)
        {
            GameObject root = CreateViewRoot("ActionMenuView", out CanvasGroup canvasGroup);

            GameObject panel = CreatePanel(root.transform, "Panel", new Vector2(0.5f, 0f), new Vector2(960f, 1240f), new Vector2(0.5f, 0f), new Vector2(0f, 80f));
            GameObject containerObject = CreateUIObject("ButtonContainer", panel.transform);
            StretchFull(containerObject.GetComponent<RectTransform>());
            VerticalLayoutGroup layout = containerObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.spacing = 24f;
            layout.padding = new RectOffset(32, 32, 32, 32);
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ActionMenuView view = root.AddComponent<ActionMenuView>();
            SetReference(view, "canvasGroup", canvasGroup);
            SetReference(view, "buttonContainer", containerObject.GetComponent<RectTransform>());
            SetReference(view, "buttonPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(actionButtonPath).GetComponent<ActionButtonItem>());
            SetFloat(view, "transitionDuration", 0.15f);

            SavePrefab(root, "ActionMenuView");
        }

        private static void BuildLocationMenuViewPrefab(string locationButtonPath)
        {
            GameObject root = CreateViewRoot("LocationMenuView", out CanvasGroup canvasGroup);

            GameObject panel = CreatePanel(root.transform, "Panel", new Vector2(0.5f, 0.5f), new Vector2(960f, 1280f), new Vector2(0.5f, 0.5f), Vector2.zero);

            TextMeshProUGUI title = CreateText("TitleText", panel.transform, "要前往哪裡？", 64f, TextAlignmentOptions.Center);
            SetRect(title.rectTransform, new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -128f), Vector2.zero);

            GameObject containerObject = CreateUIObject("ButtonContainer", panel.transform);
            SetRect(containerObject.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(0f, 220f), new Vector2(0f, -144f));
            VerticalLayoutGroup layout = containerObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.spacing = 24f;
            layout.padding = new RectOffset(32, 32, 16, 16);
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            GameObject cancelButtonObject = CreateButtonObject("CancelButton", panel.transform, out Button cancelButton);
            SetRect(cancelButtonObject.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero);
            cancelButtonObject.GetComponent<RectTransform>().sizeDelta = new Vector2(600f, 144f);
            cancelButtonObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 112f);
            TextMeshProUGUI cancelLabel = CreateText("Label", cancelButtonObject.transform, "返回", 56f, TextAlignmentOptions.Center);
            StretchFull(cancelLabel.rectTransform);

            LocationMenuView view = root.AddComponent<LocationMenuView>();
            SetReference(view, "canvasGroup", canvasGroup);
            SetReference(view, "buttonContainer", containerObject.GetComponent<RectTransform>());
            SetReference(view, "buttonPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(locationButtonPath).GetComponent<LocationButtonItem>());
            SetReference(view, "cancelButton", cancelButton);
            SetFloat(view, "transitionDuration", 0.15f);

            SavePrefab(root, "LocationMenuView");
        }

        private static void BuildHintPopupViewPrefab()
        {
            GameObject root = CreateViewRoot("HintPopupView", out CanvasGroup canvasGroup);
            CreateBackground(root.transform, new Color(0f, 0f, 0f, 0.6f));

            GameObject panel = CreatePanel(root.transform, "Panel", new Vector2(0.5f, 0.5f), new Vector2(1440f, 720f), new Vector2(0.5f, 0.5f), Vector2.zero);

            TextMeshProUGUI messageText = CreateText("MessageText", panel.transform, "提示內容", 60f, TextAlignmentOptions.Center);
            SetRect(messageText.rectTransform, new Vector2(0f, 0.3f), Vector2.one, new Vector2(64f, 0f), new Vector2(-64f, -48f));

            GameObject confirmButtonObject = CreateButtonObject("ConfirmButton", panel.transform, out Button confirmButton);
            SetRect(confirmButtonObject.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero);
            confirmButtonObject.GetComponent<RectTransform>().sizeDelta = new Vector2(520f, 144f);
            confirmButtonObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 128f);
            TextMeshProUGUI confirmLabel = CreateText("Label", confirmButtonObject.transform, "確認", 56f, TextAlignmentOptions.Center);
            StretchFull(confirmLabel.rectTransform);

            HintPopupView view = root.AddComponent<HintPopupView>();
            SetReference(view, "canvasGroup", canvasGroup);
            SetReference(view, "messageText", messageText);
            SetReference(view, "confirmButton", confirmButton);
            SetFloat(view, "transitionDuration", 0.15f);

            SavePrefab(root, "HintPopupView");
        }

        private static void BuildCreditsViewPrefab()
        {
            GameObject root = CreateViewRoot("CreditsView", out CanvasGroup canvasGroup);
            CreateBackground(root.transform, Color.black);

            TextMeshProUGUI creditsText = CreateText("CreditsText", root.transform, "製作人員名單", 68f, TextAlignmentOptions.Center);
            SetRect(creditsText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            creditsText.rectTransform.sizeDelta = new Vector2(2400f, 1600f);

            GameObject finishButtonObject = CreateButtonObject("FinishButton", root.transform, out Button finishButton);
            SetRect(finishButtonObject.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero);
            finishButtonObject.GetComponent<RectTransform>().sizeDelta = new Vector2(600f, 144f);
            finishButtonObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 128f);
            TextMeshProUGUI finishLabel = CreateText("Label", finishButtonObject.transform, "結束", 56f, TextAlignmentOptions.Center);
            StretchFull(finishLabel.rectTransform);

            CreditsView view = root.AddComponent<CreditsView>();
            SetReference(view, "canvasGroup", canvasGroup);
            SetReference(view, "creditsText", creditsText);
            SetReference(view, "finishButton", finishButton);

            SavePrefab(root, "CreditsView");
        }

        #endregion

        #region Scene

        private static void BuildGameScene()
        {
            EnsureFolder("Assets/Scenes");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            cameraObject.AddComponent<AudioListener>();

            GameObject lightObject = new GameObject("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

            GameObject canvasObject = new GameObject("MainCanvas");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = referenceResolution;
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();

            GameObject uiRootObject = CreateUIObject("UIRoot", canvasObject.transform);
            StretchFull(uiRootObject.GetComponent<RectTransform>());

            GameObject overlayRootObject = CreateUIObject("OverlayRoot", canvasObject.transform);
            StretchFull(overlayRootObject.GetComponent<RectTransform>());

            GameObject dialogueViewInstance = InstantiateDialogueView(canvasObject.transform);

            GameObject blackoutObject = CreateUIObject("BlackoutOverlay", canvasObject.transform);
            StretchFull(blackoutObject.GetComponent<RectTransform>());
            Image blackoutImage = blackoutObject.AddComponent<Image>();
            blackoutImage.color = Color.black;
            CanvasGroup blackoutGroup = blackoutObject.AddComponent<CanvasGroup>();
            blackoutObject.SetActive(false);

            UserInterfaceController uiController = canvasObject.AddComponent<UserInterfaceController>();
            SetReference(uiController, "uiRoot", uiRootObject.GetComponent<RectTransform>());
            SetReference(uiController, "blackoutOverlay", blackoutGroup);

            GameObject launcherObject = new GameObject("GameLauncher");
            DefaultGameLauncher launcher = launcherObject.AddComponent<DefaultGameLauncher>();
            SetReference(launcher, "uiController", uiController);
            SetReference(launcher, "overlayRoot", overlayRootObject.GetComponent<RectTransform>());
            if (dialogueViewInstance != null)
            {
                SetReference(launcher, "dialogueView", dialogueViewInstance.GetComponent<ProjectBSR.DialogueSystem.View.DialogueView>());
            }

            EditorSceneManager.SaveScene(scene, SCENE_PATH);
        }

        /// <summary>
        /// DialogueView prefab 以 1920x1080 設計（KahaGameCore 規格），
        /// 因此包一層固定 1080p 尺寸、等比放大的容器，使其鋪滿 4K 設計畫布。
        /// </summary>
        private static GameObject InstantiateDialogueView(Transform canvasTransform)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DIALOGUE_VIEW_PREFAB_PATH);
            if (prefab == null)
            {
                Debug.LogError($"[DefaultUiBuilder] 找不到 DialogueView prefab：{DIALOGUE_VIEW_PREFAB_PATH}，請手動拖入場景並指定給 DefaultGameLauncher。");
                return null;
            }

            GameObject scaleRoot = CreateUIObject("DialogueScaleRoot", canvasTransform);
            RectTransform scaleRect = scaleRoot.GetComponent<RectTransform>();
            scaleRect.anchorMin = scaleRect.anchorMax = new Vector2(0.5f, 0.5f);
            scaleRect.sizeDelta = dialogueViewDesignResolution;
            float scaleFactor = referenceResolution.x / dialogueViewDesignResolution.x;
            scaleRect.localScale = new Vector3(scaleFactor, scaleFactor, 1f);

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.SetParent(scaleRoot.transform, false);
            RectTransform rectTransform = instance.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                StretchFull(rectTransform);
            }
            instance.SetActive(false);
            return instance;
        }

        #endregion

        #region Helpers

        private static GameObject CreateViewRoot(string name, out CanvasGroup canvasGroup)
        {
            GameObject root = CreateUIObject(name, null);
            RectTransform rectTransform = root.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            canvasGroup = root.AddComponent<CanvasGroup>();
            return root;
        }

        private static void SetFloat(Component target, string fieldName, float value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(fieldName);
            if (property == null)
            {
                Debug.LogError($"[DefaultUiBuilder] {target.GetType().Name} 找不到欄位 {fieldName}");
                return;
            }
            property.floatValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            if (parent != null)
            {
                gameObject.transform.SetParent(parent, false);
            }
            return gameObject;
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, string text, float fontSize, TextAlignmentOptions alignment)
        {
            GameObject gameObject = CreateUIObject(name, parent);
            TextMeshProUGUI tmpText = gameObject.AddComponent<TextMeshProUGUI>();
            tmpText.text = text;
            tmpText.fontSize = fontSize;
            tmpText.alignment = alignment;
            tmpText.color = Color.white;
            tmpText.raycastTarget = false;
            return tmpText;
        }

        private static GameObject CreateButtonObject(string name, Transform parent, out Button button)
        {
            GameObject gameObject = CreateUIObject(name, parent);
            Image image = gameObject.AddComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            image.type = Image.Type.Sliced;
            image.color = buttonColor;
            button = gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            return gameObject;
        }

        private static GameObject CreatePanel(Transform parent, string name, Vector2 anchor, Vector2 size, Vector2 pivot, Vector2 anchoredPosition)
        {
            GameObject panel = CreateUIObject(name, parent);
            RectTransform rectTransform = panel.GetComponent<RectTransform>();
            rectTransform.anchorMin = anchor;
            rectTransform.anchorMax = anchor;
            rectTransform.pivot = pivot;
            rectTransform.sizeDelta = size;
            rectTransform.anchoredPosition = anchoredPosition;
            Image image = panel.AddComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            image.type = Image.Type.Sliced;
            image.color = panelColor;
            return panel;
        }

        private static void CreateBackground(Transform parent, Color color)
        {
            GameObject background = CreateUIObject("Background", parent);
            StretchFull(background.GetComponent<RectTransform>());
            Image image = background.AddComponent<Image>();
            image.color = color;
        }

        private static void StretchFull(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        private static void SetRect(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;
        }

        private static void SetReference(Component target, string fieldName, Object value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(fieldName);
            if (property == null)
            {
                Debug.LogError($"[DefaultUiBuilder] {target.GetType().Name} 找不到欄位 {fieldName}");
                return;
            }
            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static string SavePrefab(GameObject root, string prefabName)
        {
            string path = $"{PREFAB_FOLDER}/{prefabName}.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return path;
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string parent = System.IO.Path.GetDirectoryName(folderPath).Replace('\\', '/');
            string folderName = System.IO.Path.GetFileName(folderPath);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }

        #endregion
    }
}
