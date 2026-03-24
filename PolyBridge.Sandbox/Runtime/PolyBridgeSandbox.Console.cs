using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace PolyBridge.Sandbox
{
    public partial class PolyBridgeSandbox
    {
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

            row.RegisterCallback<ClickEvent>(_ =>
            {
                var text = $"[{entry.Timestamp}] [{entry.Level}] {entry.Message}";
                GUIUtility.systemCopyBuffer = text;
                Debug.Log($"[PolyBridge Sandbox] Copied to clipboard.");
            });

            var time = new Label(entry.Timestamp);
            time.AddToClassList("sandbox-log-time");
            row.Add(time);

            var levelIcon = entry.Level switch
            {
                SandboxLogLevel.Info => "\u2139",
                SandboxLogLevel.Warning => "\u26A0",
                SandboxLogLevel.Error => "\u2718",
                SandboxLogLevel.Native => "\u25B8",
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
    }
}
