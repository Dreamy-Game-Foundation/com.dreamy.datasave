namespace Dreamy.Datasave
{
    public interface IDatasaveService
    {
        T Load<T>(string key = null) where T : SaveData, new();
        void Save<T>(T data, string key = null) where T : SaveData;
        void SaveAll();
        bool Exists(string key);
        void Delete(string key);
        void DeleteAll();
    }
}
