#if UNITY_ANDROID
using UnityEngine;

namespace PolyBridge.Core.Runtime
{
    internal class AndroidBridge
    {
        private readonly AndroidJavaObject _javaObject;

        public AndroidBridge(string classPath)
        {
            _javaObject = new AndroidJavaObject(classPath);
        }

        public void Call(string method, params object[] args)
            => _javaObject.Call(method, args);

        public T Call<T>(string method, params object[] args)
            => _javaObject.Call<T>(method, args);
    }
}
#endif
