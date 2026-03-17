namespace PolyBridge.Core.Serialization
{
    public static class PolyBridgeSerializerRegistry
    {
        private static IPolyBridgeSerializer _serializer;

        public static IPolyBridgeSerializer Serializer
        {
            get
            {
#if UNITY_5_3_OR_NEWER
                return _serializer ??= new JsonUtilitySerializer();
#else
                return _serializer;
#endif
            }
            set => _serializer = value;
        }
    }
}
