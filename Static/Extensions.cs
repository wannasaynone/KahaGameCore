namespace KahaGameCore.Static
{
    public static class Extensions
    {
        public static UnityEngine.Vector3 DirectionTo(this UnityEngine.Transform transform, UnityEngine.Vector3 target)
        {
            return (target - transform.position).normalized;
        }

        public static string RemoveBlankCharacters(this string value)
        {
            value = value.Replace(" ", "");
            value = value.Replace("\n", "");
            value = value.Replace("\t", "");

            return value;
        }

        public static int ToInt(this string value)
        {
            if (int.TryParse(value, out int _intValue))
            {
                return _intValue;
            }
            else
            {
                return 0;
            }
        }
    }
}
