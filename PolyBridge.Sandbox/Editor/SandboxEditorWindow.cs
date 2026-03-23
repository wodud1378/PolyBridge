using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using PanelSettings = UnityEngine.UIElements.PanelSettings;

namespace PolyBridge.Sandbox.Editor
{
    public class SandboxEditorWindow : EditorWindow
    {
        private SandboxConfig _config;

        private VisualElement _warningContainer;
        private ObjectField _configField;
        private ObjectField _panelSettingsField;
        private Button _createConfigBtn;
        private Button _createPanelSettingsBtn;
        private VisualElement _settingsContainer;
        private Toggle _autoInitToggle;
        private VisualElement _gestureList;

        public static void ShowWindow()
        {
            var window = GetWindow<SandboxEditorWindow>();
            window.titleContent = new GUIContent("PolyBridge Sandbox");
            window.minSize = new Vector2(320, 400);
        }

        private void CreateGUI()
        {
            var styleSheet = Resources.Load<StyleSheet>("SandboxWindow");
            if (styleSheet != null)
                rootVisualElement.styleSheets.Add(styleSheet);

            BuildLayout();
            BindEvents();

            _config = FindConfig();
            Refresh();
        }

        private void BuildLayout()
        {
            var root = rootVisualElement;
            root.style.paddingTop = 8;
            root.style.paddingLeft = 8;
            root.style.paddingRight = 8;

            // Warnings
            _warningContainer = new VisualElement();
            root.Add(_warningContainer);

            // Configuration
            var configSection = new VisualElement();
            configSection.Add(CreateSectionTitle("Configuration"));

            // Config row
            var configRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 4 } };
            _configField = new ObjectField("Config") { objectType = typeof(SandboxConfig) };
            _configField.style.flexGrow = 1;
            configRow.Add(_configField);
            _createConfigBtn = new Button { text = "Create", style = { width = 60 } };
            configRow.Add(_createConfigBtn);
            configSection.Add(configRow);

            // PanelSettings row
            var panelRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 4 } };
            _panelSettingsField = new ObjectField("Panel Settings") { objectType = typeof(PanelSettings) };
            _panelSettingsField.style.flexGrow = 1;
            panelRow.Add(_panelSettingsField);
            _createPanelSettingsBtn = new Button { text = "Create", style = { width = 60 } };
            panelRow.Add(_createPanelSettingsBtn);
            configSection.Add(panelRow);

            root.Add(configSection);

            // Settings
            _settingsContainer = new VisualElement { style = { marginTop = 8 } };
            _settingsContainer.Add(CreateSectionTitle("Settings"));

            _autoInitToggle = new Toggle("Auto Initialize");
            _settingsContainer.Add(_autoInitToggle);

            // Gestures
            var gestureHeader = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 8, marginBottom = 4 } };
            gestureHeader.Add(CreateSectionTitle("Gestures"));
            _settingsContainer.Add(gestureHeader);

            _gestureList = new VisualElement();
            _settingsContainer.Add(_gestureList);

            var addRow = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.FlexEnd, marginTop = 4 } };
            var addKeyboardBtn = new Button { text = "+ Keyboard Shortcut" };
            addKeyboardBtn.clicked += () => AddGesture(new KeyboardShortcutGesture());
            var addTouchBtn = new Button { text = "+ Multi Touch" };
            addTouchBtn.clicked += () => AddGesture(new MultiTouchGesture());
            addRow.Add(addKeyboardBtn);
            addRow.Add(addTouchBtn);
            _settingsContainer.Add(addRow);

            root.Add(_settingsContainer);
        }

        private void BindEvents()
        {
            _configField.RegisterValueChangedCallback(evt =>
            {
                _config = evt.newValue as SandboxConfig;
                if (_config != null && !IsPreloaded(_config))
                    RegisterPreloadedAsset(_config);
                Refresh();
            });

            _panelSettingsField.RegisterValueChangedCallback(evt =>
            {
                if (_config == null) return;
                _config.panelSettings = evt.newValue as PanelSettings;
                EditorUtility.SetDirty(_config);
                Refresh();
            });

            _createConfigBtn.clicked += CreateConfig;
            _createPanelSettingsBtn.clicked += CreatePanelSettings;

            _autoInitToggle.RegisterValueChangedCallback(evt =>
            {
                if (_config == null) return;
                _config.autoInitialize = evt.newValue;
                EditorUtility.SetDirty(_config);
            });
        }

        private void Refresh()
        {
            // Fields
            _configField.SetValueWithoutNotify(_config);
            _createConfigBtn.style.display = _config == null ? DisplayStyle.Flex : DisplayStyle.None;

            if (_config != null)
            {
                _panelSettingsField.SetValueWithoutNotify(_config.panelSettings);
                _autoInitToggle.SetValueWithoutNotify(_config.autoInitialize);
            }

            _panelSettingsField.SetEnabled(_config != null);
            _createPanelSettingsBtn.style.display =
                _config != null && _config.panelSettings == null ? DisplayStyle.Flex : DisplayStyle.None;

            _settingsContainer.style.display = _config != null ? DisplayStyle.Flex : DisplayStyle.None;

            // Warnings
            RebuildWarnings();

            // Gestures
            RebuildGestureList();
        }

        private void RebuildWarnings()
        {
            _warningContainer.Clear();

            if (_config != null && !IsPreloaded(_config))
            {
                var box = new HelpBox("Config is not in Preloaded Assets. It won't load at runtime.", HelpBoxMessageType.Warning);
                var btn = new Button { text = "Register to Preloaded Assets" };
                btn.clicked += () => { RegisterPreloadedAsset(_config); Refresh(); };
                _warningContainer.Add(box);
                _warningContainer.Add(btn);
            }

            if (_config != null && _config.panelSettings == null)
            {
                var box = new HelpBox("PanelSettings is required for Sandbox UI to render.", HelpBoxMessageType.Warning);
                _warningContainer.Add(box);
            }
        }

        private void RebuildGestureList()
        {
            _gestureList.Clear();
            if (_config == null) return;

            for (var i = 0; i < _config.gestures.Count; i++)
            {
                var gesture = _config.gestures[i];
                if (gesture == null) continue;
                var index = i;

                var card = new VisualElement { style = { marginBottom = 4, paddingLeft = 4, paddingRight = 4, paddingTop = 4, paddingBottom = 4 } };

                // Header
                var header = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween } };
                header.Add(new Label(gesture.DisplayName) { style = { unityFontStyleAndWeight = FontStyle.Bold } });
                var removeBtn = new Button { text = "-", style = { width = 24 } };
                removeBtn.clicked += () => { _config.gestures.RemoveAt(index); EditorUtility.SetDirty(_config); RebuildGestureList(); };
                header.Add(removeBtn);
                card.Add(header);

                // Fields
                switch (gesture)
                {
                    case KeyboardShortcutGesture kb:
                    {
                        var keyField = new EnumField("Key", kb.key);
                        keyField.RegisterValueChangedCallback(evt => { kb.key = (KeyCode)evt.newValue; EditorUtility.SetDirty(_config); });
                        card.Add(keyField);

                        var shiftToggle = new Toggle("Require Shift") { value = kb.requireShift };
                        shiftToggle.RegisterValueChangedCallback(evt => { kb.requireShift = evt.newValue; EditorUtility.SetDirty(_config); });
                        card.Add(shiftToggle);
                        break;
                    }
                    case MultiTouchGesture mt:
                    {
                        var slider = new SliderInt("Touches", 2, 5) { value = mt.requiredTouches, showInputField = true };
                        slider.RegisterValueChangedCallback(evt => { mt.requiredTouches = evt.newValue; EditorUtility.SetDirty(_config); });
                        card.Add(slider);
                        break;
                    }
                }

                _gestureList.Add(card);
            }
        }

        private void AddGesture(ISandboxGesture gesture)
        {
            if (_config == null) return;
            _config.gestures.Add(gesture);
            EditorUtility.SetDirty(_config);
            RebuildGestureList();
        }

        private void CreateConfig()
        {
            var path = EditorUtility.SaveFilePanelInProject(
                "Create Sandbox Config", "SandboxConfig", "asset",
                "Choose location for SandboxConfig");
            if (string.IsNullOrEmpty(path)) return;

            var config = CreateInstance<SandboxConfig>();
            AssetDatabase.CreateAsset(config, path);
            AssetDatabase.SaveAssets();
            RegisterPreloadedAsset(config);

            _config = config;
            CreatePanelSettings();
            Refresh();
        }

        private void CreatePanelSettings()
        {
            if (_config == null) return;

            var configPath = AssetDatabase.GetAssetPath(_config);
            string path;

            if (!string.IsNullOrEmpty(configPath))
            {
                var dir = System.IO.Path.GetDirectoryName(configPath);
                path = $"{dir}/SandboxPanelSettings.asset";
            }
            else
            {
                path = EditorUtility.SaveFilePanelInProject(
                    "Create Panel Settings", "SandboxPanelSettings", "asset",
                    "Choose location for PanelSettings");
                if (string.IsNullOrEmpty(path)) return;
            }

            var ps = CreateInstance<PanelSettings>();
            AssetDatabase.CreateAsset(ps, path);
            _config.panelSettings = ps;
            EditorUtility.SetDirty(_config);
            AssetDatabase.SaveAssets();
            Refresh();
        }

        private static SandboxConfig FindConfig()
        {
            var guids = AssetDatabase.FindAssets("t:SandboxConfig");
            if (guids.Length == 0) return null;
            var assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<SandboxConfig>(assetPath);
        }

        private static bool IsPreloaded(Object asset)
        {
            return PlayerSettings.GetPreloadedAssets().Contains(asset);
        }

        private static void RegisterPreloadedAsset(Object asset)
        {
            var preloaded = PlayerSettings.GetPreloadedAssets().ToList();
            if (preloaded.Contains(asset)) return;
            preloaded.Add(asset);
            PlayerSettings.SetPreloadedAssets(preloaded.ToArray());
        }

        private static Label CreateSectionTitle(string text)
        {
            return new Label(text)
            {
                style =
                {
                    fontSize = 14,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    color = new Color(0.86f, 0.86f, 0.86f),
                    marginBottom = 6,
                    paddingBottom = 4,
                    borderBottomWidth = 1,
                    borderBottomColor = new Color(0.31f, 0.31f, 0.31f)
                }
            };
        }
    }
}
