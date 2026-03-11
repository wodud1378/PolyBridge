namespace PolyBridge.Generator.Models
{
    internal interface IAsyncType { }

    internal readonly struct TaskType : IAsyncType { }

    internal readonly struct UniTaskType : IAsyncType { }
}
