namespace KahaGameCore.Combat
{
    public interface IValueContainer 
    {
        int GetTotal(string tag, bool baseOnly);
        System.Guid Add(string tag, int value);
        void AddToTemp(System.Guid guid, int value);
        void SetTemp(System.Guid guid, int value);
        void AddBase(string tag, int value);
        void SetBase(string tag, int value);
    }
}