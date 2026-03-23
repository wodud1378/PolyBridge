using UnityEditor;

namespace PolyBridge.Sandbox.Editor
{
    public static class SandboxEditorMenu
    {
        [MenuItem("Window/PolyBridge/Sandbox")]
        private static void OpenWindow()
        {
            SandboxEditorWindow.ShowWindow();
        }
    }
}
