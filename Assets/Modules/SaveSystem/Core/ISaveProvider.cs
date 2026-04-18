namespace Modules.SaveSystem
{
    public interface ISaveProvider
    {
        void Initialize();
        bool IsReady { get; }
        void Save(string key, string json);
        string Load(string key);
        bool HasKey(string key);
    }
}
