namespace UnityEngine
{
    public static class Extensions
    {
        public static Vector3 DirectionTo(this Transform transform, Vector3 target)
        {
            return (target - transform.position).normalized;
        }
    }
}
