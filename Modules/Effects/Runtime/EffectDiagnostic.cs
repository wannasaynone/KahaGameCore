namespace KahaGameCore.Effects
{
    public sealed class EffectDiagnostic
    {
        public EffectDiagnostic(string code, string message, int position, int length)
        {
            Code = code;
            Message = message;
            Position = position;
            Length = length;
        }

        public string Code { get; }
        public string Message { get; }
        public int Position { get; }
        public int Length { get; }

        public override string ToString()
        {
            return $"{Code} at {Position}:{Length}: {Message}";
        }
    }
}
