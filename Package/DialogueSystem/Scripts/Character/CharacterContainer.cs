using System.Collections.Generic;

namespace KahaGameCore.Package.DialogueSystem
{
    public static class CharacterContainer
    {
        private static List<Character> characters = new List<Character>();

        public static void AddCharacter(Character character)
        {
            if (characters.Contains(character))
                return;
            characters.Add(character);
        }

        public static void RemoveCharacter(Character character)
        {
            if (!characters.Contains(character))
                return;
            characters.Remove(character);
        }

        public static Character GetClosetCharacter(Character character, float maxDistance = float.MaxValue)
        {
            Character closetCharacter = null;
            float minDistance = float.MaxValue;
            foreach (var c in characters)
            {
                if (c == character)
                    continue;

                if (c == null)
                    continue;

                float distance = UnityEngine.Vector3.Distance(character.transform.position, c.transform.position);

                if (distance > maxDistance)
                    continue;

                if (distance < minDistance)
                {
                    minDistance = distance;
                    closetCharacter = c;
                }
            }
            return closetCharacter;
        }
    }
}