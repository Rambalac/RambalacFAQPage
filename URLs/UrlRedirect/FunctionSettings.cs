namespace RambalacHome.Function
{
    public class FunctionSettings
    {
        public StorageSettings Storage { get; set; }
        public long MemoryCacheLimit { get; set; } = 1000;
        public string AzureMapsApiKey { get; set; }
    }
}