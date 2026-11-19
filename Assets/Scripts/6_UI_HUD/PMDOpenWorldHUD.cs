using UnityEngine;

namespace POKE.PMDCollab.OpenWorld
{
    /// <summary>
    /// 100% Retro Pixel-Art HUD (Pokémon Mystery Dungeon GBA/NDS Style):
    /// - Uses procedurally generated FilterMode.Point 9-slice pixel-art window frames (stepped pixel corners,
    ///   2px gold/cyan pixel inner borders, corner rivets, and 2x2 dithered dark-navy pixel backgrounds).
    /// - Pixel-art beveled buttons (Normal, Hover, Active, Accent) with 2px pixel highlight/shadow bevels.
    /// - Segmented pixel-art HP, EXP, Monster HP, and Cooldown bars with 2px black pixel borders & top shine.
    /// - Crisp 1px pixel-shadowed text rendering (DrawPixelLabel) across all HUD panels and modals.
    /// - Pixel-art Mouse Aim Crosshair reticle in world space.
    /// </summary>
    public class PMDOpenWorldHUD : MonoBehaviour
    {
        private PMDOpenWorldManager _manager;

        // Procedural Pixel-Art Textures (FilterMode.Point)
        private Texture2D _pixelWindowTex;
        private Texture2D _pixelSubPanelTex;
        private Texture2D _pixelPortraitFrameTex;
        private Texture2D _pixelBtnNormalTex;
        private Texture2D _pixelBtnHoverTex;
        private Texture2D _pixelBtnActiveTex;
        private Texture2D _pixelAccentNormalTex;
        private Texture2D _pixelAccentHoverTex;
        private Texture2D _pixelTagBgTex;
        private Texture2D _pixelCrosshairTex;

        private GUIStyle _boxStyle;
        private GUIStyle _subBoxStyle;
        private GUIStyle _portraitBoxStyle;
        private GUIStyle _tagBoxStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _accentButtonStyle;
        private GUIStyle _worldTagStyle;
        private GUIStyle _gatewayTagStyle;
        private GUIStyle _floatingTextStyle;
        private GUIStyle _bannerStyle;

        private Vector2 _mapScrollPos;
        private bool _showMapTeleportList = false;
        private string _pokeSearchFilter = "";

        private void Awake()
        {
            _manager = GetComponent<PMDOpenWorldManager>();
            if (_manager == null)
                _manager = FindFirstObjectByType<PMDOpenWorldManager>();
        }

        private void EnsureStyles()
        {
            if (_boxStyle != null && _pixelWindowTex != null)
                return;

            // 1. Build Crisp Pixel-Art 9-Slice Textures (FilterMode.Point)
            _pixelWindowTex = CreatePixelWindowTexture(
                24, 24,
                new Color32(8, 12, 22, 255),      // Outer black-navy 2px border
                new Color32(72, 200, 248, 255),   // Cyan PMD outer frame
                new Color32(248, 216, 88, 255),   // Gold PMD inner frame + corner studs
                new Color32(16, 26, 46, 242),     // Dither A
                new Color32(22, 34, 58, 242));    // Dither B

            _pixelSubPanelTex = CreatePixelWindowTexture(
                16, 16,
                new Color32(8, 12, 20, 255),
                new Color32(52, 118, 168, 255),
                new Color32(34, 74, 112, 255),
                new Color32(12, 20, 36, 245),
                new Color32(16, 26, 44, 245));

            _pixelPortraitFrameTex = CreatePixelWindowTexture(
                16, 16,
                new Color32(8, 10, 18, 255),
                new Color32(248, 208, 72, 255),
                new Color32(184, 128, 32, 255),
                new Color32(10, 14, 24, 255),
                new Color32(14, 20, 32, 255));

            _pixelBtnNormalTex = CreatePixelButtonTexture(
                16, 16,
                new Color32(8, 12, 20, 255),
                new Color32(90, 155, 215, 255),   // Top/Left pixel bevel highlight
                new Color32(24, 42, 72, 255),     // Bottom/Right pixel bevel shadow
                new Color32(32, 58, 96, 255),
                new Color32(38, 66, 108, 255));

            _pixelBtnHoverTex = CreatePixelButtonTexture(
                16, 16,
                new Color32(8, 12, 20, 255),
                new Color32(248, 224, 112, 255),
                new Color32(42, 74, 118, 255),
                new Color32(52, 92, 148, 255),
                new Color32(60, 104, 164, 255));

            _pixelBtnActiveTex = CreatePixelButtonTexture(
                16, 16,
                new Color32(8, 12, 20, 255),
                new Color32(20, 36, 62, 255),
                new Color32(80, 140, 195, 255),
                new Color32(24, 44, 74, 255),
                new Color32(28, 50, 82, 255));

            _pixelAccentNormalTex = CreatePixelButtonTexture(
                16, 16,
                new Color32(8, 14, 12, 255),
                new Color32(120, 245, 155, 255),
                new Color32(22, 78, 42, 255),
                new Color32(34, 118, 64, 255),
                new Color32(42, 132, 72, 255));

            _pixelAccentHoverTex = CreatePixelButtonTexture(
                16, 16,
                new Color32(8, 14, 12, 255),
                new Color32(255, 240, 120, 255),
                new Color32(32, 105, 56, 255),
                new Color32(48, 152, 84, 255),
                new Color32(58, 168, 96, 255));

            _pixelTagBgTex = CreatePixelButtonTexture(
                12, 12,
                new Color32(6, 10, 18, 240),
                new Color32(64, 168, 224, 240),
                new Color32(18, 38, 64, 240),
                new Color32(12, 22, 38, 230),
                new Color32(16, 28, 48, 230));

            _pixelCrosshairTex = CreatePixelCrosshairTexture();

            _boxStyle = new GUIStyle
            {
                normal = { background = _pixelWindowTex },
                border = new RectOffset(6, 6, 6, 6),
                padding = new RectOffset(10, 10, 10, 10)
            };

            _subBoxStyle = new GUIStyle
            {
                normal = { background = _pixelSubPanelTex },
                border = new RectOffset(5, 5, 5, 5),
                padding = new RectOffset(6, 6, 6, 6)
            };

            _portraitBoxStyle = new GUIStyle
            {
                normal = { background = _pixelPortraitFrameTex },
                border = new RectOffset(5, 5, 5, 5)
            };

            _tagBoxStyle = new GUIStyle
            {
                normal = { background = _pixelTagBgTex },
                border = new RectOffset(4, 4, 4, 4)
            };

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.92f, 0.38f) }
            };

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                normal = { textColor = new Color(0.92f, 0.96f, 1f) }
            };

            _buttonStyle = new GUIStyle
            {
                normal = { background = _pixelBtnNormalTex, textColor = new Color(0.95f, 0.98f, 1f) },
                hover = { background = _pixelBtnHoverTex, textColor = new Color(1f, 0.95f, 0.45f) },
                active = { background = _pixelBtnActiveTex, textColor = new Color(0.8f, 0.9f, 1f) },
                border = new RectOffset(4, 4, 4, 4),
                padding = new RectOffset(4, 4, 2, 2),
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            _accentButtonStyle = new GUIStyle
            {
                normal = { background = _pixelAccentNormalTex, textColor = new Color(1f, 0.97f, 0.55f) },
                hover = { background = _pixelAccentHoverTex, textColor = Color.white },
                active = { background = _pixelBtnActiveTex, textColor = new Color(0.9f, 1f, 0.9f) },
                border = new RectOffset(4, 4, 4, 4),
                padding = new RectOffset(4, 4, 2, 2),
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            _worldTagStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            _gatewayTagStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.48f, 0.98f, 1f) }
            };

            _floatingTextStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            _bannerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 0.95f, 0.45f) }
            };
        }

        private static Texture2D CreatePixelWindowTexture(
            int w, int h,
            Color32 outerBorder,
            Color32 midFrame,
            Color32 innerTrim,
            Color32 bgDarkA,
            Color32 bgDarkB)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            Color32 clear = new Color32(0, 0, 0, 0);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Stepped notched pixel corners (2x2 corner cutout)
                    bool isCornerCut =
                        (x == 0 && y == 0) || (x == w - 1 && y == 0) ||
                        (x == 0 && y == h - 1) || (x == w - 1 && y == h - 1);
                    if (isCornerCut)
                    {
                        tex.SetPixel(x, y, clear);
                        continue;
                    }

                    int distX = Mathf.Min(x, w - 1 - x);
                    int distY = Mathf.Min(y, h - 1 - y);
                    int minDist = Mathf.Min(distX, distY);

                    if (minDist == 0 || (distX == 1 && distY == 1))
                    {
                        tex.SetPixel(x, y, outerBorder);
                    }
                    else if (minDist == 1)
                    {
                        tex.SetPixel(x, y, midFrame);
                    }
                    else if (minDist == 2)
                    {
                        tex.SetPixel(x, y, innerTrim);
                    }
                    else if (minDist == 3)
                    {
                        tex.SetPixel(x, y, outerBorder);
                    }
                    else
                    {
                        // Pixel corner studs (4,4)
                        if ((distX == 4 && distY == 4))
                        {
                            tex.SetPixel(x, y, innerTrim);
                        }
                        else
                        {
                            // 2x2 pixel checkerboard dither
                            bool dither = (((x / 2) + (y / 2)) % 2) == 0;
                            tex.SetPixel(x, y, dither ? bgDarkA : bgDarkB);
                        }
                    }
                }
            }

            tex.Apply(false);
            return tex;
        }

        private static Texture2D CreatePixelButtonTexture(
            int w, int h,
            Color32 borderCol,
            Color32 bevelLight,
            Color32 bevelDark,
            Color32 fillA,
            Color32 fillB)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            Color32 clear = new Color32(0, 0, 0, 0);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if ((x == 0 && y == 0) || (x == w - 1 && y == 0) ||
                        (x == 0 && y == h - 1) || (x == w - 1 && y == h - 1))
                    {
                        tex.SetPixel(x, y, clear);
                        continue;
                    }

                    if (x == 0 || x == w - 1 || y == 0 || y == h - 1)
                    {
                        tex.SetPixel(x, y, borderCol);
                    }
                    else if (y == h - 2 || x == 1)
                    {
                        tex.SetPixel(x, y, bevelLight);
                    }
                    else if (y == 1 || x == w - 2)
                    {
                        tex.SetPixel(x, y, bevelDark);
                    }
                    else
                    {
                        bool dither = (((x / 2) + (y / 2)) % 2) == 0;
                        tex.SetPixel(x, y, dither ? fillA : fillB);
                    }
                }
            }

            tex.Apply(false);
            return tex;
        }

        private static Texture2D CreatePixelCrosshairTexture()
        {
            const int s = 13;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            Color32 clear = new Color32(0, 0, 0, 0);
            Color32 black = new Color32(10, 12, 18, 240);
            Color32 white = new Color32(255, 255, 255, 255);

            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                    tex.SetPixel(x, y, clear);

            int c = s / 2;
            // Center pixel & 4 pixel arms with black 1px outline
            int[,] pts =
            {
                { c, c },
                { c - 3, c }, { c - 4, c },
                { c + 3, c }, { c + 4, c },
                { c, c - 3 }, { c, c - 4 },
                { c, c + 3 }, { c, c + 4 }
            };

            for (int i = 0; i < pts.GetLength(0); i++)
            {
                int px = pts[i, 0];
                int py = pts[i, 1];
                for (int oy = -1; oy <= 1; oy++)
                    for (int ox = -1; ox <= 1; ox++)
                        if (px + ox >= 0 && px + ox < s && py + oy >= 0 && py + oy < s)
                            tex.SetPixel(px + ox, py + oy, black);
            }

            for (int i = 0; i < pts.GetLength(0); i++)
            {
                tex.SetPixel(pts[i, 0], pts[i, 1], white);
            }

            tex.Apply(false);
            return tex;
        }

        private void DrawPixelLabel(Rect rect, string text, GUIStyle style, Color textColor)
        {
            Color prev = style.normal.textColor;
            // 1px crisp pixel drop shadow
            style.normal.textColor = new Color(0.03f, 0.05f, 0.09f, textColor.a);
            GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), text, style);
            style.normal.textColor = textColor;
            GUI.Label(rect, text, style);
            style.normal.textColor = prev;
        }

        private void DrawPixelBar(Rect rect, float ratio, Color fillCol, Color bgCol, int segments = 14)
        {
            ratio = Mathf.Clamp01(ratio);
            // Outer 2px black pixel border
            GUI.color = new Color(0.04f, 0.06f, 0.10f, 1f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);

            // Inner trough
            Rect inner = new Rect(rect.x + 2f, rect.y + 2f, rect.width - 4f, rect.height - 4f);
            GUI.color = bgCol;
            GUI.DrawTexture(inner, Texture2D.whiteTexture);

            // Fill bar
            float fillW = Mathf.Round(inner.width * ratio);
            if (fillW > 0f)
            {
                Rect fillRect = new Rect(inner.x, inner.y, fillW, inner.height);
                GUI.color = fillCol;
                GUI.DrawTexture(fillRect, Texture2D.whiteTexture);

                // Top 2px pixel highlight strip
                if (inner.height >= 5f)
                {
                    GUI.color = Color.Lerp(fillCol, Color.white, 0.42f);
                    GUI.DrawTexture(new Rect(inner.x, inner.y, fillW, 2f), Texture2D.whiteTexture);
                }

                // Bottom 1px pixel shadow strip
                if (inner.height >= 6f)
                {
                    GUI.color = Color.Lerp(fillCol, Color.black, 0.35f);
                    GUI.DrawTexture(new Rect(inner.x, inner.yMax - 1f, fillW, 1f), Texture2D.whiteTexture);
                }
            }

            // Pixel segment ticks
            if (segments > 1 && inner.width >= 30f)
            {
                GUI.color = new Color(0.04f, 0.06f, 0.10f, 0.55f);
                float step = inner.width / segments;
                for (int i = 1; i < segments; i++)
                {
                    float sx = Mathf.Round(inner.x + i * step);
                    GUI.DrawTexture(new Rect(sx, inner.y, 1f, inner.height), Texture2D.whiteTexture);
                }
            }

            GUI.color = Color.white;
        }

        private void OnGUI()
        {
            EnsureStyles();

            if (_manager == null)
                _manager = FindFirstObjectByType<PMDOpenWorldManager>();

            var player = PMDPlayerController.Instance;
            if (player == null)
                return;

            DrawWorldSpaceMonsterBarsAndGateways(player);
            DrawTopLeftPortraitAndControls(player);
            DrawRoomTransitionBanner();
            DrawBottomSkillHotbar(player);
            DrawRightRegionAndCharacterPanel(player);
            DrawToastsAndModals(player);
        }

        private void DrawTopLeftPortraitAndControls(PMDPlayerController player)
        {
            var data = player.Animator != null ? player.Animator.CharacterData : null;
            if (data == null)
                return;

            var gs = PMDGameState.Instance;
            int lvl = gs != null ? gs.Level : 1;
            int exp = gs != null ? gs.Exp : 0;
            int nextExp = gs != null ? gs.ExpToNextLevel : 30;
            int coins = gs != null ? gs.Coins : 0;

            string activePokeName = player.ActiveHuntingSpeciesName;
            float pokeHp = player.DisplayedPokemonHp;
            float pokeMaxHp = Mathf.Max(1f, player.DisplayedPokemonMaxHp);

            Rect cardRect = new Rect(14f, 14f, 412f, 218f);
            GUI.Box(cardRect, GUIContent.none, _boxStyle);

            // Pixel Portrait Frame (shows active Pokémon or selected Pokéball Pokémon)
            Rect portFrameRect = new Rect(26f, 26f, 84f, 84f);
            GUI.Box(portFrameRect, GUIContent.none, _portraitBoxStyle);

            var pokePortraitData = player.IsPokemonOut ? data : (player.SelectedPokeballPokemon ?? data);
            string emotion = pokeHp < pokeMaxHp * 0.35f ? "Pain" :
                             (player.Animator.IsPlayingOneShot ? "Determined" : "Normal");
            Sprite portrait = pokePortraitData.GetPortrait(emotion) ?? pokePortraitData.GetPortrait("Normal");
            if (portrait != null && portrait.texture != null)
            {
                Rect portRect = new Rect(30f, 30f, 76f, 76f);
                DrawSpriteInGUI(portRect, portrait);
            }

            string headerText = player.IsPokemonOut
                ? $"🟢 ĐIỀU KHIỂN: {activePokeName.ToUpper()} [Lv.{lvl}] • {coins}P"
                : $"🔴 TRONG BÓNG: {activePokeName.ToUpper()} [Lv.{lvl}] • {coins}P";
            DrawPixelLabel(new Rect(118f, 22f, 294f, 22f),
                headerText,
                _titleStyle, player.IsPokemonOut ? new Color(0.45f, 1f, 0.65f) : new Color(1f, 0.92f, 0.38f));

            // Segmented Pixel HP Bar (belongs ONLY to the Pokémon, never the Human Trainer)
            float hpRatio = Mathf.Clamp01(pokeHp / pokeMaxHp);
            Color hpCol = Color.Lerp(new Color(0.95f, 0.28f, 0.25f), new Color(0.25f, 0.92f, 0.45f), hpRatio);
            Rect hpBarRect = new Rect(118f, 45f, 288f, 16f);
            DrawPixelBar(hpBarRect, hpRatio, hpCol, new Color(0.14f, 0.16f, 0.22f, 1f), 16);
            DrawPixelLabel(new Rect(124f, 44f, 276f, 18f),
                $"HP {activePokeName} {Mathf.CeilToInt(pokeHp)}/{Mathf.CeilToInt(pokeMaxHp)}",
                _labelStyle, Color.white);

            // Segmented Pixel EXP Bar
            float expRatio = Mathf.Clamp01((float)exp / Mathf.Max(1, nextExp));
            Rect expBarRect = new Rect(118f, 64f, 288f, 10f);
            DrawPixelBar(expBarRect, expRatio, new Color(0.32f, 0.82f, 1f), new Color(0.10f, 0.14f, 0.22f, 1f), 16);

            var mapGen = _manager != null ? _manager.MapGenerator : null;
            string regionName = (mapGen != null && mapGen.ActiveZone != null) ? mapGen.ActiveZone.displayName : "Vùng 1: Thảo Nguyên Thị Trấn";
            string clearingName = FindCurrentClearingName(player.transform.position);
            int seed = mapGen != null ? mapGen.CurrentSeed : 0;

            DrawPixelLabel(new Rect(118f, 77f, 288f, 18f),
                $"{regionName} (EXP {exp}/{nextExp})", _labelStyle, new Color(0.65f, 0.92f, 1f));
            DrawPixelLabel(new Rect(118f, 94f, 288f, 18f),
                $"Khu vực: {clearingName} (Seed #{seed})", _labelStyle, new Color(0.88f, 0.94f, 1f));
            DrawPixelLabel(new Rect(26f, 114f, 386f, 18f),
                $"Trong bóng: {player.RecruitedParty.Count}/3  •  [R] {(player.IsPokemonOut ? "Thu vào bóng" : "Ném bóng ra")}  |  [E] Bắt  |  [Q] Hồi HP",
                _labelStyle, new Color(0.75f, 0.98f, 0.78f));
            DrawPixelLabel(new Rect(26f, 132f, 386f, 18f),
                "Di chuyển: [WASD]  |  Ngắm: [Chuột]  |  Đổi Pokémon: [Tab]",
                _labelStyle, new Color(0.95f, 0.92f, 0.72f));

            if (gs != null)
            {
                string ballBtnText = player.IsPokemonOut
                    ? $"🔴 [R] THU {activePokeName.ToUpper()}"
                    : $"🔴 [R] NÉM {activePokeName.ToUpper()}";
                if (GUI.Button(new Rect(26f, 184f, 224f, 26f), ballBtnText, _accentButtonStyle))
                {
                    player.TogglePokeballControl();
                }
                string autoBtnText = player.IsAutoTrainEnabled ? "🤖 [T] AUTO: BẬT" : "🤖 [T] AUTO: TẮT";
                if (GUI.Button(new Rect(256f, 184f, 156f, 26f), autoBtnText, player.IsAutoTrainEnabled ? _accentButtonStyle : _buttonStyle))
                {
                    player.ToggleAutoTrain();
                }
                if (GUI.Button(new Rect(26f, 154f, 90f, 26f), "[I] TÚI ĐỒ", _buttonStyle))
                {
                    gs.IsInventoryOpen = !gs.IsInventoryOpen;
                    gs.IsQuestBoardOpen = false;
                    gs.IsShopOpen = false;
                }
                if (GUI.Button(new Rect(122f, 154f, 102f, 26f), "[B] NHIỆM VỤ", _buttonStyle))
                {
                    gs.IsQuestBoardOpen = !gs.IsQuestBoardOpen;
                    gs.IsInventoryOpen = false;
                    gs.IsShopOpen = false;
                    if (gs.IsQuestBoardOpen && gs.QuestOffers.Count == 0)
                        gs.GenerateQuestOffers(mapGen != null ? mapGen.ActiveZoneIndex : 0);
                }
                if (GUI.Button(new Rect(230f, 154f, 84f, 26f), "[F5] LƯU", _buttonStyle))
                {
                    gs.SaveGame();
                }
                if (GUI.Button(new Rect(320f, 154f, 84f, 26f), "TẢI GAME", _buttonStyle))
                {
                    var save = gs.LoadSaveData();
                    if (save != null)
                    {
                        gs.ApplySaveData(save);
                        PMDGameState.Toast("Đã tải dữ liệu lưu!", new Color(0.55f, 0.95f, 1f));
                    }
                }
            }
        }

        private void DrawRoomTransitionBanner()
        {
            float elapsed = Time.time - PMDOpenWorldMapGenerator.LastTransitionBannerTime;
            if (elapsed < 0f || elapsed > 3.2f || string.IsNullOrEmpty(PMDOpenWorldMapGenerator.LastTransitionBannerText))
                return;

            float alpha = elapsed < 2.2f ? 1f : Mathf.Clamp01((3.2f - elapsed) / 1.0f);
            float w = 540f;
            float h = 44f;
            Rect bannerRect = new Rect((Screen.width - w) * 0.5f, 16f, w, h);

            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.Box(bannerRect, GUIContent.none, _boxStyle);
            DrawPixelLabel(bannerRect, PMDOpenWorldMapGenerator.LastTransitionBannerText, _bannerStyle, new Color(1f, 0.95f, 0.45f, alpha));
            GUI.color = Color.white;
        }

        private void DrawBottomSkillHotbar(PMDPlayerController player)
        {
            var skills = player.CurrentSkills;
            if (skills == null || skills.Length == 0)
                return;

            float slotW = 196f;
            float slotH = 88f;
            float spacing = 10f;
            float totalW = (slotW * skills.Length) + (spacing * (skills.Length - 1)) + 28f;
            float startX = (Screen.width - totalW) * 0.5f;
            float startY = Screen.height - slotH - 18f;

            Rect barBg = new Rect(startX, startY - 30f, totalW, slotH + 40f);
            GUI.Box(barBg, GUIContent.none, _boxStyle);

            string hunterName = player.ActiveHuntingSpeciesName;
            DrawPixelLabel(new Rect(startX + 14f, startY - 24f, totalW - 28f, 20f),
                $"★ CHIÊU THỨC POKÉMON ĐANG RA SĂN: {hunterName.ToUpper()}  (HLV Chỉ Huy theo Chuột • Bấm J / K / L / U):",
                _titleStyle, new Color(1f, 0.92f, 0.38f));

            for (int i = 0; i < skills.Length; i++)
            {
                var skill = skills[i];
                if (skill == null)
                    continue;

                float x = startX + 14f + i * (slotW + spacing);
                Rect slotRect = new Rect(x, startY, slotW, slotH);

                float cdRem = player.GetSkillCooldownRemaining(i);
                bool isReady = cdRem <= 0.01f;

                GUI.Box(slotRect, GUIContent.none, _subBoxStyle);

                // Top 4px pixel elemental bar inside slot
                Color elemCol = isReady ? skill.elementColor : new Color(0.35f, 0.38f, 0.45f, 0.9f);
                GUI.color = elemCol;
                GUI.DrawTexture(new Rect(slotRect.x + 4f, slotRect.y + 4f, slotRect.width - 8f, 4f), Texture2D.whiteTexture);
                GUI.color = Color.white;

                if (GUI.Button(slotRect, GUIContent.none, GUIStyle.none))
                {
                    player.TryCastSkillSlot(i);
                }

                DrawPixelLabel(new Rect(slotRect.x + 8f, slotRect.y + 9f, slotRect.width - 16f, 20f),
                    $"[{skill.hotkeyLabel}] {skill.skillName.ToUpper()}", _titleStyle, isReady ? skill.elementColor : new Color(0.65f, 0.7f, 0.78f));

                DrawPixelLabel(new Rect(slotRect.x + 8f, slotRect.y + 27f, slotRect.width - 16f, 18f),
                    $"Hệ {skill.moveType}  •  BP {skill.canonicalPower}  •  Dmg {skill.damage:0}",
                    _labelStyle, new Color(0.78f, 0.92f, 1f));

                if (!string.IsNullOrEmpty(skill.description))
                {
                    DrawPixelLabel(new Rect(slotRect.x + 8f, slotRect.y + 43f, slotRect.width - 16f, 22f),
                        skill.description, _labelStyle, new Color(0.88f, 0.90f, 0.95f));
                }

                Rect gaugeRect = new Rect(slotRect.x + 8f, slotRect.y + 66f, slotRect.width - 16f, 14f);
                if (isReady)
                {
                    DrawPixelBar(gaugeRect, 1f, new Color(0.22f, 0.72f, 0.36f), new Color(0.08f, 0.14f, 0.12f), 10);
                    DrawPixelLabel(new Rect(gaugeRect.x + 6f, gaugeRect.y - 2f, gaugeRect.width - 12f, 18f),
                        "■ SẴN SÀNG TUNG CHIÊU", _labelStyle, new Color(0.65f, 1f, 0.72f));
                }
                else
                {
                    float ratio = 1f - Mathf.Clamp01(cdRem / Mathf.Max(0.1f, skill.cooldown));
                    DrawPixelBar(gaugeRect, ratio, new Color(0.95f, 0.48f, 0.18f), new Color(0.14f, 0.10f, 0.10f), 10);
                    DrawPixelLabel(new Rect(gaugeRect.x + 6f, gaugeRect.y - 2f, gaugeRect.width - 12f, 18f),
                        $"Hồi chiêu: {cdRem:0.0}s", _labelStyle, new Color(1f, 0.85f, 0.55f));
                }
            }
        }

        private void DrawRightRegionAndCharacterPanel(PMDPlayerController player)
        {
            float panelW = 308f;
            Rect rightPanel = new Rect(Screen.width - panelW - 14f, 14f, panelW, 496f);
            GUI.Box(rightPanel, GUIContent.none, _boxStyle);

            DrawPixelLabel(new Rect(rightPanel.x + 12f, rightPanel.y + 10f, 284f, 22f),
                "★ CHỌN ĐỔI POKÉMON RA SĂN [TAB]", _titleStyle, new Color(1f, 0.92f, 0.38f));

            var mapGen = _manager != null ? _manager.MapGenerator : null;

            if (GUI.Button(new Rect(rightPanel.x + 12f, rightPanel.y + 34f, 284f, 28f),
                "[PHÍM G] TẠO MAP NGẪU NHIÊN MỚI", _accentButtonStyle))
            {
                if (mapGen != null)
                {
                    mapGen.RegenerateCurrentRegionWithNewSeed();
                }
            }

            if (GUI.Button(new Rect(rightPanel.x + 12f, rightPanel.y + 66f, 284f, 24f),
                _showMapTeleportList ? "ĐANG XEM: 10 VÙNG ĐẤT (MỞ CHỌN POKÉMON)" : "ĐANG XEM: CHỌN POKÉMON RA SĂN (MỞ BẢN ĐỒ)", _buttonStyle))
            {
                _showMapTeleportList = !_showMapTeleportList;
            }

            if (_showMapTeleportList && mapGen != null)
            {
                var activeZone = mapGen.ActiveZone;
                if (activeZone != null && !activeZone.isIndoor)
                {
                    float dirY = rightPanel.y + 94f;
                    float btnW = 68f;
                    if (GUI.Button(new Rect(rightPanel.x + 12f, dirY, btnW, 24f), "▲ BẮC", _buttonStyle))
                        mapGen.TransitionToRegion(activeZone.northZoneIndex, RoomEntranceSide.South, false);
                    if (GUI.Button(new Rect(rightPanel.x + 84f, dirY, btnW, 24f), "▼ NAM", _buttonStyle))
                        mapGen.TransitionToRegion(activeZone.southZoneIndex, RoomEntranceSide.North, false);
                    if (GUI.Button(new Rect(rightPanel.x + 156f, dirY, btnW, 24f), "◄ TÂY", _buttonStyle))
                        mapGen.TransitionToRegion(activeZone.westZoneIndex, RoomEntranceSide.East, false);
                    if (GUI.Button(new Rect(rightPanel.x + 228f, dirY, btnW, 24f), "► ĐÔNG", _buttonStyle))
                        mapGen.TransitionToRegion(activeZone.eastZoneIndex, RoomEntranceSide.West, false);
                }

                var zones = mapGen.Zones;
                Rect scrollViewRect = new Rect(rightPanel.x + 12f, rightPanel.y + 124f, 284f, 356f);
                Rect contentRect = new Rect(0f, 0f, 264f, zones.Count * 32f);

                _mapScrollPos = GUI.BeginScrollView(scrollViewRect, _mapScrollPos, contentRect);
                for (int i = 0; i < zones.Count; i++)
                {
                    var z = zones[i];
                    bool isCurrent = (i == mapGen.ActiveZoneIndex);
                    string prefix = isCurrent ? "★ " : "• ";
                    GUIStyle st = isCurrent ? _accentButtonStyle : _buttonStyle;
                    if (GUI.Button(new Rect(0f, i * 32f, 264f, 28f), $"{prefix}{z.displayName}", st))
                    {
                        mapGen.TransitionToRegion(i, RoomEntranceSide.Center, true);
                    }
                }
                GUI.EndScrollView();
            }
            else if (_manager != null && _manager.LoadedCharacters != null)
            {
                float curY = rightPanel.y + 94f;

                // 1. Active Hunting Squad slots (up to 3 companions in party)
                var party = player.RecruitedParty;
                DrawPixelLabel(new Rect(rightPanel.x + 12f, curY, 284f, 18f),
                    $"ĐỘI ĐANG ĐI SĂN ({party.Count}/3) — Bấm để đổi chủ lực:", _labelStyle, new Color(0.65f, 0.95f, 1f));
                curY += 20f;

                for (int p = 0; p < party.Count && p < 3; p++)
                {
                    var mon = party[p];
                    if (mon == null) continue;
                    bool isLead = (p == 0);
                    Rect slotBtn = new Rect(rightPanel.x + 12f + p * 94f, curY, 90f, 24f);
                    string label = isLead ? $"★1:{mon.PokemonName}" : $"#{p + 1}:{mon.PokemonName}";
                    if (GUI.Button(slotBtn, label, isLead ? _accentButtonStyle : _buttonStyle))
                    {
                        if (!isLead)
                            player.PromotePartyMemberToLeadHunter(p);
                    }
                }
                curY += 30f;

                var chars = _manager.LoadedCharacters;
                int totalPoke = 0;
                for (int i = 0; i < chars.Count; i++)
                    if (chars[i] != null && chars[i].pokemonId != "0000")
                        totalPoke++;

                DrawPixelLabel(new Rect(rightPanel.x + 12f, curY, 284f, 18f),
                    $"KHO {totalPoke} POKÉMON (Tìm tên hoặc nhập #ID 1-1025):", _labelStyle, new Color(1f, 0.92f, 0.55f));
                curY += 20f;

                // Search / Pokédex #1-1025 Live Download Bar
                GUI.Box(new Rect(rightPanel.x + 12f, curY, 178f, 24f), GUIContent.none, _subBoxStyle);
                _pokeSearchFilter = GUI.TextField(new Rect(rightPanel.x + 16f, curY + 3f, 170f, 18f), _pokeSearchFilter ?? "", _labelStyle);

                if (GUI.Button(new Rect(rightPanel.x + 194f, curY, 102f, 24f),
                    PMDRuntimeCharacterLoader.IsDownloadingPokemon ? "ĐANG TẢI..." : "TẢI #ID MỚI", _accentButtonStyle))
                {
                    if (int.TryParse((_pokeSearchFilter ?? "").Trim().TrimStart('#'), out int reqDex) && reqDex >= 1 && reqDex <= 1025)
                    {
                        PMDRuntimeCharacterLoader.DownloadAndSummonByDexIdAsync(reqDex);
                    }
                    else
                    {
                        PMDGameState.Toast("Nhập số Pokédex (từ 1 đến 1025) vào ô tìm kiếm rồi bấm [TẢI #ID MỚI]!", new Color(1f, 0.8f, 0.35f));
                    }
                }
                curY += 28f;

                string filter = (_pokeSearchFilter ?? "").Trim();
                int filteredCount = 0;
                for (int i = 0; i < chars.Count; i++)
                {
                    var c = chars[i];
                    if (c == null || c.pokemonId == "0000") continue;
                    if (!string.IsNullOrEmpty(filter) &&
                        c.pokemonName.IndexOf(filter, System.StringComparison.OrdinalIgnoreCase) < 0 &&
                        c.pokemonId.IndexOf(filter, System.StringComparison.OrdinalIgnoreCase) < 0)
                        continue;
                    filteredCount++;
                }

                int totalRows = (filteredCount + 1) / 2;
                Rect scrollViewRect = new Rect(rightPanel.x + 10f, curY, 288f, rightPanel.y + rightPanel.height - curY - 10f);
                Rect contentRect = new Rect(0f, 0f, 268f, Mathf.Max(60f, totalRows * 33f + 6f));

                _mapScrollPos = GUI.BeginScrollView(scrollViewRect, _mapScrollPos, contentRect);
                int gridIdx = 0;
                for (int i = 0; i < chars.Count; i++)
                {
                    var c = chars[i];
                    if (c == null || c.pokemonId == "0000")
                        continue;

                    if (!string.IsNullOrEmpty(filter) &&
                        c.pokemonName.IndexOf(filter, System.StringComparison.OrdinalIgnoreCase) < 0 &&
                        c.pokemonId.IndexOf(filter, System.StringComparison.OrdinalIgnoreCase) < 0)
                        continue;

                    int col = gridIdx % 2;
                    int row = gridIdx / 2;
                    gridIdx++;

                    bool isSelected = string.Equals(player.ActiveHuntingSpeciesName, c.pokemonName, System.StringComparison.OrdinalIgnoreCase);
                    Rect btnRect = new Rect(col * 136f, row * 33f, 130f, 28f);
                    string btnText = $"{(isSelected ? "🔴" : "⚪")}#{c.dexId} {c.pokemonName.ToUpper()}";
                    if (GUI.Button(btnRect, btnText, isSelected ? _accentButtonStyle : _buttonStyle))
                    {
                        player.SelectCharacterByIndex(i);
                    }
                }
                GUI.EndScrollView();
            }
        }

        private string FindCurrentClearingName(Vector2 playerPos)
        {
            var mapGen = _manager != null ? _manager.MapGenerator : null;
            if (mapGen == null || mapGen.ActiveClearings == null || mapGen.ActiveClearings.Count == 0)
                return "Quảng Trường Trung Tâm";

            string best = mapGen.ActiveClearings[0].clearingName;
            float bestDist = float.MaxValue;
            for (int i = 0; i < mapGen.ActiveClearings.Count; i++)
            {
                var cl = mapGen.ActiveClearings[i];
                float d = Vector2.Distance(playerPos, cl.center);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = cl.clearingName;
                }
            }
            return best;
        }

        private void DrawWorldSpaceMonsterBarsAndGateways(PMDPlayerController player)
        {
            Camera cam = Camera.main;
            if (cam == null)
                return;

            // 0. Draw Crisp Pixel-Art Mouse Aim Crosshair in World Space
            Vector2 aimDir = player.GetMouseAimDirection();
            if (aimDir.sqrMagnitude > 0.01f && _pixelCrosshairTex != null)
            {
                Vector3 aimWorld = player.transform.position + new Vector3(0f, 0.22f, 0f) + (Vector3)(aimDir * 1.25f);
                Vector3 aimScreen = cam.WorldToScreenPoint(aimWorld);
                if (aimScreen.z > 0f)
                {
                    float ax = Mathf.Round(aimScreen.x);
                    float ay = Mathf.Round(Screen.height - aimScreen.y);
                    Color aimCol = (player.CurrentSkills != null && player.CurrentSkills.Length > 0 && player.CurrentSkills[0] != null)
                        ? player.CurrentSkills[0].elementColor
                        : new Color(1f, 0.92f, 0.35f);
                    GUI.color = aimCol;
                    GUI.DrawTexture(new Rect(ax - 13f, ay - 13f, 26f, 26f), _pixelCrosshairTex);
                    GUI.color = Color.white;
                }
            }

            // 1. Draw Pixel-Art World-Space Labels above nearby Room Gateways & Dungeon Stairs
            var gateways = PMDRoomGatewayTrigger.ActiveGateways;
            for (int i = 0; i < gateways.Count; i++)
            {
                var gw = gateways[i];
                if (gw == null || string.IsNullOrEmpty(gw.gatewayLabel))
                    continue;

                if (Vector2.Distance(player.transform.position, gw.transform.position) > 15f)
                    continue;

                Vector3 worldPos = gw.transform.position + new Vector3(0f, 1.35f, 0f);
                Vector3 screenPos = cam.WorldToScreenPoint(worldPos);
                if (screenPos.z <= 0f)
                    continue;

                float guiX = Mathf.Round(screenPos.x);
                float guiY = Mathf.Round(Screen.height - screenPos.y);

                Rect tagBg = new Rect(guiX - 138f, guiY - 13f, 276f, 24f);
                GUI.Box(tagBg, GUIContent.none, _tagBoxStyle);
                DrawPixelLabel(tagBg, gw.gatewayLabel, _gatewayTagStyle, new Color(0.55f, 0.98f, 1f));
            }

            // 2. Draw Pixel-Art Overhead Monster HP Bars & Dazed Capture Prompts
            var mons = PMDWildPokemonAI.ActiveMons;
            for (int i = 0; i < mons.Count; i++)
            {
                var mon = mons[i];
                if (mon == null || (mon.IsFainted && !mon.IsDazed && !mon.IsRecruited) || mon.IsBeingCaptured)
                    continue;

                if (Vector2.Distance(player.transform.position, mon.transform.position) > 13f)
                    continue;

                Vector3 worldPos = mon.transform.position + new Vector3(0f, 0.98f, 0f);
                Vector3 screenPos = cam.WorldToScreenPoint(worldPos);
                if (screenPos.z <= 0f)
                    continue;

                float guiX = Mathf.Round(screenPos.x);
                float guiY = Mathf.Round(Screen.height - screenPos.y);
                Rect barBg = new Rect(guiX - 36f, guiY - 5f, 72f, 9f);

                if (mon.IsDazed)
                {
                    float catchChance = PMDCaptureBall.CalculateCatchChance(mon, false);
                    DrawPixelLabel(
                        new Rect(guiX - 95f, guiY - 40f, 190f, 18f),
                        $"[E] Ném Poké Ball • {catchChance * 100f:0}%",
                        _worldTagStyle,
                        new Color(1f, 0.92f, 0.32f));
                    DrawPixelLabel(
                        new Rect(guiX - 95f, guiY - 24f, 190f, 18f),
                        $"{mon.PokemonName} [NGẤT NGƯ • {mon.CaptureAttemptsRemaining} lượt]",
                        _worldTagStyle,
                        new Color(0.92f, 0.72f, 1f));
                    DrawPixelBar(barBg, mon.DazeTimeRatio, new Color(1f, 0.80f, 0.22f), new Color(0.14f, 0.08f, 0.22f, 0.95f), 6);
                    continue;
                }

                string statusTag = mon.IsRecruited ? "★ ĐỒNG ĐỘI" : (mon.IsAggressive ? "⚔ HUNG DỮ" : "HOANG DÃ");
                string ailment = mon.ActiveAilmentTag;
                if (!string.IsNullOrEmpty(ailment))
                    statusTag = $"{statusTag} • {ailment}";

                Color tagCol = !string.IsNullOrEmpty(ailment)
                    ? new Color(1f, 0.90f, 0.35f)
                    : (mon.IsRecruited
                        ? new Color(0.45f, 0.92f, 1f)
                        : (mon.IsAggressive ? new Color(1f, 0.55f, 0.45f) : new Color(0.75f, 1f, 0.78f)));
                DrawPixelLabel(new Rect(guiX - 105f, guiY - 24f, 210f, 18f), $"{mon.PokemonName} [{statusTag}]", _worldTagStyle, tagCol);

                float ratio = Mathf.Clamp01(mon.CurrentHp / Mathf.Max(1f, mon.MaxHp));
                Color barCol = mon.IsRecruited
                    ? new Color(0.3f, 0.85f, 1f)
                    : (mon.IsAggressive ? new Color(0.95f, 0.32f, 0.28f) : new Color(0.32f, 0.92f, 0.45f));
                DrawPixelBar(barBg, ratio, barCol, new Color(0.08f, 0.10f, 0.14f, 0.95f), 6);
            }

            // 3. Render Pixel-Shadowed Floating Combat Numbers & Skill Callouts
            var texts = PMDVfxPlayer.ActiveFloatingTexts;
            float now = Time.time;
            for (int i = texts.Count - 1; i >= 0; i--)
            {
                var entry = texts[i];
                if (now >= entry.expireTime)
                {
                    texts.RemoveAt(i);
                    continue;
                }

                float t = Mathf.Clamp01((now - entry.startTime) / Mathf.Max(0.01f, entry.expireTime - entry.startTime));
                Vector3 worldPos = entry.worldPosition + new Vector3(0f, t * 0.85f, 0f);
                Vector3 screenPos = cam.WorldToScreenPoint(worldPos);
                if (screenPos.z <= 0f)
                    continue;

                float guiX = Mathf.Round(screenPos.x);
                float guiY = Mathf.Round(Screen.height - screenPos.y);

                Color c = entry.color;
                c.a = 1f - (t * 0.65f);
                DrawPixelLabel(new Rect(guiX - 100f, guiY - 12f, 200f, 24f), entry.text, _floatingTextStyle, c);
            }

            // 4. Draw Nearby Interactable Prompt [F]
            var nearestIt = PMDInteractable.FindNearest(player.transform.position);
            if (nearestIt != null && !string.IsNullOrEmpty(nearestIt.PromptText))
            {
                Vector3 screenPos = cam.WorldToScreenPoint(nearestIt.transform.position + new Vector3(0f, 1.15f, 0f));
                if (screenPos.z > 0f)
                {
                    Rect promptRect = new Rect(Mathf.Round(screenPos.x) - 115f, Mathf.Round(Screen.height - screenPos.y) - 13f, 230f, 26f);
                    GUI.Box(promptRect, GUIContent.none, _tagBoxStyle);
                    DrawPixelLabel(promptRect, nearestIt.PromptText, _gatewayTagStyle, new Color(1f, 0.95f, 0.45f));
                }
            }
        }

        private void DrawToastsAndModals(PMDPlayerController player)
        {
            var gs = PMDGameState.Instance;
            if (gs == null)
                return;

            // Toast notifications (top-center below banner)
            float now = Time.unscaledTime;
            float toastY = 68f;
            for (int i = gs.Toasts.Count - 1; i >= 0; i--)
            {
                var t = gs.Toasts[i];
                if (now - t.time > 4.0f)
                {
                    gs.Toasts.RemoveAt(i);
                    continue;
                }
                Rect tr = new Rect((Screen.width - 490f) * 0.5f, toastY, 490f, 30f);
                GUI.Box(tr, GUIContent.none, _tagBoxStyle);
                DrawPixelLabel(new Rect(tr.x + 12f, tr.y + 5f, tr.width - 24f, 20f), t.text, _titleStyle, t.color);
                toastY += 34f;
            }

            // Dialogue Box (Classic PMD Pixel Dialog Frame)
            if (gs.IsDialogueOpen)
            {
                Rect dlgRect = new Rect((Screen.width - 630f) * 0.5f, Screen.height - 236f, 630f, 114f);
                GUI.Box(dlgRect, GUIContent.none, _boxStyle);

                DrawPixelLabel(new Rect(dlgRect.x + 18f, dlgRect.y + 12f, 450f, 22f), $"★ {gs.DialogueSpeaker.ToUpper()}", _titleStyle, new Color(1f, 0.92f, 0.38f));
                string line = gs.DialogueLines[Mathf.Clamp(gs.DialogueIndex, 0, gs.DialogueLines.Length - 1)];
                DrawPixelLabel(new Rect(dlgRect.x + 18f, dlgRect.y + 38f, 594f, 44f), line, _labelStyle, Color.white);

                if (GUI.Button(new Rect(dlgRect.x + 472f, dlgRect.y + 78f, 142f, 26f), "TIẾP TỤC [SPACE/F] ►", _accentButtonStyle))
                {
                    gs.AdvanceDialogue();
                }
                return;
            }

            // Inventory Modal [I]
            if (gs.IsInventoryOpen)
            {
                Rect win = new Rect((Screen.width - 540f) * 0.5f, (Screen.height - 410f) * 0.5f, 540f, 410f);
                GUI.Box(win, GUIContent.none, _boxStyle);

                DrawPixelLabel(new Rect(win.x + 18f, win.y + 14f, 380f, 24f), $"★ TÚI ĐỒ THÁM HIỂM   (TIỀN: {gs.Coins} POKÉ)", _titleStyle, new Color(1f, 0.92f, 0.38f));
                if (GUI.Button(new Rect(win.x + 432f, win.y + 12f, 92f, 26f), "✖ ĐÓNG [I]", _buttonStyle))
                {
                    gs.IsInventoryOpen = false;
                }

                float y = win.y + 50f;
                var keys = new System.Collections.Generic.List<string>(gs.Inventory.Keys);
                if (keys.Count == 0)
                {
                    DrawPixelLabel(new Rect(win.x + 20f, y, 490f, 24f), "Túi đồ đang trống. Hãy nhặt vật phẩm trên bản đồ hoặc mua tại Cửa Hàng!", _labelStyle, new Color(0.8f, 0.88f, 1f));
                }
                for (int i = 0; i < keys.Count && y < win.y + 370f; i++)
                {
                    string k = keys[i];
                    int cnt = gs.CountItem(k);
                    if (cnt <= 0) continue;
                    var info = PMDGameState.GetItemInfo(k);
                    GUI.Box(new Rect(win.x + 14f, y - 2f, 512f, 28f), GUIContent.none, _subBoxStyle);
                    DrawPixelLabel(new Rect(win.x + 22f, y + 2f, 318f, 22f), $"■ {info.displayName} x{cnt} — {info.description}", _labelStyle, Color.white);
                    if (GUI.Button(new Rect(win.x + 346f, y, 82f, 24f), "DÙNG", _accentButtonStyle))
                    {
                        gs.UseItem(k);
                    }
                    if (GUI.Button(new Rect(win.x + 434f, y, 86f, 24f), $"BÁN (+{Mathf.Max(5, info.buyPrice / 2)})", _buttonStyle))
                    {
                        gs.SellItem(k);
                    }
                    y += 32f;
                }
                return;
            }

            // Quest Board Modal [B]
            if (gs.IsQuestBoardOpen)
            {
                Rect win = new Rect((Screen.width - 550f) * 0.5f, (Screen.height - 400f) * 0.5f, 550f, 400f);
                GUI.Box(win, GUIContent.none, _boxStyle);

                DrawPixelLabel(new Rect(win.x + 18f, win.y + 14f, 400f, 24f), "★ BẢNG NHIỆM VỤ HỘI THÁM HIỂM", _titleStyle, new Color(1f, 0.92f, 0.38f));
                if (GUI.Button(new Rect(win.x + 442f, win.y + 12f, 92f, 26f), "✖ ĐÓNG [B]", _buttonStyle))
                {
                    gs.IsQuestBoardOpen = false;
                }

                float y = win.y + 48f;
                DrawPixelLabel(new Rect(win.x + 18f, y, 500f, 22f), $"NHIỆM VỤ ĐANG NHẬN ({gs.ActiveQuests.Count}/{PMDGameState.MaxActiveQuests}):", _titleStyle, new Color(0.55f, 0.95f, 1f));
                y += 24f;
                for (int i = 0; i < gs.ActiveQuests.Count; i++)
                {
                    var q = gs.ActiveQuests[i];
                    GUI.Box(new Rect(win.x + 16f, y - 2f, 518f, 26f), GUIContent.none, _subBoxStyle);
                    DrawPixelLabel(new Rect(win.x + 24f, y + 2f, 500f, 22f), $"★ {q.title}: {q.progress}/{q.required}  (Thưởng: {q.rewardCoins} Poké, {q.rewardExp} EXP)", _labelStyle, new Color(0.75f, 1f, 0.78f));
                    y += 28f;
                }

                y += 10f;
                DrawPixelLabel(new Rect(win.x + 18f, y, 500f, 22f), "NHIỆM VỤ MỚI TRÊN BẢNG:", _titleStyle, new Color(1f, 0.92f, 0.38f));
                y += 26f;
                for (int i = 0; i < gs.QuestOffers.Count; i++)
                {
                    var q = gs.QuestOffers[i];
                    GUI.Box(new Rect(win.x + 16f, y - 2f, 518f, 28f), GUIContent.none, _subBoxStyle);
                    DrawPixelLabel(new Rect(win.x + 24f, y + 2f, 395f, 24f), $"• {q.title} — Thưởng {q.rewardCoins} Poké, {q.rewardExp} EXP", _labelStyle, Color.white);
                    if (GUI.Button(new Rect(win.x + 424f, y, 104f, 24f), "NHẬN", _accentButtonStyle))
                    {
                        gs.AcceptQuest(q);
                    }
                    y += 32f;
                }
                return;
            }

            // Shop Modal
            if (gs.IsShopOpen)
            {
                Rect win = new Rect((Screen.width - 540f) * 0.5f, (Screen.height - 410f) * 0.5f, 540f, 410f);
                GUI.Box(win, GUIContent.none, _boxStyle);

                DrawPixelLabel(new Rect(win.x + 18f, win.y + 14f, 390f, 24f), $"★ CỬA HÀNG PELIPPER   (TIỀN: {gs.Coins} POKÉ)", _titleStyle, new Color(1f, 0.92f, 0.38f));
                if (GUI.Button(new Rect(win.x + 432f, win.y + 12f, 92f, 26f), "✖ ĐÓNG", _buttonStyle))
                {
                    gs.IsShopOpen = false;
                }

                float y = win.y + 48f;
                for (int i = 0; i < PMDGameState.ShopStock.Length && y < win.y + 375f; i++)
                {
                    string file = PMDGameState.ShopStock[i];
                    var info = PMDGameState.GetItemInfo(file);
                    GUI.Box(new Rect(win.x + 14f, y - 2f, 512f, 28f), GUIContent.none, _subBoxStyle);
                    DrawPixelLabel(new Rect(win.x + 22f, y + 2f, 372f, 24f), $"■ {info.displayName} ({info.buyPrice} Poké) — {info.description}", _labelStyle, Color.white);
                    if (GUI.Button(new Rect(win.x + 400f, y, 120f, 24f), $"MUA ({info.buyPrice} POKÉ)", _accentButtonStyle))
                    {
                        if (gs.Coins >= info.buyPrice)
                        {
                            gs.AddCoins(-info.buyPrice);
                            gs.AddItem(file, 1, false);
                            PMDGameState.Toast($"Đã mua {info.displayName}!", new Color(0.55f, 1f, 0.65f));
                        }
                        else
                        {
                            PMDGameState.Toast("Không đủ tiền Poké!", new Color(1f, 0.45f, 0.4f));
                        }
                    }
                    y += 30f;
                }
                return;
            }

            // Game Over Modal
            if (gs.IsGameOver)
            {
                Rect win = new Rect((Screen.width - 430f) * 0.5f, (Screen.height - 206f) * 0.5f, 430f, 206f);
                GUI.Box(win, GUIContent.none, _boxStyle);

                DrawPixelLabel(new Rect(win.x + 22f, win.y + 18f, 386f, 26f), "★ ĐỘI THÁM HIỂM ĐÃ GỤC NGÃ!", _titleStyle, new Color(1f, 0.45f, 0.4f));
                DrawPixelLabel(new Rect(win.x + 22f, win.y + 52f, 386f, 48f), gs.LastGameOverSummary, _labelStyle, Color.white);
                if (GUI.Button(new Rect(win.x + 95f, win.y + 130f, 240f, 36f), "★ HỒI SINH VỀ THỊ TRẤN", _accentButtonStyle))
                {
                    gs.IsGameOver = false;
                    gs.RefreshTimeScale();
                    player.ReviveAfterGameOver();
                    _manager?.MapGenerator?.TransitionToRegion(0, RoomEntranceSide.Center, false);
                }
                return;
            }

            // Pause Modal [Esc]
            if (gs.IsPaused)
            {
                Rect win = new Rect((Screen.width - 350f) * 0.5f, (Screen.height - 196f) * 0.5f, 350f, 196f);
                GUI.Box(win, GUIContent.none, _boxStyle);

                DrawPixelLabel(new Rect(win.x + 22f, win.y + 16f, 306f, 24f), "★ TẠM DỪNG TRÒ CHƠI", _titleStyle, new Color(1f, 0.92f, 0.38f));
                if (GUI.Button(new Rect(win.x + 55f, win.y + 56f, 240f, 32f), "► TIẾP TỤC CHƠI [ESC]", _accentButtonStyle))
                {
                    gs.IsPaused = false;
                    gs.RefreshTimeScale();
                }
                if (GUI.Button(new Rect(win.x + 55f, win.y + 98f, 240f, 30f), "LƯU TIẾN TRÌNH [F5]", _buttonStyle))
                {
                    gs.SaveGame();
                }
                if (GUI.Button(new Rect(win.x + 55f, win.y + 136f, 240f, 30f), "TẠO MAP MỚI [G]", _buttonStyle))
                {
                    gs.IsPaused = false;
                    gs.RefreshTimeScale();
                    _manager?.MapGenerator?.RegenerateCurrentRegionWithNewSeed();
                }
            }
        }

        private static void DrawSpriteInGUI(Rect rect, Sprite sprite)
        {
            Texture2D tex = sprite.texture;
            tex.filterMode = FilterMode.Point;
            Rect tr = sprite.textureRect;
            Rect uv = new Rect(tr.x / tex.width, tr.y / tex.height, tr.width / tex.width, tr.height / tex.height);
            GUI.DrawTextureWithTexCoords(rect, tex, uv);
        }
    }
}
