using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace PolyBridge.Sandbox.Editor
{
    public class SandboxEditorWindow : EditorWindow
    {
        private SandboxConfig _config;

        private VisualElement _configMissing;
        private VisualElement _configFound;
        private VisualElement _settingsSection;
        private Label _configPathLabel;
        private IMGUIContainer _configInspector;

        public static void ShowWindow()
        {
            var window = GetWindow<SandboxEditorWindow>();
            window.titleContent = new GUIContent("PolyBridge Sandbox");
            window.minSize = new Vector2(320, 400);
        }

        private void CreateGUI()
        {
            var template = Resources.Load<VisualTreeAsset>("SandboxWindow");
            var styleSheet = Resources.Load<StyleSheet>("SandboxWindow");

            if (template != null)
                template.CloneTree(rootVisualElement);

            if (styleSheet != null)
                rootVisualElement.styleSheets.Add(styleSheet);

            _configMissing = rootVisualElement.Q("config-missing");
            _configFound = rootVisualElement.Q("config-found");
            _settingsSection = rootVisualElement.Q("settings-section");
            _configPathLabel = rootVisualElement.Q<Label>("config-path");
            _configInspector = rootVisualElement.Q<IMGUIContainer>("config-inspector");

            rootVisualElement.Q<Button>("btn-create-config")?.RegisterCallback<ClickEvent>(_ => CreateConfig());
            rootVisualElement.Q<Button>("btn-select-config")?.RegisterCallback<ClickEvent>(_ => SelectConfig());

            if (_configInspector != null)
                _configInspector.onGUIHandler = DrawConfigInspector;

            RefreshConfigState();
        }

        private void RefreshConfigState()
        {
            _config = FindConfig();

            if (_config != null)
            {
                var assetPath = AssetDatabase.GetAssetPath(_config);
                var resourcesPath = ExtractResourcesPath(assetPath);
                EditorPrefs.SetString(SandboxConfig.ResourcesPathKey, resourcesPath);

                if (_configPathLabel != null)
                    _configPathLabel.text = assetPath;

                SetVisible(_configMissing, false);
                SetVisible(_configFound, true);
                SetVisible(_settingsSection, true);
            }
            else
            {
                SetVisible(_configMissing, true);
                SetVisible(_configFound, false);
                SetVisible(_settingsSection, false);
            }
        }

        private void DrawConfigInspector()
        {
            if (_config == null) return;

            EditorGUI.BeginChangeCheck();

            _config.autoInitialize = EditorGUILayout.Toggle("Auto Initialize", _config.autoInitialize);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Gestures", EditorStyles.boldLabel);

            for (var i = 0; i < _config.gestures.Count; i++)
            {
                var gesture = _config.gestures[i];
                if (gesture == null) continue;

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(gesture.DisplayName, GUILayout.Width(140));

                if (GUILayout.Button("-", GUILayout.Width(24)))
                {
                    _config.gestures.RemoveAt(i);
                    i--;
                    EditorGUILayout.EndHorizontal();
                    continue;
                }

                EditorGUILayout.EndHorizontal();

                EditorGUI.indentLevel++;
                DrawGestureSettings(gesture);
                EditorGUI.indentLevel--;

                if (i < _config.gestures.Count - 1)
                    EditorGUILayout.Space(2);
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("+ Keyboard Shortcut", GUILayout.Width(140)))
                _config.gestures.Add(new KeyboardShortcutGesture());
            if (GUILayout.Button("+ Multi Touch", GUILayout.Width(110)))
                _config.gestures.Add(new MultiTouchGesture());
            EditorGUILayout.EndHorizontal();

            if (EditorGUI.EndChangeCheck())
                EditorUtility.SetDirty(_config);
        }

        private static void DrawGestureSettings(ISandboxGesture gesture)
        {
            switch (gesture)
            {
                case KeyboardShortcutGesture kb:
                    kb.key = (KeyCode)EditorGUILayout.EnumPopup("Key", kb.key);
                    kb.requireShift = EditorGUILayout.Toggle("Require Shift", kb.requireShift);
                    break;
                case MultiTouchGesture mt:
                    mt.requiredTouches = EditorGUILayout.IntSlider("Touches", mt.requiredTouches, 2, 5);
                    break;
            }
        }

        private void CreateConfig()
        {
            var path = EditorUtility.SaveFilePanelInProject(
                "Create Sandbox Config",
                "SandboxConfig",
                "asset",
                "Choose location for SandboxConfig");

            if (string.IsNullOrEmpty(path)) return;

            var config = CreateInstance<SandboxConfig>();
            AssetDatabase.CreateAsset(config, path);
            AssetDatabase.SaveAssets();

            RefreshConfigState();
        }

        private void SelectConfig()
        {
            if (_config != null)
            {
                Selection.activeObject = _config;
                EditorGUIUtility.PingObject(_config);
            }
        }

        private static SandboxConfig FindConfig()
        {
            var savedPath = EditorPrefs.GetString(SandboxConfig.ResourcesPathKey, "");
            if (!string.IsNullOrEmpty(savedPath))
            {
                var config = Resources.Load<SandboxConfig>(savedPath);
                if (config != null) return config;
            }

            var defaultConfig = Resources.Load<SandboxConfig>(SandboxConfig.DefaultResourcesPath);
            if (defaultConfig != null) return defaultConfig;

            var guids = AssetDatabase.FindAssets("t:SandboxConfig");
            if (guids.Length > 0)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<SandboxConfig>(assetPath);
            }

            return null;
        }

        private static string ExtractResourcesPath(string assetPath)
        {
            var resourcesIndex = assetPath.IndexOf("/Resources/", System.StringComparison.Ordinal);
            if (resourcesIndex < 0) return SandboxConfig.DefaultResourcesPath;

            var relativePath = assetPath.Substring(resourcesIndex + "/Resources/".Length);
            if (relativePath.EndsWith(".asset"))
                relativePath = relativePath.Substring(0, relativePath.Length - ".asset".Length);

            return relativePath;
        }

        private static void SetVisible(VisualElement element, bool visible)
        {
            if (element != null)
                element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
