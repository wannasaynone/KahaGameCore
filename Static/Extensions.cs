namespace UnityEngine
{
    public static class Extensions
    {
        public static Vector3 Direction(this Transform transform, Vector3 target)
        {
            return (target - transform.position).normalized;
        }
    }
}
