#if UNITY_5_3_OR_NEWER
namespace PolyBridge.Core.Serialization
{
    public class JsonUtilitySerializer : IPolyBridgeSerializer
    {
        public T Deserialize<T>(string data) => UnityEngine.JsonUtility.FromJson<T>(data);
        public string Serialize<T>(T obj) => UnityEngine.JsonUtility.ToJson(obj);
    }
}
#endif
