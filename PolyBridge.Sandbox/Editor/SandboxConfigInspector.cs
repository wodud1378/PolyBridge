using UnityEditor;
using UnityEngine.UIElements;

namespace PolyBridge.Sandbox.Editor
{
    [CustomEditor(typeof(SandboxConfig))]
    public class SandboxConfigInspector : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            var btn = new Button { text = "Open Editor Window" };
            btn.clicked += SandboxEditorWindow.ShowWindow;
            btn.style.marginBottom = 8;
            root.Add(btn);

            var defaultInspector = new IMGUIContainer(() =>
            {
                EditorGUI.BeginDisabledGroup(true);
                DrawDefaultInspector();
                EditorGUI.EndDisabledGroup();
            });
            root.Add(defaultInspector);

            return root;
        }
    }
}
