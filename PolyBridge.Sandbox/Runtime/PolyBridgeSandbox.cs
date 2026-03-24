using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace PolyBridge.Sandbox
{
    [AddComponentMenu("PolyBridge/Sandbox")]
    public partial class PolyBridgeSandbox : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;

        private readonly Dictionary<Type, object> _instances = new();
        private readonly List<Delegate> _eventHandlers = new();
        private List<SandboxServiceInfo> _services;
        private SandboxConsole _console;

        private enum ConsoleState { Minimized, Medium, Maximized }
        private enum SubTab { Methods, Events }

        // Service tabs
        private VisualElement _tabBar;
        private int _activeServiceTab;

        // Sub tabs
        private VisualElement _subTabBar;
        private Button _methodsSubTab;
        private Button _eventsSubTab;
        private SubTab _activeSubTab = SubTab.Methods;

        // Content
        private VisualElement _methodPanel;
        private ScrollView _contentScroll;

        // Console
        private VisualElement _consolePanel;
        private VisualElement _consoleToolbar;
        private ScrollView _consoleScroll;
        private TextField _searchField;
        private Button _minimizeBtn;
        private Button _mediumBtn;
        private Button _maximizeBtn;
        private ConsoleState _consoleState = ConsoleState.Minimized;
        private readonly HashSet<SandboxLogLevel> _activeFilters = new()
        {
            SandboxLogLevel.Info, SandboxLogLevel.Warning, SandboxLogLevel.Error, SandboxLogLevel.Native
        };
        private readonly Dictionary<SandboxLogLevel, Button> _filterButtons = new();

        // Event log storage per event name
        private readonly Dictionary<string, List<(string time, string data)>> _eventLogs = new();
        private readonly Dictionary<string, ScrollView> _eventLogViews = new();

        private INativeLogReader _nativeLogger;
        private float _nativeLogTimer;

        private void OnEnable()
        {
            try
            {
                if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
                if (uiDocument == null)
                {
                    Debug.LogError("[PolyBridge Sandbox] UIDocument not found.");
                    return;
                }

                _console = new SandboxConsole();
                _console.OnLogAdded += OnLogAdded;
                _nativeLogger = NativeLogReaderFactory.Create();
                _services = SandboxScanner.ScanAll();
                Debug.Log($"[PolyBridge Sandbox] Scanned {_services.Count} service(s).");
                BuildUI();
            }
            catch (Exception e)
            {
                Debug.LogError($"[PolyBridge Sandbox] OnEnable failed: {e}");
            }
        }

        private void OnDisable()
        {
            UnsubscribeAllEvents();
            if (_console != null)
            {
                _console.OnLogAdded -= OnLogAdded;
                _console.Dispose();
                _console = null;
            }
        }

        private void Update()
        {
            if (_nativeLogger == null || _console == null) return;
            _nativeLogTimer += Time.unscaledDeltaTime;
            if (_nativeLogTimer >= 2f)
            {
                _nativeLogTimer = 0f;
                _nativeLogger.ReadNewLogs(_console);
            }
        }

        // ============ UI Build ============

        private void BuildUI()
        {
            var root = uiDocument.rootVisualElement;
            root.Clear();

            foreach (var ussName in new[] { "SandboxContainer", "SandboxMethod", "SandboxConsole" })
            {
                var ss = Resources.Load<StyleSheet>(ussName);
                if (ss != null) root.styleSheets.Add(ss);
            }

            root.style.backgroundColor = new Color(0.12f, 0.12f, 0.12f);

            var containerTemplate = Resources.Load<VisualTreeAsset>("SandboxContainer");
            var methodTemplate = Resources.Load<VisualTreeAsset>("SandboxMethodPanel");
            var consoleTemplate = Resources.Load<VisualTreeAsset>("SandboxConsolePanel");

            if (containerTemplate != null)
                containerTemplate.CloneTree(root);

            var sandboxRoot = root.Q(className: "sandbox-root");
            if (sandboxRoot != null)
                ApplySafeArea(sandboxRoot);
            else
            {
                root.Add(new Label("Sandbox UXML templates not found."));
                return;
            }

            _tabBar = root.Q("tab-bar");
            _methodPanel = root.Q("method-panel");
            _consolePanel = root.Q("console-panel");

            _subTabBar = new VisualElement();
            _subTabBar.AddToClassList("sandbox-sub-tab-bar");
            _methodsSubTab = new Button(() => SelectSubTab(SubTab.Methods)) { text = "Methods" };
            _eventsSubTab = new Button(() => SelectSubTab(SubTab.Events)) { text = "Events" };
            _methodsSubTab.AddToClassList("sandbox-sub-tab");
            _eventsSubTab.AddToClassList("sandbox-sub-tab");
            _subTabBar.Add(_methodsSubTab);
            _subTabBar.Add(_eventsSubTab);
            _methodPanel.Insert(0, _subTabBar);

            // Method/event scroll
            if (_methodPanel.Q("method-scroll") == null && methodTemplate != null)
                methodTemplate.CloneTree(_methodPanel);
            _contentScroll = root.Q<ScrollView>("method-scroll");

            // Console
            if (_consolePanel.Q("console-scroll") == null && consoleTemplate != null)
                consoleTemplate.CloneTree(_consolePanel);
            _consoleScroll = root.Q<ScrollView>("console-scroll");
            _consoleToolbar = root.Q("console-toolbar");

            var btnGroup = root.Q("console-state-buttons");
            _minimizeBtn = new Button(() => SetConsoleState(ConsoleState.Minimized)) { text = "\u2014" };
            _mediumBtn = new Button(() => SetConsoleState(ConsoleState.Medium)) { text = "\u25A1" };
            _maximizeBtn = new Button(() => SetConsoleState(ConsoleState.Maximized)) { text = "\u25A0" };
            _minimizeBtn.AddToClassList("sandbox-console-state-btn");
            _mediumBtn.AddToClassList("sandbox-console-state-btn");
            _maximizeBtn.AddToClassList("sandbox-console-state-btn");
            btnGroup.Add(_minimizeBtn);
            btnGroup.Add(_mediumBtn);
            btnGroup.Add(_maximizeBtn);

            PopulateConsoleToolbar();
            ApplyConsoleState();

            if (_services.Count == 0)
            {
                var emptyLabel = new Label("No [Sandbox] services found.");
                emptyLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
                emptyLabel.style.paddingTop = 16;
                emptyLabel.style.paddingLeft = 16;
                _contentScroll.Add(emptyLabel);
                return;
            }

            for (var i = 0; i < _services.Count; i++)
            {
                var index = i;
                var tab = new Button(() => SelectServiceTab(index)) { text = _services[i].DisplayName };
                tab.AddToClassList("sandbox-tab");
                _tabBar.Add(tab);
            }

            SelectServiceTab(0);
        }

        // ============ Service Tabs ============

        private void SelectServiceTab(int index)
        {
            _activeServiceTab = index;
            for (var i = 0; i < _tabBar.childCount; i++)
            {
                if (i == index) _tabBar[i].AddToClassList("sandbox-tab--active");
                else _tabBar[i].RemoveFromClassList("sandbox-tab--active");
            }

            var service = _services[index];
            _eventsSubTab.style.display = service.HasEvents ? DisplayStyle.Flex : DisplayStyle.None;

            if (_activeSubTab == SubTab.Events && !service.HasEvents)
                _activeSubTab = SubTab.Methods;

            SelectSubTab(_activeSubTab);
        }

        // ============ Sub Tabs ============

        private void SelectSubTab(SubTab subTab)
        {
            _activeSubTab = subTab;

            if (subTab == SubTab.Methods)
            {
                _methodsSubTab.AddToClassList("sandbox-sub-tab--active");
                _eventsSubTab.RemoveFromClassList("sandbox-sub-tab--active");
            }
            else
            {
                _eventsSubTab.AddToClassList("sandbox-sub-tab--active");
                _methodsSubTab.RemoveFromClassList("sandbox-sub-tab--active");
            }

            RefreshContent();
        }

        private void RefreshContent()
        {
            _contentScroll.Clear();
            var service = _services[_activeServiceTab];
            var instance = GetOrCreateInstance(service);

            if (instance == null)
            {
                var errorLabel = new Label($"Failed to create {service.ServiceType.Name}");
                errorLabel.style.color = new Color(0.9f, 0.3f, 0.3f);
                errorLabel.style.paddingTop = 16;
                errorLabel.style.paddingLeft = 16;
                _contentScroll.Add(errorLabel);
                return;
            }

            if (_activeSubTab == SubTab.Methods)
            {
                foreach (var method in service.Methods)
                    _contentScroll.Add(BuildMethodCard(instance, method));
            }
            else
            {
                SubscribeEvents(instance, service);
                foreach (var evt in service.Events)
                    _contentScroll.Add(BuildEventCard(evt));
            }
        }

        private object GetOrCreateInstance(SandboxServiceInfo service)
        {
            if (_instances.TryGetValue(service.ServiceType, out var existing))
                return existing;

            try
            {
                var instance = Activator.CreateInstance(service.ServiceType);
                _instances[service.ServiceType] = instance;
                Debug.Log($"[PolyBridge Sandbox] Created instance: {service.ServiceType.Name}");
                return instance;
            }
            catch (Exception e)
            {
                Debug.LogError($"[PolyBridge Sandbox] Failed to create {service.ServiceType.Name}: {e}");
                return null;
            }
        }

        // ============ Helpers ============

        private static TextField CreateInputField(Type type)
        {
            var field = new TextField();
            field.style.flexGrow = 1;
            if (type == typeof(int)) field.value = "0";
            else if (type == typeof(float)) field.value = "0";
            else if (type == typeof(bool)) field.value = "false";
            else field.value = "";
            return field;
        }

        private static string GetTypeLabel(Type type)
        {
            if (type == typeof(string)) return "string";
            if (type == typeof(int)) return "int";
            if (type == typeof(float)) return "float";
            if (type == typeof(double)) return "double";
            if (type == typeof(bool)) return "bool";
            if (type == typeof(long)) return "long";
            return type.Name;
        }

        private static void ApplySafeArea(VisualElement root)
        {
            var safeArea = Screen.safeArea;

            root.style.paddingLeft = safeArea.x;
            root.style.paddingTop = Screen.height - safeArea.yMax;
            root.style.paddingRight = Screen.width - safeArea.xMax;
            root.style.paddingBottom = safeArea.y;
        }
    }
}
