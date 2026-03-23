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

        private readonly Dictionary<Type, object> _instances = new();
        private List<SandboxServiceInfo> _services;
        private VisualElement _tabBar;
        private ScrollView _scroll;
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
            var root = uiDocument.rootVisualElement;
            root.Clear();

            var styleSheet = Resources.Load<StyleSheet>("PolyBridgeSandbox");
            if (styleSheet != null)
                root.styleSheets.Add(styleSheet);

            var container = new VisualElement();
            container.AddToClassList("sandbox-root");

            // Tab bar
            _tabBar = new VisualElement();
            _tabBar.AddToClassList("sandbox-tab-bar");
            container.Add(_tabBar);

            // Scroll content
            _scroll = new ScrollView(ScrollViewMode.Vertical);
            _scroll.AddToClassList("sandbox-scroll");
            container.Add(_scroll);

            if (_services.Count == 0)
            {
                var emptyLabel = new Label("No [Sandbox] services found.");
                emptyLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
                emptyLabel.style.paddingTop = 16;
                emptyLabel.style.paddingLeft = 16;
                _scroll.Add(emptyLabel);
                root.Add(container);
                return;
            }

            for (var i = 0; i < _services.Count; i++)
            {
                var index = i;
                var tab = new Button(() => SelectTab(index)) { text = _services[i].DisplayName };
                tab.AddToClassList("sandbox-tab");
                _tabBar.Add(tab);
            }

            root.Add(container);
            SelectTab(0);
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

            _scroll.Clear();
            var service = _services[index];
            var instance = GetOrCreateInstance(service);

            foreach (var method in service.Methods)
                _scroll.Add(BuildMethodCard(instance, method));
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

            // Header
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

            // Params
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

            // Result
            var resultContainer = new VisualElement();
            resultContainer.style.display = DisplayStyle.None;

            var resultStatus = new Label();
            resultStatus.AddToClassList("sandbox-result-status");
            resultContainer.Add(resultStatus);

            var resultBody = new Label();
            resultBody.AddToClassList("sandbox-result-body");
            resultContainer.Add(resultBody);

            // Buttons
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

        private async void InvokeMethod(
            object instance, SandboxMethodInfo method, List<TextField> inputs,
            Button invokeBtn, VisualElement container, Label status, Label body)
        {
            // Show running state
            invokeBtn.SetEnabled(false);
            container.style.display = DisplayStyle.Flex;
            var running = SandboxResult.Running();
            SetResultState(container, status, running);
            body.text = running.Body;

            // Collect params
            var paramValues = new string[inputs.Count];
            for (var i = 0; i < inputs.Count; i++)
                paramValues[i] = GetInputValue(inputs[i]);

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

        private static string GetInputValue(VisualElement input)
        {
            return input is TextField tf ? tf.value : "";
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
