namespace KahaGameCore.Combat
{
    public interface IValueContainer 
    {
        int GetTotal(string tag, bool baseOnly);
        void Add(string tag, int value);
    }
}