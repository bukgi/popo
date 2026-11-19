using System;
using System.Collections.Generic;
using UnityEngine;

namespace POKE.PMDCollab.OpenWorld
{
    public enum PMDElement
    {
        Normal,
        Fire,
        Water,
        Grass,
        Electric,
        Ice,
        Fighting,
        Poison,
        Ground,
        Flying,
        Psychic,
        Bug,
        Rock,
        Ghost,
        Dragon,
        Dark,
        Steel,
        Fairy
    }

    /// <summary>
    /// Bảng hệ khắc chế đầy đủ 18 hệ chuẩn Pokémon cho toàn bộ 84 loài Pokémon trong dự án.
    /// Tự động chuẩn hóa tên thư mục có mã số (vd: "0025_Pikachu" -> "Pikachu", "Ho-Oh" -> "Ho_Oh").
    /// </summary>
    public static class PMDTypeChart
    {
        private static readonly Dictionary<string, PMDElement[]> SpeciesTypes =
            new Dictionary<string, PMDElement[]>(StringComparer.OrdinalIgnoreCase)
            {
                // Electric
                { "Pikachu", new[] { PMDElement.Electric } },
                { "Raichu", new[] { PMDElement.Electric } },
                { "Jolteon", new[] { PMDElement.Electric } },
                { "Zapdos", new[] { PMDElement.Electric, PMDElement.Flying } },
                { "Ampharos", new[] { PMDElement.Electric } },
                { "Luxray", new[] { PMDElement.Electric } },

                // Grass
                { "Bulbasaur", new[] { PMDElement.Grass, PMDElement.Poison } },
                { "Ivysaur", new[] { PMDElement.Grass, PMDElement.Poison } },
                { "Venusaur", new[] { PMDElement.Grass, PMDElement.Poison } },
                { "Chikorita", new[] { PMDElement.Grass } },
                { "Celebi", new[] { PMDElement.Psychic, PMDElement.Grass } },
                { "Treecko", new[] { PMDElement.Grass } },
                { "Sceptile", new[] { PMDElement.Grass } },
                { "Turtwig", new[] { PMDElement.Grass } },
                { "Torterra", new[] { PMDElement.Grass, PMDElement.Ground } },
                { "Leafeon", new[] { PMDElement.Grass } },
                { "Decidueye", new[] { PMDElement.Grass, PMDElement.Ghost } },

                // Fire
                { "Charmander", new[] { PMDElement.Fire } },
                { "Charmeleon", new[] { PMDElement.Fire } },
                { "Charizard", new[] { PMDElement.Fire, PMDElement.Flying } },
                { "Ninetales", new[] { PMDElement.Fire } },
                { "Arcanine", new[] { PMDElement.Fire } },
                { "Flareon", new[] { PMDElement.Fire } },
                { "Moltres", new[] { PMDElement.Fire, PMDElement.Flying } },
                { "Cyndaquil", new[] { PMDElement.Fire } },
                { "Typhlosion", new[] { PMDElement.Fire } },
                { "Ho_Oh", new[] { PMDElement.Fire, PMDElement.Flying } },
                { "Torchic", new[] { PMDElement.Fire } },
                { "Blaziken", new[] { PMDElement.Fire, PMDElement.Fighting } },
                { "Chimchar", new[] { PMDElement.Fire } },
                { "Infernape", new[] { PMDElement.Fire, PMDElement.Fighting } },
                { "Chandelure", new[] { PMDElement.Ghost, PMDElement.Fire } },

                // Water
                { "Squirtle", new[] { PMDElement.Water } },
                { "Wartortle", new[] { PMDElement.Water } },
                { "Blastoise", new[] { PMDElement.Water } },
                { "Gyarados", new[] { PMDElement.Water, PMDElement.Flying } },
                { "Lapras", new[] { PMDElement.Water, PMDElement.Ice } },
                { "Vaporeon", new[] { PMDElement.Water } },
                { "Totodile", new[] { PMDElement.Water } },
                { "Feraligatr", new[] { PMDElement.Water } },
                { "Mudkip", new[] { PMDElement.Water } },
                { "Swampert", new[] { PMDElement.Water, PMDElement.Ground } },
                { "Milotic", new[] { PMDElement.Water } },
                { "Kyogre", new[] { PMDElement.Water } },
                { "Piplup", new[] { PMDElement.Water } },
                { "Empoleon", new[] { PMDElement.Water, PMDElement.Steel } },
                { "Palkia", new[] { PMDElement.Water, PMDElement.Dragon } },
                { "Greninja", new[] { PMDElement.Water, PMDElement.Dark } },

                // Ice
                { "Articuno", new[] { PMDElement.Ice, PMDElement.Flying } },
                { "Glaceon", new[] { PMDElement.Ice } },

                // Psychic
                { "Alakazam", new[] { PMDElement.Psychic } },
                { "Mewtwo", new[] { PMDElement.Psychic } },
                { "Mew", new[] { PMDElement.Psychic } },
                { "Espeon", new[] { PMDElement.Psychic } },
                { "Lugia", new[] { PMDElement.Psychic, PMDElement.Flying } },
                { "Gardevoir", new[] { PMDElement.Psychic, PMDElement.Fairy } },
                { "Metagross", new[] { PMDElement.Steel, PMDElement.Psychic } },
                { "Jirachi", new[] { PMDElement.Steel, PMDElement.Psychic } },

                // Ghost & Dark & Fairy
                { "Gengar", new[] { PMDElement.Ghost, PMDElement.Poison } },
                { "Giratina", new[] { PMDElement.Ghost, PMDElement.Dragon } },
                { "Mimikyu", new[] { PMDElement.Ghost, PMDElement.Fairy } },
                { "Dragapult", new[] { PMDElement.Dragon, PMDElement.Ghost } },
                { "Umbreon", new[] { PMDElement.Dark } },
                { "Absol", new[] { PMDElement.Dark } },
                { "Darkrai", new[] { PMDElement.Dark } },
                { "Zoroark", new[] { PMDElement.Dark } },
                { "Hydreigon", new[] { PMDElement.Dark, PMDElement.Dragon } },
                { "Sylveon", new[] { PMDElement.Fairy } },

                // Fighting, Ground, Rock, Bug, Steel
                { "Machamp", new[] { PMDElement.Fighting } },
                { "Riolu", new[] { PMDElement.Fighting } },
                { "Lucario", new[] { PMDElement.Fighting, PMDElement.Steel } },
                { "Groudon", new[] { PMDElement.Ground } },
                { "Scizor", new[] { PMDElement.Bug, PMDElement.Steel } },
                { "Tyranitar", new[] { PMDElement.Rock, PMDElement.Dark } },
                { "Aggron", new[] { PMDElement.Steel, PMDElement.Rock } },

                // Dragon
                { "Dragonite", new[] { PMDElement.Dragon, PMDElement.Flying } },
                { "Flygon", new[] { PMDElement.Ground, PMDElement.Dragon } },
                { "Salamence", new[] { PMDElement.Dragon, PMDElement.Flying } },
                { "Rayquaza", new[] { PMDElement.Dragon, PMDElement.Flying } },
                { "Garchomp", new[] { PMDElement.Dragon, PMDElement.Ground } },
                { "Dialga", new[] { PMDElement.Steel, PMDElement.Dragon } },

                // Normal
                { "Eevee", new[] { PMDElement.Normal } },
                { "Snorlax", new[] { PMDElement.Normal } },
                { "Arceus", new[] { PMDElement.Normal } }
            };

        private static readonly (Color color, PMDElement element)[] SkillPalette =
        {
            (new Color(1f, 0.88f, 0.22f), PMDElement.Electric),
            (new Color(0.35f, 0.92f, 0.42f), PMDElement.Grass),
            (new Color(1f, 0.45f, 0.2f), PMDElement.Fire),
            (new Color(0.3f, 0.78f, 1f), PMDElement.Water),
            (new Color(0.95f, 0.4f, 0.35f), PMDElement.Fighting),
            (new Color(0.95f, 0.48f, 0.92f), PMDElement.Psychic),
            (new Color(0.55f, 0.92f, 1f), PMDElement.Ice),
            (new Color(0.92f, 0.88f, 0.78f), PMDElement.Normal)
        };

        public static string NormalizeSpeciesKey(string speciesName)
        {
            if (string.IsNullOrEmpty(speciesName))
                return string.Empty;

            string s = speciesName.Trim();
            int us = s.IndexOf('_');
            if (us > 0 && us < s.Length - 1)
            {
                bool allDigits = true;
                for (int i = 0; i < us; i++)
                {
                    if (!char.IsDigit(s[i])) { allDigits = false; break; }
                }
                if (allDigits)
                    s = s.Substring(us + 1);
            }
            return s.Replace('-', '_');
        }

        public static PMDElement[] GetSpeciesTypes(string speciesName)
        {
            string key = NormalizeSpeciesKey(speciesName);
            if (!string.IsNullOrEmpty(key) && SpeciesTypes.TryGetValue(key, out var types))
                return types;
            return new[] { PMDElement.Normal };
        }

        public static PMDElement FromTypeName(string moveType)
        {
            if (!string.IsNullOrEmpty(moveType) && Enum.TryParse(moveType.Trim(), true, out PMDElement parsed))
                return parsed;
            return PMDElement.Normal;
        }

        public static PMDElement FromColor(Color c)
        {
            PMDElement best = PMDElement.Normal;
            float bestDist = float.MaxValue;
            for (int i = 0; i < SkillPalette.Length; i++)
            {
                Color p = SkillPalette[i].color;
                float d = (p.r - c.r) * (p.r - c.r) + (p.g - c.g) * (p.g - c.g) + (p.b - c.b) * (p.b - c.b);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = SkillPalette[i].element;
                }
            }
            return best;
        }

        public static float GetMultiplier(PMDElement attack, PMDElement[] defenderTypes)
        {
            float mult = 1f;
            if (defenderTypes == null)
                return mult;
            for (int i = 0; i < defenderTypes.Length; i++)
                mult *= GetSingle(attack, defenderTypes[i]);
            return mult;
        }

        private static float GetSingle(PMDElement a, PMDElement d)
        {
            switch (a)
            {
                case PMDElement.Normal:
                    if (d == PMDElement.Ghost) return 0.25f;
                    if (d == PMDElement.Rock || d == PMDElement.Steel) return 0.5f;
                    break;
                case PMDElement.Fire:
                    if (d == PMDElement.Grass || d == PMDElement.Ice || d == PMDElement.Bug || d == PMDElement.Steel) return 2f;
                    if (d == PMDElement.Fire || d == PMDElement.Water || d == PMDElement.Rock || d == PMDElement.Dragon) return 0.5f;
                    break;
                case PMDElement.Water:
                    if (d == PMDElement.Fire || d == PMDElement.Ground || d == PMDElement.Rock) return 2f;
                    if (d == PMDElement.Water || d == PMDElement.Grass || d == PMDElement.Dragon) return 0.5f;
                    break;
                case PMDElement.Grass:
                    if (d == PMDElement.Water || d == PMDElement.Ground || d == PMDElement.Rock) return 2f;
                    if (d == PMDElement.Fire || d == PMDElement.Grass || d == PMDElement.Poison || d == PMDElement.Flying || d == PMDElement.Bug || d == PMDElement.Dragon || d == PMDElement.Steel) return 0.5f;
                    break;
                case PMDElement.Electric:
                    if (d == PMDElement.Ground) return 0.25f;
                    if (d == PMDElement.Water || d == PMDElement.Flying) return 2f;
                    if (d == PMDElement.Electric || d == PMDElement.Grass || d == PMDElement.Dragon) return 0.5f;
                    break;
                case PMDElement.Ice:
                    if (d == PMDElement.Grass || d == PMDElement.Ground || d == PMDElement.Flying || d == PMDElement.Dragon) return 2f;
                    if (d == PMDElement.Fire || d == PMDElement.Water || d == PMDElement.Ice || d == PMDElement.Steel) return 0.5f;
                    break;
                case PMDElement.Fighting:
                    if (d == PMDElement.Ghost) return 0.25f;
                    if (d == PMDElement.Normal || d == PMDElement.Ice || d == PMDElement.Rock || d == PMDElement.Dark || d == PMDElement.Steel) return 2f;
                    if (d == PMDElement.Poison || d == PMDElement.Flying || d == PMDElement.Psychic || d == PMDElement.Bug || d == PMDElement.Fairy) return 0.5f;
                    break;
                case PMDElement.Poison:
                    if (d == PMDElement.Steel) return 0.25f;
                    if (d == PMDElement.Grass || d == PMDElement.Fairy) return 2f;
                    if (d == PMDElement.Poison || d == PMDElement.Ground || d == PMDElement.Rock || d == PMDElement.Ghost) return 0.5f;
                    break;
                case PMDElement.Ground:
                    if (d == PMDElement.Flying) return 0.25f;
                    if (d == PMDElement.Fire || d == PMDElement.Electric || d == PMDElement.Poison || d == PMDElement.Rock || d == PMDElement.Steel) return 2f;
                    if (d == PMDElement.Grass || d == PMDElement.Bug) return 0.5f;
                    break;
                case PMDElement.Flying:
                    if (d == PMDElement.Grass || d == PMDElement.Fighting || d == PMDElement.Bug) return 2f;
                    if (d == PMDElement.Electric || d == PMDElement.Rock || d == PMDElement.Steel) return 0.5f;
                    break;
                case PMDElement.Psychic:
                    if (d == PMDElement.Dark) return 0.25f;
                    if (d == PMDElement.Fighting || d == PMDElement.Poison) return 2f;
                    if (d == PMDElement.Psychic || d == PMDElement.Steel) return 0.5f;
                    break;
                case PMDElement.Bug:
                    if (d == PMDElement.Grass || d == PMDElement.Psychic || d == PMDElement.Dark) return 2f;
                    if (d == PMDElement.Fire || d == PMDElement.Fighting || d == PMDElement.Poison || d == PMDElement.Flying || d == PMDElement.Ghost || d == PMDElement.Steel || d == PMDElement.Fairy) return 0.5f;
                    break;
                case PMDElement.Rock:
                    if (d == PMDElement.Fire || d == PMDElement.Ice || d == PMDElement.Flying || d == PMDElement.Bug) return 2f;
                    if (d == PMDElement.Fighting || d == PMDElement.Ground || d == PMDElement.Steel) return 0.5f;
                    break;
                case PMDElement.Ghost:
                    if (d == PMDElement.Normal) return 0.25f;
                    if (d == PMDElement.Psychic || d == PMDElement.Ghost) return 2f;
                    if (d == PMDElement.Dark) return 0.5f;
                    break;
                case PMDElement.Dragon:
                    if (d == PMDElement.Fairy) return 0.25f;
                    if (d == PMDElement.Dragon) return 2f;
                    if (d == PMDElement.Steel) return 0.5f;
                    break;
                case PMDElement.Dark:
                    if (d == PMDElement.Psychic || d == PMDElement.Ghost) return 2f;
                    if (d == PMDElement.Fighting || d == PMDElement.Dark || d == PMDElement.Fairy) return 0.5f;
                    break;
                case PMDElement.Steel:
                    if (d == PMDElement.Ice || d == PMDElement.Rock || d == PMDElement.Fairy) return 2f;
                    if (d == PMDElement.Fire || d == PMDElement.Water || d == PMDElement.Electric || d == PMDElement.Steel) return 0.5f;
                    break;
                case PMDElement.Fairy:
                    if (d == PMDElement.Fighting || d == PMDElement.Dragon || d == PMDElement.Dark) return 2f;
                    if (d == PMDElement.Fire || d == PMDElement.Poison || d == PMDElement.Steel) return 0.5f;
                    break;
            }
            return 1f;
        }

        public static string ElementName(PMDElement e)
        {
            switch (e)
            {
                case PMDElement.Fire: return "Lửa";
                case PMDElement.Water: return "Nước";
                case PMDElement.Grass: return "Cỏ";
                case PMDElement.Electric: return "Điện";
                case PMDElement.Ice: return "Băng";
                case PMDElement.Fighting: return "Giác Đấu";
                case PMDElement.Poison: return "Độc";
                case PMDElement.Ground: return "Đất";
                case PMDElement.Flying: return "Bay";
                case PMDElement.Psychic: return "Siêu Linh";
                case PMDElement.Bug: return "Côn Trùng";
                case PMDElement.Rock: return "Đá";
                case PMDElement.Ghost: return "Ma";
                case PMDElement.Dragon: return "Rồng";
                case PMDElement.Dark: return "Bóng Tối";
                case PMDElement.Steel: return "Thép";
                case PMDElement.Fairy: return "Tiên";
                default: return "Thường";
            }
        }

        public static string TypesLabel(string speciesName)
        {
            var types = GetSpeciesTypes(speciesName);
            string s = "";
            for (int i = 0; i < types.Length; i++)
                s += (i > 0 ? "/" : "") + ElementName(types[i]);
            return s;
        }
    }
}
