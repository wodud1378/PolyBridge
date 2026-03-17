namespace PolyBridge.Core.Serialization
{
    public interface IPolyBridgeSerializer
    {
        T Deserialize<T>(string data);
        string Serialize<T>(T obj);
    }
}
