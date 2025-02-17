using KahaGameCore.GameData;

namespace KahaGameCore.Package.DialogueSystem
{
    public class LocalizeData : IGameData
    {
        public int ID { get; private set; }
        public string en_us { get; private set; }
        public string zh_tw { get; private set; }
        public string ja_jp { get; private set; }

        public string GetLocalizedContent(Language language)
        {
            switch (language)
            {
                case Language.en_us:
                    return en_us;
                case Language.zh_tw:
                    return zh_tw;
                case Language.ja_jp:
                    return ja_jp;
                default:
                    return en_us;
            }
        }
    }
}