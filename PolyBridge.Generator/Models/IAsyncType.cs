namespace PolyBridge.Generator.Models
{
    internal interface IAsyncType
    {
        string RunMethod { get; }
    }

    internal readonly struct TaskType : IAsyncType
    {
        public string RunMethod => "System.Threading.Tasks.Task.Run";
    }

    internal readonly struct UniTaskType : IAsyncType
    {
        public string RunMethod => "Cysharp.Threading.Tasks.UniTask.RunOnThreadPool";
    }
}
