using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using UnityEngine;
using UnityEngine.UIElements;

namespace PolyBridge.Sandbox
{
    public partial class PolyBridgeSandbox
    {
        private void SubscribeEvents(object instance, SandboxServiceInfo service)
        {
            if (service.BridgeProperty == null) return;

            try
            {
                var bridge = service.BridgeProperty.GetValue(instance);
                if (bridge == null)
                {
                    Debug.LogWarning($"[PolyBridge Sandbox] Bridge is null for {service.DisplayName}");
                    return;
                }

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
                        Debug.Log($"[PolyBridge Sandbox] Subscribed event: {evtName}");
                    }
                    else
                    {
                        Debug.LogWarning($"[PolyBridge Sandbox] Failed to create handler for event: {evtName}");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[PolyBridge Sandbox] Event subscription failed for {service.DisplayName}: {e}");
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
    }
}
