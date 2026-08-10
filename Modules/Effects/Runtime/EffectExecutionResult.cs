using System;

namespace KahaGameCore.Effects
{
    public enum EffectExecutionStatus
    {
        Succeeded,
        Failed,
        Cancelled
    }

    public sealed class EffectExecutionResult
    {
        private EffectExecutionResult(EffectExecutionStatus status, EffectDiagnostic diagnostic)
        {
            if (status != EffectExecutionStatus.Succeeded && diagnostic == null)
            {
                throw new ArgumentNullException(nameof(diagnostic));
            }

            Status = status;
            Diagnostic = diagnostic;
        }

        public EffectExecutionStatus Status { get; }
        public EffectDiagnostic Diagnostic { get; }
        public bool IsSuccess => Status == EffectExecutionStatus.Succeeded;

        public static EffectExecutionResult Succeeded()
        {
            return new EffectExecutionResult(EffectExecutionStatus.Succeeded, null);
        }

        public static EffectExecutionResult Failed(EffectDiagnostic diagnostic)
        {
            return new EffectExecutionResult(EffectExecutionStatus.Failed, diagnostic);
        }

        public static EffectExecutionResult Cancelled(EffectDiagnostic diagnostic)
        {
            return new EffectExecutionResult(EffectExecutionStatus.Cancelled, diagnostic);
        }

        public string FormatDiagnostic()
        {
            return Diagnostic == null ? string.Empty : Diagnostic.ToString();
        }
    }
}
