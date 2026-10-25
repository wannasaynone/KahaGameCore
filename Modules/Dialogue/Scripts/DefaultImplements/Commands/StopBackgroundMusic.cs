using KahaGameCore.Audio;

namespace KahaGameCore.Dialogue.DefaultImplements.Command
{
    /// <summary>Arg1: fade-out seconds (optional, default 0.5).</summary>
    public class StopBackgroundMusic : DialogueCommandBase
    {
        public override void Process(string[] args, DialogueContext context)
        {
            float fadeOutDuration = 0.5f;
            if (args.Length > 0 && !string.IsNullOrEmpty(args[0]) && float.TryParse(args[0], out float parsedDuration))
            {
                fadeOutDuration = parsedDuration;
            }

            AudioManager.StopBgm(fadeOutDuration);
            context.onComplete?.Invoke();
        }
    }

    public class StopBackgroundMusicFactory : DialogueCommandFactoryBase
    {
        public override DialogueCommandBase Create()
        {
            return new StopBackgroundMusic();
        }
    }
}
