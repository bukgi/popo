using System.Collections.Generic;
using UnityEngine;

namespace POKE.PMDCollab.OpenWorld
{
    /// <summary>
    /// Procedurally generates a complete 8-directional pixel-art Human Pokémon Trainer ("Trainer")
    /// with full animations (Idle, Walk/Run 8-frame stride, Attack/Throw Poké Ball, Shoot, Charge, Hop, Hurt, Sleep)
    /// and 40x40 pixel portraits, all using FilterMode.Point.
    /// </summary>
    public static class PMDTrainerSpriteBuilder
    {
        private const int FrameW = 32;
        private const int FrameH = 36;
        private const float PixelsPerUnit = 24f;

        private static PMDCharacterData _cachedTrainer;

        // Pixel-Art Palette (16-bit GBA/DS Trainer Red/Ash Style)
        private static readonly Color ColOutline = new Color(0.09f, 0.08f, 0.13f, 1f);
        private static readonly Color ColSkin = new Color(1.00f, 0.86f, 0.73f, 1f);
        private static readonly Color ColSkinShade = new Color(0.88f, 0.69f, 0.55f, 1f);
        private static readonly Color ColHair = new Color(0.15f, 0.16f, 0.24f, 1f);
        private static readonly Color ColHairLight = new Color(0.26f, 0.28f, 0.40f, 1f);

        private static readonly Color ColCapRed = new Color(0.93f, 0.20f, 0.22f, 1f);
        private static readonly Color ColCapShade = new Color(0.70f, 0.11f, 0.15f, 1f);
        private static readonly Color ColWhite = new Color(0.97f, 0.98f, 1.00f, 1f);
        private static readonly Color ColWhiteShade = new Color(0.78f, 0.82f, 0.88f, 1f);

        private static readonly Color ColJacketBlue = new Color(0.20f, 0.48f, 0.92f, 1f);
        private static readonly Color ColJacketShade = new Color(0.11f, 0.29f, 0.65f, 1f);
        private static readonly Color ColShirtDark = new Color(0.16f, 0.18f, 0.25f, 1f);
        private static readonly Color ColGold = new Color(1.00f, 0.86f, 0.24f, 1f);

        private static readonly Color ColPackGreen = new Color(0.24f, 0.68f, 0.36f, 1f);
        private static readonly Color ColPackShade = new Color(0.14f, 0.45f, 0.23f, 1f);

        private static readonly Color ColJeans = new Color(0.22f, 0.30f, 0.52f, 1f);
        private static readonly Color ColJeansShade = new Color(0.14f, 0.19f, 0.36f, 1f);
        private static readonly Color ColJeansLight = new Color(0.34f, 0.44f, 0.70f, 1f);

        private static readonly Color ColCyanGlow = new Color(0.45f, 0.95f, 1.00f, 1f);

        private enum AnimKind
        {
            Idle,
            Walk,
            Attack,
            Hop,
            Hurt,
            Sleep
        }

        public static PMDCharacterData BuildTrainerCharacterData()
        {
            if (_cachedTrainer != null && _cachedTrainer.runtimeVersion == PMDRuntimeCharacterLoader.CurrentSpriteSheetVersion)
            {
                var idle = _cachedTrainer.GetAnimation("Idle");
                if (idle != null && idle.directionRows.Count > 0 && idle.directionRows[0].frames.Count > 0 && idle.directionRows[0].frames[0] != null)
                    return _cachedTrainer;
            }

            var data = ScriptableObject.CreateInstance<PMDCharacterData>();
            data.dexId = 0;
            data.pokemonId = "0000";
            data.pokemonName = "Trainer";
            data.runtimeVersion = PMDRuntimeCharacterLoader.CurrentSpriteSheetVersion;
            data.formFullName = "Pokémon Trainer";
            data.shadowSize = 1;
            data.name = $"Runtime_0000_Trainer_v{PMDRuntimeCharacterLoader.CurrentSpriteSheetVersion}";

            // Core animations
            var idleSeq = BuildAnimSequence("Idle", AnimKind.Idle, 6, new[] { 10, 10, 10, 10, 10, 10 }, -1);
            var walkSeq = BuildAnimSequence("Walk", AnimKind.Walk, 8, new[] { 5, 5, 5, 5, 5, 5, 5, 5 }, -1);
            var attackSeq = BuildAnimSequence("Attack", AnimKind.Attack, 6, new[] { 4, 4, 5, 5, 4, 4 }, 2);
            var hopSeq = BuildAnimSequence("Hop", AnimKind.Hop, 4, new[] { 5, 6, 6, 5 }, 1);
            var hurtSeq = BuildAnimSequence("Hurt", AnimKind.Hurt, 2, new[] { 8, 8 }, 0);
            var sleepSeq = BuildAnimSequence("Sleep", AnimKind.Sleep, 2, new[] { 20, 20 }, -1);

            data.animations.Add(idleSeq);
            data.animations.Add(walkSeq);
            data.animations.Add(attackSeq);
            data.animations.Add(CloneSequenceAlias(attackSeq, "Shoot"));
            data.animations.Add(CloneSequenceAlias(attackSeq, "Charge"));
            data.animations.Add(CloneSequenceAlias(attackSeq, "Strike"));
            data.animations.Add(hopSeq);
            data.animations.Add(CloneSequenceAlias(hopSeq, "Pose"));
            data.animations.Add(hurtSeq);
            data.animations.Add(sleepSeq);

            BuildPortraits(data);

            _cachedTrainer = data;
            return data;
        }

        private static PMDAnimSequence CloneSequenceAlias(PMDAnimSequence source, string aliasName)
        {
            var seq = new PMDAnimSequence
            {
                name = aliasName,
                copyOf = source.name,
                frameWidth = source.frameWidth,
                frameHeight = source.frameHeight,
                hitFrame = source.hitFrame,
                rushFrame = source.rushFrame,
                returnFrame = source.returnFrame
            };
            seq.durations.AddRange(source.durations);
            seq.directionRows.AddRange(source.directionRows);
            return seq;
        }

        private static PMDAnimSequence BuildAnimSequence(string animName, AnimKind kind, int frameCount, int[] durations, int hitFrame)
        {
            var seq = new PMDAnimSequence
            {
                name = animName,
                frameWidth = FrameW,
                frameHeight = FrameH,
                hitFrame = hitFrame
            };
            seq.durations.AddRange(durations);

            int texW = FrameW * frameCount;
            int texH = FrameH * 8;
            var tex = new Texture2D(texW, texH, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = $"Trainer_{animName}_Sheet"
            };

            Color[] sheetPixels = new Color[texW * texH];

            for (int dirIdx = 0; dirIdx < 8; dirIdx++)
            {
                PMDDirection dir = (PMDDirection)dirIdx;
                var dirRow = new PMDDirectionFrames { direction = dir };
                int yBottom = texH - ((dirIdx + 1) * FrameH);

                for (int f = 0; f < frameCount; f++)
                {
                    Color[] frameBuf = new Color[FrameW * FrameH];
                    DrawTrainerFrame(frameBuf, dir, kind, f, frameCount);

                    int xLeft = f * FrameW;
                    for (int py = 0; py < FrameH; py++)
                    {
                        int dstRow = (yBottom + py) * texW + xLeft;
                        int srcRow = py * FrameW;
                        for (int px = 0; px < FrameW; px++)
                        {
                            sheetPixels[dstRow + px] = frameBuf[srcRow + px];
                        }
                    }

                    Rect rect = new Rect(xLeft, yBottom, FrameW, FrameH);
                    Sprite sp = Sprite.Create(tex, rect, new Vector2(0.5f, 0.22f), PixelsPerUnit);
                    sp.name = $"Trainer_{animName}_R{dirIdx}_C{f}";
                    dirRow.frames.Add(sp);
                }

                seq.directionRows.Add(dirRow);
            }

            tex.SetPixels(sheetPixels);
            tex.Apply(false);
            return seq;
        }

        // =========================================================================================
        // 8-DIRECTION PROCEDURAL HUMAN TRAINER RENDERER
        // =========================================================================================
        private static void DrawTrainerFrame(Color[] buf, PMDDirection dir, AnimKind kind, int frame, int totalFrames)
        {
            bool mirrorX = (dir == PMDDirection.Left || dir == PMDDirection.DownLeft || dir == PMDDirection.UpLeft);
            PMDDirection canonicalDir = dir switch
            {
                PMDDirection.Left => PMDDirection.Right,
                PMDDirection.DownLeft => PMDDirection.DownRight,
                PMDDirection.UpLeft => PMDDirection.UpRight,
                _ => dir
            };

            // Compute pose kinematics
            int bodyBobY = 0;
            int torsoLeanX = 0;
            float leftLegPhase = 0f;
            float rightLegPhase = 0f;
            int leftFootLift = 0;
            int rightFootLift = 0;
            float leftArmSwing = 0f;
            float rightArmSwing = 0f;
            int ballBounceY = 0;
            bool throwFlash = false;

            switch (kind)
            {
                case AnimKind.Idle:
                {
                    int[] bobs = { 0, 0, 1, 1, 0, 0 };
                    int[] ballBobs = { 0, 1, 2, 1, 0, 0 };
                    bodyBobY = bobs[frame % bobs.Length];
                    ballBounceY = ballBobs[frame % ballBobs.Length];
                    rightArmSwing = 0.2f;
                    break;
                }
                case AnimKind.Walk:
                {
                    // 8-frame full human stride cycle
                    float angle = (frame / (float)totalFrames) * Mathf.PI * 2f;
                    float s = Mathf.Sin(angle);
                    float c = Mathf.Cos(angle);

                    // Double-bounce per full stride cycle (contact dips, passing rises)
                    int[] walkBob = { 0, -1, 1, 1, 0, -1, 1, 1 };
                    bodyBobY = walkBob[frame % 8];
                    torsoLeanX = canonicalDir == PMDDirection.Right ? 1 : 0;

                    leftLegPhase = s;
                    rightLegPhase = -s;
                    leftFootLift = c > 0.15f ? Mathf.RoundToInt(c * 2.4f) : 0;
                    rightFootLift = c < -0.15f ? Mathf.RoundToInt(-c * 2.4f) : 0;

                    leftArmSwing = -s * 1.15f;
                    rightArmSwing = s * 1.15f;
                    break;
                }
                case AnimKind.Attack:
                {
                    // 6-frame Poké Ball windup -> throw -> command point
                    int[] bobs = { 0, 1, 0, 0, 0, 0 };
                    int[] leans = { -1, -2, 2, 2, 1, 0 };
                    float[] rArm = { -1.1f, -1.6f, 1.85f, 1.6f, 1.2f, 0.4f };
                    float[] lArm = { 0.6f, 0.8f, -0.9f, -0.7f, -0.4f, 0f };
                    bodyBobY = bobs[frame % 6];
                    torsoLeanX = leans[frame % 6];
                    rightArmSwing = rArm[frame % 6];
                    leftArmSwing = lArm[frame % 6];
                    leftLegPhase = frame >= 2 ? 0.65f : -0.35f;
                    rightLegPhase = frame >= 2 ? -0.65f : 0.35f;
                    throwFlash = (frame == 2 || frame == 3);
                    break;
                }
                case AnimKind.Hop:
                {
                    int[] hopY = { 1, 4, 3, 0 };
                    bodyBobY = hopY[frame % 4];
                    leftFootLift = bodyBobY > 0 ? 1 : 0;
                    rightFootLift = bodyBobY > 0 ? 2 : 0;
                    rightArmSwing = 1.65f;
                    leftArmSwing = 1.25f;
                    ballBounceY = 3;
                    throwFlash = (frame == 1 || frame == 2);
                    break;
                }
                case AnimKind.Hurt:
                {
                    bodyBobY = -1;
                    torsoLeanX = -2;
                    leftArmSwing = -0.7f;
                    rightArmSwing = -0.6f;
                    break;
                }
                case AnimKind.Sleep:
                {
                    bodyBobY = -3;
                    leftFootLift = 0;
                    rightFootLift = 0;
                    rightArmSwing = -0.2f;
                    break;
                }
            }

            int cx = 16 + torsoLeanX;
            int baseY = 7 + bodyBobY;

            // 1. Back Layer: Backpack when facing Up / UpRight / Right, and Back Arm
            if (canonicalDir == PMDDirection.Up)
            {
                DrawArm(buf, cx - 6, baseY + 11, leftArmSwing, false, canonicalDir, false, 0, false);
                DrawArm(buf, cx + 6, baseY + 11, rightArmSwing, true, canonicalDir, true, ballBounceY, throwFlash);
            }
            else if (canonicalDir == PMDDirection.Right || canonicalDir == PMDDirection.DownRight || canonicalDir == PMDDirection.UpRight)
            {
                // Back arm (left arm is behind torso when facing right)
                DrawArm(buf, cx - 2, baseY + 11, leftArmSwing, false, canonicalDir, false, 0, false);
            }

            if (canonicalDir == PMDDirection.Right || canonicalDir == PMDDirection.UpRight || canonicalDir == PMDDirection.DownRight)
            {
                int packX = canonicalDir == PMDDirection.Right ? cx - 5 : cx - 4;
                DrawRectOutlined(buf, packX, baseY + 8, 4, 8, ColPackGreen, ColPackShade);
                SetPx(buf, packX + 1, baseY + 13, ColGold);
            }

            // 2. Articulated Legs & Sneakers
            DrawLegs(buf, cx, 7 + (kind == AnimKind.Hop ? bodyBobY : 0), canonicalDir, leftLegPhase, rightLegPhase, leftFootLift, rightFootLift, kind == AnimKind.Sleep);

            // 3. Torso / Trainer Jacket & Belt
            DrawTorso(buf, cx, baseY + 7, canonicalDir);

            // If facing Up, Backpack sits on top of the back of the jacket
            if (canonicalDir == PMDDirection.Up)
            {
                DrawRectOutlined(buf, cx - 4, baseY + 8, 9, 8, ColPackGreen, ColPackShade);
                DrawHLine(buf, cx - 3, cx + 3, baseY + 13, ColGold);
                SetPx(buf, cx, baseY + 11, ColWhite);
            }

            // 4. Front Arms + Held Poké Ball
            if (canonicalDir == PMDDirection.Down)
            {
                DrawArm(buf, cx - 6, baseY + 11, leftArmSwing, false, canonicalDir, false, 0, false);
                DrawArm(buf, cx + 6, baseY + 11, rightArmSwing, true, canonicalDir, true, ballBounceY, throwFlash);
            }
            else if (canonicalDir != PMDDirection.Up)
            {
                int frontArmX = canonicalDir == PMDDirection.Right ? cx + 1 : cx + 4;
                DrawArm(buf, frontArmX, baseY + 11, rightArmSwing, true, canonicalDir, true, ballBounceY, throwFlash);
            }

            // 5. Head, Spiky Hair & Iconic Red Trainer Cap
            DrawHeadAndCap(buf, cx, baseY + 15, canonicalDir, kind);

            // 6. Throw / Command Motion Arc on Attack frames
            if (throwFlash && kind == AnimKind.Attack)
            {
                DrawThrowSlashArc(buf, cx, baseY + 12, canonicalDir);
            }

            // Apply horizontal flip for Left / DownLeft / UpLeft while preserving crisp pixel coordinates
            if (mirrorX)
            {
                FlipBufferHorizontally(buf);
            }
        }

        private static void DrawLegs(
            Color[] buf,
            int cx,
            int footBaseY,
            PMDDirection dir,
            float leftPhase,
            float rightPhase,
            int leftLift,
            int rightLift,
            bool kneeling)
        {
            if (kneeling)
            {
                DrawRectOutlined(buf, cx - 4, footBaseY - 1, 4, 4, ColJeans, ColJeansShade);
                DrawRectOutlined(buf, cx + 1, footBaseY - 2, 4, 4, ColCapRed, ColWhite);
                return;
            }

            if (dir == PMDDirection.Right)
            {
                // Side profile scissor stride: legs swing horizontally along X
                int lx = cx + Mathf.RoundToInt(leftPhase * 3.6f);
                int rx = cx + Mathf.RoundToInt(rightPhase * 3.6f);
                int ly = footBaseY + leftLift;
                int ry = footBaseY + rightLift;

                // Draw further leg first, closer leg second
                if (leftPhase < rightPhase)
                {
                    DrawSingleSideLeg(buf, lx, ly, true);
                    DrawSingleSideLeg(buf, rx, ry, false);
                }
                else
                {
                    DrawSingleSideLeg(buf, rx, ry, true);
                    DrawSingleSideLeg(buf, lx, ly, false);
                }
                return;
            }

            // Front / Back / Diagonal stride
            int leftX = cx - 4 + (dir == PMDDirection.DownRight || dir == PMDDirection.UpRight ? Mathf.RoundToInt(leftPhase * 1.4f) : 0);
            int rightX = cx + 1 + (dir == PMDDirection.DownRight || dir == PMDDirection.UpRight ? Mathf.RoundToInt(rightPhase * 1.4f) : 0);

            int leftY = footBaseY + leftLift + Mathf.RoundToInt(-leftPhase * 1.2f);
            int rightY = footBaseY + rightLift + Mathf.RoundToInt(-rightPhase * 1.2f);

            DrawSingleFrontBackLeg(buf, leftX, leftY, leftLift > 0, dir == PMDDirection.Up);
            DrawSingleFrontBackLeg(buf, rightX, rightY, rightLift > 0, dir == PMDDirection.Up);
        }

        private static void DrawSingleFrontBackLeg(Color[] buf, int x, int y, bool lifted, bool isBackView)
        {
            // Jeans leg (width 4, height 5)
            DrawRectOutlined(buf, x, y + 2, 4, 5, ColJeans, ColJeansShade);
            SetPx(buf, x + 1, y + 4, ColJeansLight);

            // Sneaker (width 4, height 3)
            DrawRectOutlined(buf, x, y, 4, 3, ColCapRed, ColCapShade);
            SetPx(buf, x + 1, y + 1, ColWhite);
            SetPx(buf, x + 2, y + 1, ColWhite);

            // Dark shoe sole when foot lifts during walk/run
            if (lifted || isBackView)
            {
                SetPx(buf, x + 1, y, ColOutline);
                SetPx(buf, x + 2, y, ColOutline);
            }
        }

        private static void DrawSingleSideLeg(Color[] buf, int x, int y, bool isBackLeg)
        {
            Color jeanMain = isBackLeg ? ColJeansShade : ColJeans;
            DrawRectOutlined(buf, x - 1, y + 2, 4, 5, jeanMain, ColJeansShade);

            // Sneaker pointing right (width 5, height 3)
            DrawRectOutlined(buf, x - 1, y, 5, 3, ColCapRed, ColCapShade);
            SetPx(buf, x + 1, y + 1, ColWhite);
            SetPx(buf, x + 2, y + 1, ColWhite);
        }

        private static void DrawTorso(Color[] buf, int cx, int y, PMDDirection dir)
        {
            int w = dir == PMDDirection.Right ? 8 : 11;
            int x = cx - (w / 2);

            DrawRectOutlined(buf, x, y, w, 8, ColJacketBlue, ColJacketShade);

            // White & gold collar at top of jacket
            DrawHLine(buf, x + 1, x + w - 2, y + 6, ColWhite);

            if (dir == PMDDirection.Down || dir == PMDDirection.DownRight)
            {
                int center = dir == PMDDirection.Down ? cx : cx + 1;
                // Dark inner shirt & gold zipper line
                for (int py = y + 1; py <= y + 6; py++)
                {
                    SetPx(buf, center, py, ColShirtDark);
                    SetPx(buf, center - 1, py, ColGold);
                }

                // Trainer Belt + Belt Buckle + Mini Belt Poké Ball
                DrawHLine(buf, x + 1, x + w - 2, y + 1, ColOutline);
                SetPx(buf, center, y + 1, ColGold);
                SetPx(buf, x + 2, y + 1, ColCapRed);
            }
            else if (dir == PMDDirection.Right)
            {
                // Side jacket stripe & belt
                for (int py = y + 2; py <= y + 5; py++)
                    SetPx(buf, cx + 1, py, ColWhite);
                DrawHLine(buf, x + 1, x + w - 2, y + 1, ColOutline);
                SetPx(buf, cx + 1, y + 1, ColCapRed);
            }
        }

        private static void DrawArm(
            Color[] buf,
            int shoulderX,
            int shoulderY,
            float swing,
            bool isRightArm,
            PMDDirection dir,
            bool holdsPokeBall,
            int ballBounceY,
            bool glowBall)
        {
            int dx = 0;
            int dy = -3;

            if (dir == PMDDirection.Right)
            {
                dx = Mathf.RoundToInt(swing * 3.2f);
                dy = -2 + Mathf.RoundToInt(Mathf.Abs(swing) * 1.2f);
                if (swing > 1.3f)
                {
                    dx = 5;
                    dy = 1;
                }
            }
            else
            {
                dx = isRightArm ? 1 : -1;
                dy = -3 + Mathf.RoundToInt(swing * 2.2f);
                if (swing > 1.3f)
                {
                    dy = 2;
                }
            }

            int handX = shoulderX + dx;
            int handY = shoulderY + dy;

            // Sleeve
            DrawRectOutlined(buf, Mathf.Min(shoulderX, handX) - 1, Mathf.Min(shoulderY, handY), 3, 4, ColJacketBlue, ColJacketShade);

            // Red fingerless trainer glove & hand
            SetPx(buf, handX, handY, ColCapRed);
            SetPx(buf, handX, handY - 1, ColSkin);
            SetPxOutline(buf, handX, handY);
            SetPxOutline(buf, handX, handY - 1);

            if (holdsPokeBall)
            {
                int bx = handX + (dir == PMDDirection.Right ? 2 : (isRightArm ? 2 : -2));
                int by = handY + ballBounceY;
                DrawMiniPokeBall(buf, bx, by, glowBall);
            }
        }

        private static void DrawMiniPokeBall(Color[] buf, int cx, int cy, bool glowing)
        {
            // Crisp 5x5 pixel Poké Ball held in the Trainer's hand
            for (int dy = -2; dy <= 2; dy++)
            {
                for (int dx = -2; dx <= 2; dx++)
                {
                    int distSq = dx * dx + dy * dy;
                    if (distSq > 5)
                        continue;

                    int px = cx + dx;
                    int py = cy + dy;

                    if (distSq >= 4)
                    {
                        SetPx(buf, px, py, ColOutline);
                    }
                    else if (dx == 0 && dy == 0)
                    {
                        SetPx(buf, px, py, glowing ? ColCyanGlow : ColGold);
                    }
                    else if (dy == 0)
                    {
                        SetPx(buf, px, py, ColOutline);
                    }
                    else if (dy > 0)
                    {
                        SetPx(buf, px, py, ColCapRed);
                    }
                    else
                    {
                        SetPx(buf, px, py, ColWhite);
                    }
                }
            }
        }

        private static void DrawHeadAndCap(Color[] buf, int cx, int headY, PMDDirection dir, AnimKind kind)
        {
            // Head base (10x9)
            DrawRectOutlined(buf, cx - 5, headY, 10, 9, ColSkin, ColSkinShade);

            // Spiky dark hair around ears and nape
            for (int px = cx - 5; px <= cx + 4; px++)
            {
                SetPx(buf, px, headY + 4, ColHair);
            }
            SetPx(buf, cx - 6, headY + 3, ColHair);
            SetPx(buf, cx + 5, headY + 3, ColHair);
            SetPx(buf, cx - 5, headY + 2, ColHair);
            SetPx(buf, cx + 4, headY + 2, ColHair);

            if (dir == PMDDirection.Up || dir == PMDDirection.UpRight)
            {
                // Full spiky back hair
                DrawRectOutlined(buf, cx - 5, headY + 1, 10, 5, ColHair, ColOutline);
                SetPx(buf, cx - 2, headY + 3, ColHairLight);
                SetPx(buf, cx + 1, headY + 4, ColHairLight);
            }
            else if (dir == PMDDirection.Right)
            {
                // Side profile: hair at back of head, eye on right
                for (int py = headY + 1; py <= headY + 5; py++)
                {
                    SetPx(buf, cx - 4, py, ColHair);
                    SetPx(buf, cx - 3, py, ColHair);
                }
                int eyeX = cx + 2;
                int eyeY = headY + 2;
                SetPx(buf, eyeX, eyeY + 1, kind == AnimKind.Hurt ? ColCapRed : ColOutline);
                SetPx(buf, eyeX, eyeY, ColOutline);
                SetPx(buf, eyeX - 1, eyeY + 1, ColWhite);
            }
            else
            {
                // Front & DownRight face eyes
                int shift = dir == PMDDirection.DownRight ? 1 : 0;
                int leftEyeX = cx - 3 + shift;
                int rightEyeX = cx + 2 + shift;
                int eyeY = headY + 2;

                if (kind == AnimKind.Sleep)
                {
                    SetPx(buf, leftEyeX, eyeY, ColOutline);
                    SetPx(buf, leftEyeX + 1, eyeY, ColOutline);
                    SetPx(buf, rightEyeX, eyeY, ColOutline);
                    SetPx(buf, rightEyeX - 1, eyeY, ColOutline);
                }
                else
                {
                    SetPx(buf, leftEyeX, eyeY, ColOutline);
                    SetPx(buf, leftEyeX, eyeY + 1, ColOutline);
                    SetPx(buf, leftEyeX - 1, eyeY + 1, ColWhite);

                    SetPx(buf, rightEyeX, eyeY, ColOutline);
                    SetPx(buf, rightEyeX, eyeY + 1, ColOutline);
                    SetPx(buf, rightEyeX - 1, eyeY + 1, ColWhite);

                    if (kind == AnimKind.Attack || kind == AnimKind.Hop)
                    {
                        // Determined mouth / grin
                        SetPx(buf, cx + shift, headY + 1, ColCapRed);
                    }
                }
            }

            // Iconic Red & White Trainer Cap on top (y = headY + 5 .. headY + 11)
            DrawRectOutlined(buf, cx - 5, headY + 5, 10, 5, ColCapRed, ColCapShade);

            if (dir == PMDDirection.Down || dir == PMDDirection.DownRight)
            {
                int badgeX = cx + (dir == PMDDirection.DownRight ? 1 : 0);
                // White front cap emblem
                SetPx(buf, badgeX - 1, headY + 7, ColWhite);
                SetPx(buf, badgeX, headY + 7, ColWhite);
                SetPx(buf, badgeX + 1, headY + 7, ColWhite);
                SetPx(buf, badgeX, headY + 8, ColWhite);

                // Front visor brim
                DrawHLine(buf, cx - 4, cx + 4, headY + 5, ColCapRed);
                DrawHLine(buf, cx - 4, cx + 4, headY + 4, ColOutline);
                SetPx(buf, badgeX - 1, headY + 5, ColWhite);
                SetPx(buf, badgeX, headY + 5, ColWhite);
            }
            else if (dir == PMDDirection.Right || dir == PMDDirection.UpRight)
            {
                // Side visor jutting out to the right (+X)
                SetPx(buf, cx + 1, headY + 7, ColWhite);
                SetPx(buf, cx + 2, headY + 7, ColWhite);
                for (int vx = cx + 2; vx <= cx + 7; vx++)
                {
                    SetPx(buf, vx, headY + 6, ColCapRed);
                    SetPx(buf, vx, headY + 5, ColOutline);
                }
                SetPx(buf, cx + 7, headY + 6, ColOutline);
            }
            else if (dir == PMDDirection.Up)
            {
                // Back cap strap
                SetPx(buf, cx - 1, headY + 5, ColWhite);
                SetPx(buf, cx, headY + 5, ColGold);
                SetPx(buf, cx + 1, headY + 5, ColWhite);
            }
        }

        private static void DrawThrowSlashArc(Color[] buf, int cx, int cy, PMDDirection dir)
        {
            int ax = dir == PMDDirection.Right ? cx + 8 : cx + 6;
            for (int dy = -4; dy <= 5; dy++)
            {
                int curveX = ax + (3 - Mathf.Abs(dy) / 2);
                SetPx(buf, curveX, cy + dy, ColCyanGlow);
                SetPx(buf, curveX - 1, cy + dy, ColWhite);
            }
        }

        // =========================================================================================
        // 40x40 PIXEL PORTRAIT GENERATOR (Normal, Determined, Pain, Happy)
        // =========================================================================================
        private static void BuildPortraits(PMDCharacterData data)
        {
            string[] emotions = { "Normal", "Determined", "Pain", "Happy" };
            for (int i = 0; i < emotions.Length; i++)
            {
                string emo = emotions[i];
                Sprite sp = CreateTrainerPortrait(emo);
                data.portraits.Add(new PMDPortraitEntry
                {
                    emotionName = emo,
                    sprite = sp
                });
            }
        }

        private static Sprite CreateTrainerPortrait(string emotion)
        {
            const int size = 40;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = $"Trainer_Portrait_{emotion}"
            };

            Color[] buf = new Color[size * size];
            Color bgDark = new Color(0.12f, 0.18f, 0.30f, 1f);
            Color bgLight = new Color(0.18f, 0.28f, 0.46f, 1f);

            // Striped pixel portrait background
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool border = x == 0 || y == 0 || x == size - 1 || y == size - 1;
                    buf[y * size + x] = border ? ColOutline : (((x + y) / 4) % 2 == 0 ? bgDark : bgLight);
                }
            }

            // Shoulders / Blue Jacket Collar
            DrawRectGeneral(buf, size, size, 8, 2, 24, 11, ColJacketBlue, ColJacketShade, ColOutline);
            for (int y = 2; y < 12; y++)
            {
                buf[y * size + 19] = ColShirtDark;
                buf[y * size + 20] = ColShirtDark;
                buf[y * size + 18] = ColGold;
                buf[y * size + 21] = ColGold;
            }

            // Face (x: 11..28, y: 11..26)
            DrawRectGeneral(buf, size, size, 11, 11, 18, 15, ColSkin, ColSkinShade, ColOutline);

            // Spiky dark hair frames
            for (int y = 14; y <= 23; y++)
            {
                buf[y * size + 10] = ColHair;
                buf[y * size + 11] = ColHair;
                buf[y * size + 28] = ColHair;
                buf[y * size + 29] = ColHair;
            }

            // Eyes & Expression
            int eyeY = 17;
            if (emotion == "Pain")
            {
                // >< squinting eyes
                buf[(eyeY + 1) * size + 15] = ColOutline;
                buf[eyeY * size + 16] = ColOutline;
                buf[(eyeY - 1) * size + 15] = ColOutline;

                buf[(eyeY + 1) * size + 24] = ColOutline;
                buf[eyeY * size + 23] = ColOutline;
                buf[(eyeY - 1) * size + 24] = ColOutline;
            }
            else
            {
                for (int ey = eyeY; ey <= eyeY + 2; ey++)
                {
                    buf[ey * size + 15] = ColOutline;
                    buf[ey * size + 16] = ColOutline;
                    buf[ey * size + 23] = ColOutline;
                    buf[ey * size + 24] = ColOutline;
                }
                buf[(eyeY + 2) * size + 15] = ColWhite;
                buf[(eyeY + 2) * size + 23] = ColWhite;

                if (emotion == "Determined")
                {
                    buf[(eyeY + 3) * size + 14] = ColOutline;
                    buf[(eyeY + 3) * size + 15] = ColOutline;
                    buf[(eyeY + 3) * size + 24] = ColOutline;
                    buf[(eyeY + 3) * size + 25] = ColOutline;
                }
            }

            // Mouth
            for (int mx = 18; mx <= 21; mx++)
                buf[13 * size + mx] = emotion == "Determined" || emotion == "Happy" ? ColCapRed : ColOutline;

            // Iconic Red Cap (x: 9..30, y: 23..35)
            DrawRectGeneral(buf, size, size, 9, 23, 22, 12, ColCapRed, ColCapShade, ColOutline);
            // White Poké emblem on cap
            for (int bx = 16; bx <= 23; bx++)
            {
                buf[27 * size + bx] = ColWhite;
                buf[28 * size + bx] = ColWhite;
            }
            // Cap brim shadow
            for (int bx = 10; bx <= 29; bx++)
                buf[23 * size + bx] = ColOutline;

            // Mini Poké Ball held near right corner (x: 31, y: 10)
            for (int dy = -3; dy <= 3; dy++)
            {
                for (int dx = -3; dx <= 3; dx++)
                {
                    int d2 = dx * dx + dy * dy;
                    if (d2 > 10) continue;
                    int px = 32 + dx;
                    int py = 9 + dy;
                    if (px <= 0 || px >= size - 1 || py <= 0 || py >= size - 1) continue;
                    buf[py * size + px] = d2 >= 7 ? ColOutline : (dx == 0 && dy == 0 ? ColGold : (dy == 0 ? ColOutline : (dy > 0 ? ColCapRed : ColWhite)));
                }
            }

            tex.SetPixels(buf);
            tex.Apply(false);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 40f);
        }

        // =========================================================================================
        // LOW-LEVEL PIXEL DRAWING HELPERS
        // =========================================================================================
        private static void SetPx(Color[] buf, int x, int y, Color c)
        {
            if (x >= 0 && x < FrameW && y >= 0 && y < FrameH)
                buf[y * FrameW + x] = c;
        }

        private static void SetPxOutline(Color[] buf, int x, int y)
        {
            int[] ox = { -1, 1, 0, 0 };
            int[] oy = { 0, 0, -1, 1 };
            for (int i = 0; i < 4; i++)
            {
                int nx = x + ox[i];
                int ny = y + oy[i];
                if (nx >= 0 && nx < FrameW && ny >= 0 && ny < FrameH && buf[ny * FrameW + nx].a < 0.01f)
                {
                    buf[ny * FrameW + nx] = ColOutline;
                }
            }
        }

        private static void DrawHLine(Color[] buf, int x0, int x1, int y, Color c)
        {
            for (int x = x0; x <= x1; x++)
                SetPx(buf, x, y, c);
        }

        private static void DrawRectOutlined(Color[] buf, int x, int y, int w, int h, Color fill, Color shade)
        {
            for (int py = y; py < y + h; py++)
            {
                for (int px = x; px < x + w; px++)
                {
                    bool border = (px == x || px == x + w - 1 || py == y || py == y + h - 1);
                    if (border)
                    {
                        SetPx(buf, px, py, ColOutline);
                    }
                    else
                    {
                        bool isShaded = (px >= x + w - 2) || (py == y + 1);
                        SetPx(buf, px, py, isShaded ? shade : fill);
                    }
                }
            }
        }

        private static void DrawRectGeneral(Color[] buf, int bw, int bh, int x, int y, int w, int h, Color fill, Color shade, Color outline)
        {
            for (int py = y; py < y + h; py++)
            {
                for (int px = x; px < x + w; px++)
                {
                    if (px < 0 || px >= bw || py < 0 || py >= bh) continue;
                    bool border = (px == x || px == x + w - 1 || py == y || py == y + h - 1);
                    buf[py * bw + px] = border ? outline : ((px >= x + w - 3) ? shade : fill);
                }
            }
        }

        private static void FlipBufferHorizontally(Color[] buf)
        {
            for (int y = 0; y < FrameH; y++)
            {
                int row = y * FrameW;
                for (int x = 0; x < FrameW / 2; x++)
                {
                    int opp = FrameW - 1 - x;
                    Color tmp = buf[row + x];
                    buf[row + x] = buf[row + opp];
                    buf[row + opp] = tmp;
                }
            }
        }
    }
}
