namespace ProjectBSR.DialogueSystem.DefaultImplements.Command
{
    public static class CharacterPositionParser
    {
        /// <summary>
        /// This utility parses strings like "Left+200" into slotName and offsetX. Integrating this into ShowCharacter and ChangeCharacter to eliminate redundant logic across multiple commands.
        /// </summary>
        public static void Parse(string positionArg, out string slotName, out float offsetX)
        {
            offsetX = 0f;
            slotName = positionArg;

            if (string.IsNullOrEmpty(positionArg))
            {
                slotName = "Middle";
                return;
            }

            int plusIndex = positionArg.IndexOf('+');
            int minusIndex = positionArg.IndexOf('-');

            int operatorIndex = -1;
            if (plusIndex > 0) operatorIndex = plusIndex;
            if (minusIndex > 0 && (operatorIndex < 0 || minusIndex < operatorIndex)) operatorIndex = minusIndex;

            if (operatorIndex > 0)
            {
                slotName = positionArg.Substring(0, operatorIndex);
                string offsetStr = positionArg.Substring(operatorIndex);
                float.TryParse(offsetStr, out offsetX);
            }
        }
    }
}