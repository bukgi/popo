using System;
using System.Collections.Generic;
using UnityEngine;

namespace POKE.PMDCollab
{
    [Serializable]
    public class PMDDirectionFrames
    {
        public PMDDirection direction;
        public List<Sprite> frames = new List<Sprite>();
    }

    [Serializable]
    public class PMDAnimSequence
    {
        public string name;
        public int index = -1;
        public string copyOf;
        public int frameWidth;
        public int frameHeight;
        public int rushFrame = -1;
        public int hitFrame = -1;
        public int returnFrame = -1;

        [Tooltip("Duration of each frame in 1/60th of a second ticks (PMD standard).")]
        public List<int> durations = new List<int>();

        [Tooltip("8 directional rows (0 = Down, 1 = DownRight, ..., 7 = DownLeft). If 1 row, applies to all directions.")]
        public List<PMDDirectionFrames> directionRows = new List<PMDDirectionFrames>();

        public List<PMDDirectionFrames> directionFrames
        {
            get => directionRows;
            set => directionRows = value;
        }

        public List<Sprite> GetFramesForDirection(PMDDirection dir)
        {
            if (directionRows == null || directionRows.Count == 0)
                return null;

            int rowIndex = (int)dir;
            if (rowIndex >= 0 && rowIndex < directionRows.Count && directionRows[rowIndex].frames.Count > 0)
                return directionRows[rowIndex].frames;

            return directionRows[0].frames;
        }
    }

    [Serializable]
    public class PMDPortraitEntry
    {
        public string emotionName;
        public Sprite sprite;
    }

    [CreateAssetMenu(fileName = "PMDCharacterData", menuName = "POKE/PMDCollab/Character Data")]
    public class PMDCharacterData : ScriptableObject
    {
        public int dexId = 25;
        public string pokemonId = "0025";
        public string pokemonName = "Pikachu";
        public int runtimeVersion = 0;
        public bool isFullyLoaded = true;
        public string formPath = "";
        public string formFullName = "Normal";
        public int shadowSize = 1;
        public string spriteCredit = "";
        public string portraitCredit = "";
        public List<PMDAnimSequence> animations = new List<PMDAnimSequence>();
        public List<PMDPortraitEntry> portraits = new List<PMDPortraitEntry>();

        public PMDAnimSequence GetAnimation(string animName)
        {
            if (!isFullyLoaded)
            {
                OpenWorld.PMDRuntimeCharacterLoader.EnsureCharacterLoaded(this);
            }

            if (string.IsNullOrEmpty(animName))
                return null;

            for (int i = 0; i < animations.Count; i++)
            {
                if (string.Equals(animations[i].name, animName, StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrEmpty(animations[i].copyOf))
                    {
                        var target = FindRawAnimation(animations[i].copyOf);
                        if (target != null && target.directionRows.Count > 0)
                            return target;
                    }
                    return animations[i];
                }
            }
            return null;
        }

        private PMDAnimSequence FindRawAnimation(string animName)
        {
            for (int i = 0; i < animations.Count; i++)
            {
                if (string.Equals(animations[i].name, animName, StringComparison.OrdinalIgnoreCase))
                    return animations[i];
            }
            return null;
        }

        public Sprite GetPortrait(string emotion = "Normal")
        {
            if (!isFullyLoaded)
            {
                OpenWorld.PMDRuntimeCharacterLoader.EnsureCharacterLoaded(this);
            }

            for (int i = 0; i < portraits.Count; i++)
            {
                if (string.Equals(portraits[i].emotionName, emotion, StringComparison.OrdinalIgnoreCase))
                    return portraits[i].sprite;
            }
            return portraits.Count > 0 ? portraits[0].sprite : null;
        }
    }
}

