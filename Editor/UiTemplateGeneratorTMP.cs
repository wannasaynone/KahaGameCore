// Assets/Editor/UiTemplateGeneratorTMP.cs
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class UiTemplateGeneratorTMP : EditorWindow
{
    private MonoScript targetScript;
    private Transform customRoot;
    private string rootName = "Root";
    private bool includeLayoutHelpers = true;

    [MenuItem("Tools/UI/UI Template Generator (TMP)")]
    public static void Open() => GetWindow<UiTemplateGeneratorTMP>("TMP UI Template Generator");

    private void OnGUI()
    {
        GUILayout.Label("TMP 一鍵樣板產生器", EditorStyles.boldLabel);
        targetScript = (MonoScript)EditorGUILayout.ObjectField("UI 腳本 (MonoBehaviour)", targetScript, typeof(MonoScript), false);
        customRoot = (Transform)EditorGUILayout.ObjectField("放置根節點 (Optional)", customRoot, typeof(Transform), true);
        rootName = EditorGUILayout.TextField("樣板 Root 名稱", string.IsNullOrWhiteSpace(rootName) ? "Root" : rootName);
        includeLayoutHelpers = EditorGUILayout.ToggleLeft("容器自動加 Layout / ContentSizeFitter（視情況）", includeLayoutHelpers);

        using (new EditorGUI.DisabledScope(targetScript == null))
        {
            if (GUILayout.Button("Generate Template", GUILayout.Height(34)))
            {
                Generate();
            }
        }

        EditorGUILayout.HelpBox(
            "功能：\n• 建立 Canvas / EventSystem\n• 建立 <ScriptName>_View 並掛上腳本\n• 以 TMP 為主、自動產生並指派欄位對應元件\n• 支援：TextMeshProUGUI / TMP_InputField / TMP_Dropdown，與常見 UGUI 元件",
            MessageType.Info);
    }

    private void Generate()
    {
        Type type = null;
        if (targetScript != null)
        {
            // Try to get the class using reflection to handle different Unity versions
            var methods = typeof(MonoScript).GetMethods(BindingFlags.Public | BindingFlags.Instance);
            var getClassMethod = methods.FirstOrDefault(m => m.Name == "GetClass" || m.Name == "GetScriptClass");

            if (getClassMethod != null)
            {
                type = getClassMethod.Invoke(targetScript, null) as Type;
            }
            else
            {
                // Fallback: try to get the class from the script name
                var scriptName = targetScript.name;
                type = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .FirstOrDefault(t => t.Name == scriptName);
            }
        }
        if (type == null || !typeof(MonoBehaviour).IsAssignableFrom(type))
        {
            ShowNotification(new GUIContent("請選擇一個 MonoBehaviour 腳本"));
            return;
        }

        // 1) Ensure Canvas + EventSystem
        var canvas = EnsureCanvas();
        EnsureEventSystem();

        // 2) Root parent
        Transform parent = customRoot != null ? customRoot : canvas.transform;
        var root = GetOrCreate(parent, rootName);
        var viewGo = new GameObject($"{type.Name}_View", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(viewGo, "Create UI Template (TMP)");
        viewGo.transform.SetParent(root, false);
        ResetFullStretch(viewGo.GetComponent<RectTransform>());

        // 3) Attach component instance
        var mb = Undo.AddComponent(viewGo, type) as MonoBehaviour;

        // 4) Reflect fields (SerializeField or public) that are Component or RectTransform
        var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(f =>
                (f.IsPublic || f.IsDefined(typeof(SerializeField), true)) &&
                (typeof(Component).IsAssignableFrom(f.FieldType) || f.FieldType == typeof(RectTransform)))
            .ToArray();

        // 5) Build children for each field
        foreach (var f in fields)
        {
            var created = CreateForFieldTMP(f, viewGo.transform);
            if (created != null)
            {
                // Assign back to field
                Undo.RecordObject(mb, "Assign UI Field (TMP)");

                // Handle different return types from CreateForFieldTMP
                if (f.FieldType == typeof(RectTransform))
                {
                    // If we need a RectTransform, get it from the created object
                    RectTransform rt = null;
                    if (created is GameObject go)
                        rt = go.GetComponent<RectTransform>();
                    else if (created is RectTransform rectTransform)
                        rt = rectTransform;

                    if (rt != null)
                        f.SetValue(mb, rt);
                }
                else if (typeof(Component).IsAssignableFrom(f.FieldType))
                {
                    // If we need a specific Component type
                    Component comp = null;
                    if (created is GameObject go)
                        comp = go.GetComponent(f.FieldType);
                    else if (created is Component c && f.FieldType.IsAssignableFrom(c.GetType()))
                        comp = c;

                    if (comp != null)
                        f.SetValue(mb, comp);
                }

                EditorUtility.SetDirty(mb);
            }
        }

        Selection.activeObject = viewGo;
        EditorGUIUtility.PingObject(viewGo);
        Debug.Log($"[TMP Template] Generated template for {type.Name} under {root.GetHierarchyPath()}");
    }

    #region TMP-aware Builders

    private UnityEngine.Object CreateForFieldTMP(FieldInfo f, Transform parent)
    {
        string niceName = ToNiceName(f.Name);
        var ft = f.FieldType;

        // RectTransform only
        if (ft == typeof(RectTransform))
        {
            var go = CreateUIObject($"{niceName}_Rect", parent);
            return go.GetComponent<RectTransform>();
        }

        // --- TMP-first mapping ---
        if (ft == typeof(TextMeshProUGUI))
            return CreateTMPText($"{niceName}_Text", parent, "Label");

        if (ft == typeof(TMP_InputField))
            return CreateTMPInput($"{niceName}_Input", parent, "Enter text...");

        if (ft == typeof(TMP_Dropdown))
            return CreateTMPDropdown($"{niceName}_Dropdown", parent);

        // --- Common UGUI ---
        if (ft == typeof(Image))
            return CreateImage($"{niceName}_Image", parent);

        if (ft == typeof(RawImage))
            return CreateRawImage($"{niceName}_RawImage", parent);

        if (ft == typeof(Button))
            return CreateButtonTMP($"{niceName}_Button", parent, "Button");

        if (ft == typeof(Toggle))
            return CreateToggleTMP($"{niceName}_Toggle", parent, "Option");

        if (ft == typeof(Slider))
            return CreateSlider($"{niceName}_Slider", parent);

        if (ft == typeof(Scrollbar))
            return CreateScrollbar($"{niceName}_Scrollbar", parent);

        if (ft == typeof(ScrollRect))
            return CreateScrollRect($"{niceName}_Scroll", parent);

        if (typeof(LayoutGroup).IsAssignableFrom(ft))
        {
            var host = CreateUIObject($"{niceName}_Layout", parent);
            if (includeLayoutHelpers)
            {
                var v = Undo.AddComponent<VerticalLayoutGroup>(host);
                v.childForceExpandHeight = false;
                v.childForceExpandWidth = true;
                var fitter = Undo.AddComponent<ContentSizeFitter>(host);
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
            return host.GetComponent<RectTransform>();
        }

        // Generic Component: try create empty and add component
        if (typeof(Component).IsAssignableFrom(ft))
        {
            var go = CreateUIObject($"{niceName}_{ft.Name}", parent);
            var c = Undo.AddComponent(go, ft) as Component;
            return c != null ? c : go.GetComponent<RectTransform>();
        }

        // Fallback
        var fallback = CreateUIObject($"{niceName}_{ft.Name}", parent);
        return fallback.GetComponent<RectTransform>();
    }

    private TextMeshProUGUI CreateTMPText(string name, Transform parent, string text)
    {
        var go = CreateUIObject(name, parent);
        var tmp = Undo.AddComponent<TextMeshProUGUI>(go);
        tmp.text = text;
        tmp.alignment = TextAlignmentOptions.Center;
        FitSize(tmp.rectTransform, new Vector2(160, 30));
        return tmp;
    }

    private TMP_InputField CreateTMPInput(string name, Transform parent, string placeholder)
    {
        var go = CreateUIObject(name, parent);
        var bg = Undo.AddComponent<Image>(go);
        FitSize(go.GetComponent<RectTransform>(), new Vector2(240, 40));

        // Text Area
        var viewportGo = CreateUIObject("Text Area", go.transform);
        var viewportRT = viewportGo.GetComponent<RectTransform>();
        ResetFullStretch(viewportRT);
        viewportRT.offsetMin = new Vector2(10, 6);
        viewportRT.offsetMax = new Vector2(-10, -6);
        var mask = Undo.AddComponent<RectMask2D>(viewportGo);

        // Text
        var textGo = CreateUIObject("Text", viewportGo.transform);
        var text = Undo.AddComponent<TextMeshProUGUI>(textGo);
        ResetFullStretch(text.rectTransform);

        // Placeholder
        var phGo = CreateUIObject("Placeholder", viewportGo.transform);
        var ph = Undo.AddComponent<TextMeshProUGUI>(phGo);
        ph.text = placeholder;
        ph.fontStyle = FontStyles.Italic;
        ph.color = new Color(1, 1, 1, 0.5f);
        ResetFullStretch(ph.rectTransform);

        var input = Undo.AddComponent<TMP_InputField>(go);
        input.textViewport = viewportRT;
        input.textComponent = text;
        input.placeholder = ph;
        return input;
    }

    private TMP_Dropdown CreateTMPDropdown(string name, Transform parent)
    {
        var go = CreateUIObject(name, parent);
        var bg = Undo.AddComponent<Image>(go);
        FitSize(go.GetComponent<RectTransform>(), new Vector2(200, 32));

        // Label
        var labelGo = CreateUIObject("Label", go.transform);
        var label = Undo.AddComponent<TextMeshProUGUI>(labelGo);
        label.text = "Option A";
        ResetFullStretch(label.rectTransform);

        // Arrow
        var arrowGo = CreateUIObject("Arrow", go.transform);
        var arrow = Undo.AddComponent<TextMeshProUGUI>(arrowGo);
        arrow.text = "▼";
        arrow.alignment = TextAlignmentOptions.MidlineRight;
        arrow.rectTransform.anchorMin = new Vector2(1, 0);
        arrow.rectTransform.anchorMax = new Vector2(1, 1);
        arrow.rectTransform.sizeDelta = new Vector2(24, 0);
        arrow.rectTransform.anchoredPosition = Vector2.zero;

        // Template (basic)
        var templateGo = CreateUIObject("Template", go.transform);
        var templateImg = Undo.AddComponent<Image>(templateGo);
        var scrollRect = Undo.AddComponent<ScrollRect>(templateGo);
        var viewportGo = CreateUIObject("Viewport", templateGo.transform);
        var viewportMask = Undo.AddComponent<Mask>(viewportGo);
        viewportMask.showMaskGraphic = false;
        var viewportImage = Undo.AddComponent<Image>(viewportGo);
        var contentGo = CreateUIObject("Content", viewportGo.transform);
        var layout = Undo.AddComponent<VerticalLayoutGroup>(contentGo);
        ResetFullStretch(templateGo.GetComponent<RectTransform>());
        ResetFullStretch(viewportGo.GetComponent<RectTransform>());
        ResetFullStretch(contentGo.GetComponent<RectTransform>());

        var itemGo = CreateUIObject("Item", contentGo.transform);
        var itemBg = Undo.AddComponent<Toggle>(itemGo);
        var itemLabelGo = CreateUIObject("Item Label", itemGo.transform);
        var itemLabel = Undo.AddComponent<TextMeshProUGUI>(itemLabelGo);
        itemLabel.text = "Option A";

        var dd = Undo.AddComponent<TMP_Dropdown>(go);
        dd.targetGraphic = bg;
        dd.captionText = label;
        dd.template = templateGo.GetComponent<RectTransform>();
        dd.itemText = itemLabel;
        dd.options = new List<TMP_Dropdown.OptionData> {
            new TMP_Dropdown.OptionData("Option A"),
            new TMP_Dropdown.OptionData("Option B"),
        };
        templateGo.SetActive(false); // default TMP behavior
        return dd;
    }

    private Component CreateImage(string name, Transform parent)
    {
        var go = CreateUIObject(name, parent);
        var img = Undo.AddComponent<Image>(go);
        FitSize(go.GetComponent<RectTransform>(), new Vector2(100, 100));
        return img;
    }

    private Component CreateRawImage(string name, Transform parent)
    {
        var go = CreateUIObject(name, parent);
        var raw = Undo.AddComponent<RawImage>(go);
        FitSize(go.GetComponent<RectTransform>(), new Vector2(100, 100));
        return raw;
    }

    private Component CreateButtonTMP(string name, Transform parent, string label)
    {
        var go = CreateUIObject(name, parent);
        var img = Undo.AddComponent<Image>(go);
        var btn = Undo.AddComponent<Button>(go);
        FitSize(go.GetComponent<RectTransform>(), new Vector2(160, 40));

        var text = CreateTMPText("Label", go.transform, label);
        ResetFullStretch(text.rectTransform);
        return btn;
    }

    private Component CreateToggleTMP(string name, Transform parent, string label)
    {
        var go = CreateUIObject(name, parent);
        var bg = CreateImage("Background", go.transform) as Image;
        FitSize(bg.rectTransform, new Vector2(20, 20));
        var check = CreateImage("Checkmark", bg.transform) as Image;
        FitSize(check.rectTransform, new Vector2(20, 20));
        var lbl = CreateTMPText("Label", go.transform, label);
        lbl.rectTransform.anchorMin = new Vector2(0, 0);
        lbl.rectTransform.anchorMax = new Vector2(1, 1);
        lbl.rectTransform.offsetMin = new Vector2(26, 0);
        lbl.rectTransform.offsetMax = new Vector2(0, 0);

        var toggle = Undo.AddComponent<Toggle>(go);
        toggle.targetGraphic = bg;
        toggle.graphic = check;
        FitSize(go.GetComponent<RectTransform>(), new Vector2(180, 22));
        return toggle;
    }

    private Component CreateSlider(string name, Transform parent)
    {
        var go = CreateUIObject(name, parent);
        var bg = CreateImage("Background", go.transform) as Image;
        ResetFullStretch(bg.rectTransform); bg.rectTransform.offsetMin = new Vector2(0, 8); bg.rectTransform.offsetMax = new Vector2(0, -8);
        var fillArea = CreateUIObject("Fill Area", go.transform);
        var fill = CreateImage("Fill", fillArea.transform) as Image;
        ResetFullStretch(fillArea.GetComponent<RectTransform>()); fillArea.GetComponent<RectTransform>().offsetMin = new Vector2(10, 8); fillArea.GetComponent<RectTransform>().offsetMax = new Vector2(-10, -8);
        var handleSlide = CreateUIObject("Handle Slide Area", go.transform);
        var handle = CreateImage("Handle", handleSlide.transform) as Image;
        FitSize(handle.rectTransform, new Vector2(20, 20));

        var slider = Undo.AddComponent<Slider>(go);
        slider.targetGraphic = handle;
        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        FitSize(go.GetComponent<RectTransform>(), new Vector2(220, 22));
        return slider;
    }

    private Component CreateScrollbar(string name, Transform parent)
    {
        var go = CreateUIObject(name, parent);
        var bg = CreateImage("Background", go.transform) as Image;
        ResetFullStretch(bg.rectTransform);
        var sliding = CreateUIObject("Sliding Area", go.transform);
        var handle = CreateImage("Handle", sliding.transform) as Image;
        var sb = Undo.AddComponent<Scrollbar>(go);
        sb.targetGraphic = handle;
        FitSize(go.GetComponent<RectTransform>(), new Vector2(200, 20));
        return sb;
    }

    private Component CreateScrollRect(string name, Transform parent)
    {
        var go = CreateUIObject(name, parent);
        var viewport = CreateUIObject("Viewport", go.transform);
        var mask = Undo.AddComponent<Mask>(viewport);
        mask.showMaskGraphic = false;
        var viewportImg = Undo.AddComponent<Image>(viewport);
        var content = CreateUIObject("Content", viewport.transform);
        var fitter = Undo.AddComponent<ContentSizeFitter>(content);
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ResetFullStretch(viewport.GetComponent<RectTransform>());
        var sr = Undo.AddComponent<ScrollRect>(go);
        sr.viewport = viewport.GetComponent<RectTransform>();
        sr.content = content.GetComponent<RectTransform>();
        FitSize(go.GetComponent<RectTransform>(), new Vector2(320, 220));
        return sr;
    }

    #endregion

    #region Common Helpers

    private Canvas EnsureCanvas()
    {
        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            var go = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(go, "Create Canvas");
            canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
        }
        return canvas;
    }

    private void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
        }
    }

    private GameObject CreateUIObject(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Create UI Object");
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        return go;
    }

    private Transform GetOrCreate(Transform parent, string name)
    {
        var child = parent.Find(name);
        if (child != null) return child;
        var go = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Create Root");
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    private void ResetFullStretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
    }

    private void FitSize(RectTransform rt, Vector2 size)
    {
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;
    }

    private string ToNiceName(string fieldName)
    {
        var n = fieldName;
        foreach (var p in new[] { "m_", "_" })
            if (n.StartsWith(p)) n = n.Substring(p.Length);
        if (n.EndsWith("Field")) n = n.Substring(0, n.Length - 5);
        if (n.EndsWith("Component")) n = n.Substring(0, n.Length - 9);
        return string.IsNullOrEmpty(n) ? "Field" : char.ToUpper(n[0]) + n.Substring(1);
    }

    #endregion
}

public static class TransformPathExtTMP
{
    public static string GetHierarchyPath(this Transform t)
    {
        var stack = new List<string>();
        while (t != null) { stack.Add(t.name); t = t.parent; }
        stack.Reverse();
        return string.Join("/", stack);
    }
}
