using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace PolyBridge.Sandbox
{
    [AddComponentMenu("PolyBridge/Sandbox")]
    public class PolyBridgeSandbox : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private PanelSettings panelSettings;

        private readonly Dictionary<Type, object> _instances = new();
        private List<SandboxServiceInfo> _services;
        private VisualElement _root;
        private VisualElement _tabBar;
        private VisualElement _content;
        private int _activeTab;

        private void OnEnable()
        {
            if (uiDocument == null)
                uiDocument = GetComponent<UIDocument>();

            if (uiDocument == null) return;

            _services = SandboxScanner.ScanAll();
            BuildUI();
        }

        private void BuildUI()
        {
            _root = uiDocument.rootVisualElement;
            _root.Clear();

            // Load UXML template
            var template = Resources.Load<VisualTreeAsset>("PolyBridgeSandbox");
            if (template != null)
                template.CloneTree(_root);
            else
                BuildFallbackStructure(_root);

            // Load stylesheet
            var styleSheet = Resources.Load<StyleSheet>("PolyBridgeSandbox");
            if (styleSheet != null)
                _root.styleSheets.Add(styleSheet);

            _tabBar = _root.Q("tab-bar");
            _content = _root.Q("content");

            if (_tabBar == null || _content == null)
            {
                BuildFallbackStructure(_root);
                _tabBar = _root.Q("tab-bar");
                _content = _root.Q("content");
            }

            if (_services.Count == 0)
            {
                _content.Add(new Label("No [Sandbox] services found."));
                return;
            }

            for (var i = 0; i < _services.Count; i++)
            {
                var index = i;
                var tab = new Button(() => SelectTab(index))
                {
                    text = _services[i].DisplayName
                };
                tab.AddToClassList("sandbox-tab");
                _tabBar.Add(tab);
            }

            SelectTab(0);
        }

        private static void BuildFallbackStructure(VisualElement root)
        {
            root.Clear();
            var container = new VisualElement();
            container.AddToClassList("sandbox-root");

            container.Add(new Label("PolyBridge Sandbox") { name = "header" });
            container.Add(new VisualElement { name = "tab-bar" });
            container.Add(new ScrollView { name = "content" });

            root.Add(container);
        }

        private void SelectTab(int index)
        {
            _activeTab = index;

            for (var i = 0; i < _tabBar.childCount; i++)
            {
                var tab = _tabBar[i];
                if (i == index)
                    tab.AddToClassList("sandbox-tab--active");
                else
                    tab.RemoveFromClassList("sandbox-tab--active");
            }

            _content.Clear();
            var service = _services[index];
            var instance = GetOrCreateInstance(service);

            foreach (var method in service.Methods)
                _content.Add(BuildMethodCard(instance, method));
        }

        private object GetOrCreateInstance(SandboxServiceInfo service)
        {
            if (_instances.TryGetValue(service.ServiceType, out var existing))
                return existing;

            var instance = Activator.CreateInstance(service.ServiceType);
            _instances[service.ServiceType] = instance;
            return instance;
        }

        private VisualElement BuildMethodCard(object instance, SandboxMethodInfo method)
        {
            var card = new VisualElement();
            card.AddToClassList("sandbox-method-card");

            var label = new Label(method.Label);
            label.AddToClassList("sandbox-method-label");
            card.Add(label);

            var paramInputs = new List<TextField>();
            foreach (var param in method.Params)
            {
                var row = new VisualElement();
                row.AddToClassList("sandbox-param-row");

                var paramLabel = new Label(param.Name);
                paramLabel.AddToClassList("sandbox-param-label");
                row.Add(paramLabel);

                var input = new TextField();
                input.value = param.DefaultValue;
                input.AddToClassList("sandbox-param-input");
                row.Add(input);

                paramInputs.Add(input);
                card.Add(row);
            }

            var resultLabel = new Label("");
            resultLabel.AddToClassList("sandbox-result");
            resultLabel.style.display = DisplayStyle.None;

            var invokeBtn = new Button(() => InvokeMethod(instance, method, paramInputs, resultLabel))
            {
                text = method.IsAsync ? $"{method.Label} (async)" : method.Label
            };
            invokeBtn.AddToClassList("sandbox-invoke-btn");
            card.Add(invokeBtn);
            card.Add(resultLabel);

            return card;
        }

        private async void InvokeMethod(object instance, SandboxMethodInfo method, List<TextField> inputs, Label resultLabel)
        {
            resultLabel.style.display = DisplayStyle.Flex;
            resultLabel.RemoveFromClassList("sandbox-result--error");
            resultLabel.AddToClassList("sandbox-result--loading");
            resultLabel.text = "Invoking...";

            var paramValues = new string[inputs.Count];
            for (var i = 0; i < inputs.Count; i++)
                paramValues[i] = inputs[i].value;

            var result = await SandboxMethodInvoker.InvokeAsync(instance, method, paramValues);

            resultLabel.RemoveFromClassList("sandbox-result--loading");
            if (result.StartsWith("ERROR:"))
                resultLabel.AddToClassList("sandbox-result--error");
            resultLabel.text = result;
        }
    }
}
