using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

namespace PolyBridge.Sandbox
{
    [AddComponentMenu("PolyBridge/Sandbox")]
    public class PolyBridgeSandbox : MonoBehaviour
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
            if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null) return;

            _console = new SandboxConsole();
            _console.OnLogAdded += OnLogAdded;
            _nativeLogger = NativeLogReaderFactory.Create();
            _services = SandboxScanner.ScanAll();
            BuildUI();
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

            var containerTemplate = Resources.Load<VisualTreeAsset>("SandboxContainer");
            var methodTemplate = Resources.Load<VisualTreeAsset>("SandboxMethodPanel");
            var consoleTemplate = Resources.Load<VisualTreeAsset>("SandboxConsolePanel");

            if (containerTemplate != null)
                containerTemplate.CloneTree(root);
            else
            {
                root.Add(new Label("Sandbox UXML templates not found."));
                return;
            }

            _tabBar = root.Q("tab-bar");
            _methodPanel = root.Q("method-panel");
            _consolePanel = root.Q("console-panel");

            // Sub tab bar (inserted between service tabs and content)
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
            _minimizeBtn = new Button(() => SetConsoleState(ConsoleState.Minimized)) { text = "—" };
            _mediumBtn = new Button(() => SetConsoleState(ConsoleState.Medium)) { text = "□" };
            _maximizeBtn = new Button(() => SetConsoleState(ConsoleState.Maximized)) { text = "■" };
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
            var instance = Activator.CreateInstance(service.ServiceType);
            _instances[service.ServiceType] = instance;
            return instance;
        }

        // ============ Event Subscription ============

        private void SubscribeEvents(object instance, SandboxServiceInfo service)
        {
            if (service.BridgeProperty == null) return;

            var bridge = service.BridgeProperty.GetValue(instance);
            if (bridge == null) return;

            foreach (var evt in service.Events)
            {
                if (_eventLogs.ContainsKey(evt.Name)) continue;
                _eventLogs[evt.Name] = new List<(string, string)>();

                var evtName = evt.Name;
                var eventInfo = evt.Event;
                var handlerType = eventInfo.EventHandlerType;
                var invokeMethod = handlerType.GetMethod("Invoke");
                var paramTypes = invokeMethod?.GetParameters().Select(p => p.ParameterType).ToArray() ?? Array.Empty<Type>();

                Action<string> onEvent = data =>
                {
                    var time = DateTime.Now.ToString("HH:mm:ss");
                    _eventLogs[evtName].Add((time, data));
                    if (_eventLogViews.TryGetValue(evtName, out var view))
                        view.Add(BuildEventEntry(time, data));
                };

                var handler = CreateEventHandler(handlerType, paramTypes, onEvent);
                if (handler != null)
                {
                    eventInfo.AddEventHandler(bridge, handler);
                    _eventHandlers.Add(handler);
                }
            }
        }

        private static Delegate CreateEventHandler(Type handlerType, Type[] paramTypes, Action<string> onEvent)
        {
            if (paramTypes.Length == 0)
            {
                Action action = () => onEvent("(no data)");
                return handlerType == typeof(Action)
                    ? action
                    : Delegate.CreateDelegate(handlerType, action.Target, action.Method);
            }

            if (paramTypes.Length == 1 && paramTypes[0] == typeof(string))
                return Delegate.CreateDelegate(handlerType, onEvent.Target, onEvent.Method);

            var parameters = paramTypes.Select(Expression.Parameter).ToArray();
            var boxed = parameters.Select(p => Expression.Convert(p, typeof(object)));
            var array = Expression.NewArrayInit(typeof(object), boxed);

            var joinMethod = typeof(string).GetMethod("Join", new[] { typeof(string), typeof(object[]) });
            var joinCall = Expression.Call(joinMethod, Expression.Constant(", "), array);
            var invokeOnEvent = Expression.Invoke(Expression.Constant(onEvent), joinCall);

            return Expression.Lambda(handlerType, invokeOnEvent, parameters).Compile();
        }

        private void UnsubscribeAllEvents()
        {
            _eventHandlers.Clear();
            _eventLogs.Clear();
            _eventLogViews.Clear();
        }

        // ============ Event Card ============

        private VisualElement BuildEventCard(SandboxEventInfo evt)
        {
            var card = new VisualElement();
            card.AddToClassList("sandbox-event-card");

            var header = new VisualElement();
            header.AddToClassList("sandbox-event-header");

            var label = new Label(evt.Name);
            label.AddToClassList("sandbox-event-label");
            header.Add(label);

            var rightGroup = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
            var badge = new Label("event");
            badge.AddToClassList("sandbox-event-badge");
            rightGroup.Add(badge);
            var clearBtn = new Button(() => ClearEventLog(evt.Name)) { text = "Clear" };
            clearBtn.AddToClassList("sandbox-clear-btn");
            rightGroup.Add(clearBtn);
            header.Add(rightGroup);

            card.Add(header);

            var logView = new ScrollView(ScrollViewMode.Vertical);
            logView.AddToClassList("sandbox-event-log");

            // Render existing logs
            if (_eventLogs.TryGetValue(evt.Name, out var logs))
            {
                foreach (var (time, data) in logs)
                    logView.Add(BuildEventEntry(time, data));
            }

            _eventLogViews[evt.Name] = logView;
            card.Add(logView);

            return card;
        }

        private void ClearEventLog(string eventName)
        {
            if (_eventLogs.TryGetValue(eventName, out var logs))
                logs.Clear();
            if (_eventLogViews.TryGetValue(eventName, out var view))
                view.Clear();
        }

        private static VisualElement BuildEventEntry(string time, string data)
        {
            var row = new VisualElement();
            row.AddToClassList("sandbox-event-entry");

            var timeLabel = new Label(time);
            timeLabel.AddToClassList("sandbox-event-time");
            row.Add(timeLabel);

            var dataLabel = new Label(data);
            dataLabel.AddToClassList("sandbox-event-data");
            row.Add(dataLabel);

            return row;
        }

        // ============ Method Card ============

        private VisualElement BuildMethodCard(object instance, SandboxMethodInfo method)
        {
            var card = new VisualElement();
            card.AddToClassList("sandbox-method-card");

            var header = new VisualElement();
            header.AddToClassList("sandbox-method-header");

            var label = new Label(method.Label);
            label.AddToClassList("sandbox-method-label");
            header.Add(label);

            if (method.IsAsync)
            {
                var badge = new Label("async");
                badge.AddToClassList("sandbox-method-badge");
                header.Add(badge);
            }

            card.Add(header);

            var paramInputs = new List<TextField>();
            if (method.Params.Count > 0)
            {
                var paramsContainer = new VisualElement();
                paramsContainer.AddToClassList("sandbox-params");

                foreach (var param in method.Params)
                {
                    var row = new VisualElement();
                    row.AddToClassList("sandbox-param-row");

                    var paramLabel = new Label(param.Name);
                    paramLabel.AddToClassList("sandbox-param-label");
                    row.Add(paramLabel);

                    var paramType = new Label(GetTypeLabel(param.Type));
                    paramType.AddToClassList("sandbox-param-type");
                    row.Add(paramType);

                    var input = CreateInputField(param.Type);
                    input.AddToClassList("sandbox-param-input");
                    row.Add(input);

                    paramInputs.Add(input);
                    paramsContainer.Add(row);
                }

                card.Add(paramsContainer);
            }

            var resultContainer = new VisualElement();
            resultContainer.style.display = DisplayStyle.None;
            var resultStatus = new Label();
            resultStatus.AddToClassList("sandbox-result-status");
            resultContainer.Add(resultStatus);
            var resultBody = new Label();
            resultBody.AddToClassList("sandbox-result-body");
            resultContainer.Add(resultBody);

            var btnRow = new VisualElement();
            btnRow.AddToClassList("sandbox-btn-row");

            var invokeBtn = new Button { text = "Execute" };
            invokeBtn.AddToClassList("sandbox-invoke-btn");

            var clearBtn = new Button { text = "Clear" };
            clearBtn.AddToClassList("sandbox-clear-btn");
            clearBtn.clicked += () =>
            {
                resultContainer.style.display = DisplayStyle.None;
                resultContainer.RemoveFromClassList("sandbox-result--success");
                resultContainer.RemoveFromClassList("sandbox-result--error");
                resultContainer.RemoveFromClassList("sandbox-result--running");
                resultStatus.text = "";
                resultBody.text = "";
            };

            invokeBtn.clicked += () => InvokeMethod(instance, method, paramInputs, invokeBtn, resultContainer, resultStatus, resultBody);

            btnRow.Add(invokeBtn);
            btnRow.Add(clearBtn);
            card.Add(btnRow);

            resultContainer.AddToClassList("sandbox-result");
            card.Add(resultContainer);

            return card;
        }

        // ============ Method Invoke ============

        private async void InvokeMethod(
            object instance, SandboxMethodInfo method, List<TextField> inputs,
            Button invokeBtn, VisualElement container, Label status, Label body)
        {
            invokeBtn.SetEnabled(false);
            container.style.display = DisplayStyle.Flex;
            var running = SandboxResult.Running();
            SetResultState(container, status, running);
            body.text = running.Body;

            var paramValues = new string[inputs.Count];
            for (var i = 0; i < inputs.Count; i++)
                paramValues[i] = inputs[i].value;

            var result = await SandboxMethodInvoker.InvokeAsync(instance, method, paramValues);
            SetResultState(container, status, result);
            body.text = result.Body;
            invokeBtn.SetEnabled(true);
        }

        private static void SetResultState(VisualElement container, Label status, SandboxResult result)
        {
            container.RemoveFromClassList("sandbox-result--success");
            container.RemoveFromClassList("sandbox-result--error");
            container.RemoveFromClassList("sandbox-result--running");
            status.RemoveFromClassList("sandbox-result-status--success");
            status.RemoveFromClassList("sandbox-result-status--error");
            status.RemoveFromClassList("sandbox-result-status--running");

            switch (result.Status)
            {
                case SandboxResultStatus.Success:
                    container.AddToClassList("sandbox-result--success");
                    status.AddToClassList("sandbox-result-status--success");
                    status.text = "SUCCESS";
                    break;
                case SandboxResultStatus.Error:
                    container.AddToClassList("sandbox-result--error");
                    status.AddToClassList("sandbox-result-status--error");
                    status.text = "ERROR";
                    break;
                case SandboxResultStatus.Running:
                    container.AddToClassList("sandbox-result--running");
                    status.AddToClassList("sandbox-result-status--running");
                    status.text = "RUNNING...";
                    break;
            }
        }

        // ============ Console ============

        private void PopulateConsoleToolbar()
        {
            if (_consoleToolbar == null) return;

            AddFilterButton(_consoleToolbar, "I", SandboxLogLevel.Info);
            AddFilterButton(_consoleToolbar, "W", SandboxLogLevel.Warning);
            AddFilterButton(_consoleToolbar, "E", SandboxLogLevel.Error);

            if (_nativeLogger != null)
                AddFilterButton(_consoleToolbar, "N", SandboxLogLevel.Native);

            _searchField = new TextField();
            _searchField.AddToClassList("sandbox-console-search");
            _searchField.RegisterValueChangedCallback(_ => RefreshConsole());
            _consoleToolbar.Add(_searchField);

            var clearBtn = new Button(() => { _console?.Clear(); _consoleScroll?.Clear(); }) { text = "Clear" };
            clearBtn.AddToClassList("sandbox-console-clear");
            _consoleToolbar.Add(clearBtn);
        }

        private void AddFilterButton(VisualElement toolbar, string label, SandboxLogLevel level)
        {
            var btn = new Button(() => ToggleFilter(level)) { text = label };
            btn.AddToClassList("sandbox-console-filter");
            btn.AddToClassList("sandbox-console-filter--active");
            _filterButtons[level] = btn;
            toolbar.Add(btn);
        }

        private void SetConsoleState(ConsoleState state) { _consoleState = state; ApplyConsoleState(); }

        private void ApplyConsoleState()
        {
            switch (_consoleState)
            {
                case ConsoleState.Minimized:
                    _methodPanel.style.flexGrow = 9;
                    _methodPanel.style.display = DisplayStyle.Flex;
                    _consolePanel.style.flexGrow = 1;
                    _consoleToolbar.style.display = DisplayStyle.None;
                    _consoleScroll.style.display = DisplayStyle.None;
                    break;
                case ConsoleState.Medium:
                    _methodPanel.style.flexGrow = 6;
                    _methodPanel.style.display = DisplayStyle.Flex;
                    _consolePanel.style.flexGrow = 4;
                    _consoleToolbar.style.display = DisplayStyle.Flex;
                    _consoleScroll.style.display = DisplayStyle.Flex;
                    break;
                case ConsoleState.Maximized:
                    _methodPanel.style.display = DisplayStyle.None;
                    _consolePanel.style.flexGrow = 1;
                    _consoleToolbar.style.display = DisplayStyle.Flex;
                    _consoleScroll.style.display = DisplayStyle.Flex;
                    break;
            }
            SetButtonActive(_minimizeBtn, _consoleState == ConsoleState.Minimized);
            SetButtonActive(_mediumBtn, _consoleState == ConsoleState.Medium);
            SetButtonActive(_maximizeBtn, _consoleState == ConsoleState.Maximized);
        }

        private static void SetButtonActive(Button btn, bool active)
        {
            if (active) btn.AddToClassList("sandbox-console-state-btn--active");
            else btn.RemoveFromClassList("sandbox-console-state-btn--active");
        }

        private void OnLogAdded(SandboxLogEntry entry)
        {
            if (!ShouldShowLog(entry)) return;
            _consoleScroll?.Add(BuildLogEntry(entry));
        }

        private void ToggleFilter(SandboxLogLevel? level)
        {
            if (!level.HasValue) return;
            if (_activeFilters.Contains(level.Value)) _activeFilters.Remove(level.Value);
            else _activeFilters.Add(level.Value);

            foreach (var kvp in _filterButtons)
            {
                if (_activeFilters.Contains(kvp.Key)) kvp.Value.AddToClassList("sandbox-console-filter--active");
                else kvp.Value.RemoveFromClassList("sandbox-console-filter--active");
            }
            RefreshConsole();
        }

        private void RefreshConsole()
        {
            if (_consoleScroll == null || _console == null) return;
            _consoleScroll.Clear();
            foreach (var entry in _console.Logs)
            {
                if (ShouldShowLog(entry))
                    _consoleScroll.Add(BuildLogEntry(entry));
            }
        }

        private bool ShouldShowLog(SandboxLogEntry entry)
        {
            if (!_activeFilters.Contains(entry.Level)) return false;
            var search = _searchField?.value;
            return string.IsNullOrEmpty(search) || entry.Message.Contains(search, StringComparison.OrdinalIgnoreCase);
        }

        private static VisualElement BuildLogEntry(SandboxLogEntry entry)
        {
            var row = new VisualElement();
            row.AddToClassList("sandbox-log-entry");

            var time = new Label(entry.Timestamp);
            time.AddToClassList("sandbox-log-time");
            row.Add(time);

            var levelIcon = entry.Level switch
            {
                SandboxLogLevel.Info => "ℹ",
                SandboxLogLevel.Warning => "⚠",
                SandboxLogLevel.Error => "✘",
                SandboxLogLevel.Native => "▸",
                _ => " "
            };
            var level = new Label(levelIcon);
            level.AddToClassList("sandbox-log-level");
            level.AddToClassList($"sandbox-log-level--{entry.Level.ToString().ToLowerInvariant()}");
            row.Add(level);

            var message = new Label(entry.Message);
            message.AddToClassList("sandbox-log-message");
            message.AddToClassList($"sandbox-log-message--{entry.Level.ToString().ToLowerInvariant()}");
            row.Add(message);

            return row;
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
    }
}
