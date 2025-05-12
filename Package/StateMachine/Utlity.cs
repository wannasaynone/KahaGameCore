namespace Assets.Scripts.StateMachine
{
    public static class Utlity
    {
        public static string ReplaceWhitespace(this string input, string replacement)
        {
            return System.Text.RegularExpressions.Regex.Replace(input, @"\s+", replacement);
        }
    }
}