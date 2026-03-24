using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace PolyBridge.Sandbox
{
    public partial class PolyBridgeSandbox
    {
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

        private async void InvokeMethod(
            object instance, SandboxMethodInfo method, List<TextField> inputs,
            Button invokeBtn, VisualElement container, Label status, Label body)
        {
            invokeBtn.SetEnabled(false);
            container.style.display = DisplayStyle.Flex;
            var running = SandboxResult.Running();
            SetResultState(container, status, running);
            body.text = running.Body;

            Debug.Log($"[PolyBridge Sandbox] Invoke: {method.Label}");

            var paramValues = new string[inputs.Count];
            for (var i = 0; i < inputs.Count; i++)
                paramValues[i] = inputs[i].value;

            var result = await SandboxMethodInvoker.InvokeAsync(instance, method, paramValues);
            SetResultState(container, status, result);
            body.text = result.Body;
            invokeBtn.SetEnabled(true);

            if (result.Status == SandboxResultStatus.Error)
                Debug.LogError($"[PolyBridge Sandbox] {method.Label} -> ERROR: {result.Body}");
            else
                Debug.Log($"[PolyBridge Sandbox] {method.Label} -> {result.Status}: {result.Body}");
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
    }
}
