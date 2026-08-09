namespace KahaGameCore.Parameters
{
    public readonly struct ParameterChanged
    {
        public ParameterChanged(string key, ParameterValue oldValue, ParameterValue newValue)
        {
            Key = key;
            OldValue = oldValue;
            NewValue = newValue;
        }

        public string Key { get; }
        public ParameterValue OldValue { get; }
        public ParameterValue NewValue { get; }
    }
}
