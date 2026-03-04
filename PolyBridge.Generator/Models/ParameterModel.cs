namespace PolyBridge.Generator.Models
{
    internal record ParameterModel(string Type, string Name)
    {
        public string Type { get; } = Type;
        public string Name { get; } = Name;
    }
}