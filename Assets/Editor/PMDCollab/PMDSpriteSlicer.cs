using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace POKE.PMDCollab.Editor
{
    /// <summary>
    /// Slices PMDCollab animation sprite sheets and 40x40 portrait sheets using Unity's ISpriteEditorDataProvider
    /// with mandatory capability checks and Unity 2021.2+/6.0+ ISpriteNameFileIdDataProvider registration.
    /// </summary>
    public static class PMDSpriteSlicer
    {
        public static readonly string[] StandardEmotions =
        {
            "Normal", "Happy", "Pain", "Angry", "Worried",
            "Sad", "Crying", "Shouting", "TearyEyed", "Determined",
            "Joyous", "Inspired", "Surprised", "Dizzy", "Special0",
            "Special1", "Sigh", "Stunned", "Special2", "Special3"
        };

        /// <summary>
        /// Configures pixel-art TextureImporter settings and slices an action sprite sheet (*-Anim.png)
        /// into directional frames based on FrameWidth and FrameHeight from AnimData.xml.
        /// </summary>
        public static List<PMDDirectionFrames> SliceAnimationSheet(
            string assetPath,
            string animName,
            int frameWidth,
            int frameHeight,
            int pixelsPerUnit = 24)
        {
            var result = new List<PMDDirectionFrames>();
            if (frameWidth <= 0 || frameHeight <= 0)
                return result;

            var texImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (texImporter == null)
                return result;

            ConfigurePixelArtImporter(texImporter, pixelsPerUnit, readable: false);

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (tex == null)
                return result;

            texImporter.GetSourceTextureWidthAndHeight(out int sourceWidth, out int sourceHeight);
            if (sourceWidth <= 0 || sourceHeight <= 0)
            {
                sourceWidth = tex.width;
                sourceHeight = tex.height;
            }

            int columns = sourceWidth / frameWidth;
            int rows = sourceHeight / frameHeight;
            if (columns <= 0 || rows <= 0)
                return result;

            var specs = new List<(string name, Rect rect, Vector2 pivot)>(columns * rows);

            for (int row = 0; row < rows; row++)
            {
                string dirLabel = rows >= 8 && row < 8
                    ? ((PMDDirection)row).ToString()
                    : $"Row{row}";

                // Row 0 in PMDCollab is at the top of the PNG; Unity (0,0) is bottom-left
                int y = sourceHeight - ((row + 1) * frameHeight);

                for (int col = 0; col < columns; col++)
                {
                    int x = col * frameWidth;
                    string spriteName = $"{animName}_{dirLabel}_{col:D2}";
                    var rect = new Rect(x, y, frameWidth, frameHeight);
                    specs.Add((spriteName, rect, new Vector2(0.5f, 0.5f)));
                }
            }

            ApplySpriteRectsSafe(texImporter, specs);

            // Load sliced sub-asset Sprites and map them back to PMDDirectionFrames
            Dictionary<string, Sprite> spriteByName = AssetDatabase
                .LoadAllAssetsAtPath(assetPath)
                .OfType<Sprite>()
                .ToDictionary(s => s.name, s => s);

            int dirCount = Mathf.Min(rows, 8);
            for (int row = 0; row < dirCount; row++)
            {
                var dirEnum = (PMDDirection)row;
                string dirLabel = rows >= 8 ? dirEnum.ToString() : $"Row{row}";

                var dirGroup = new PMDDirectionFrames
                {
                    direction = dirEnum
                };

                for (int col = 0; col < columns; col++)
                {
                    string spriteName = $"{animName}_{dirLabel}_{col:D2}";
                    if (spriteByName.TryGetValue(spriteName, out Sprite sp))
                    {
                        dirGroup.frames.Add(sp);
                    }
                }

                if (dirGroup.frames.Count > 0)
                {
                    result.Add(dirGroup);
                }
            }

            return result;
        }

        /// <summary>
        /// Slices a 200x320 (or 200x160) PMDCollab portrait sheet into 40x40 emotion portraits,
        /// skipping empty/transparent cells automatically.
        /// </summary>
        public static List<PMDPortraitEntry> SlicePortraitSheet(string assetPath, int pixelsPerUnit = 40)
        {
            var entries = new List<PMDPortraitEntry>();
            var texImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (texImporter == null)
                return entries;

            // Temporarily readable so we can skip blank 40x40 emotion slots
            ConfigurePixelArtImporter(texImporter, pixelsPerUnit, readable: true);

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (tex == null)
                return entries;

            texImporter.GetSourceTextureWidthAndHeight(out int sourceWidth, out int sourceHeight);
            if (sourceWidth <= 0 || sourceHeight <= 0)
            {
                sourceWidth = tex.width;
                sourceHeight = tex.height;
            }

            const int cellSize = 40;
            int cols = sourceWidth / cellSize;
            int rows = sourceHeight / cellSize;

            var specs = new List<(string name, Rect rect, Vector2 pivot)>();
            var emotionOrder = new List<string>();

            for (int row = 0; row < rows; row++)
            {
                int y = sourceHeight - ((row + 1) * cellSize);
                for (int col = 0; col < cols; col++)
                {
                    int x = col * cellSize;
                    if (!HasVisiblePixels(tex, x, y, cellSize, cellSize))
                        continue;

                    int slotIndex = row * 5 + col;
                    string emotionName;
                    if (slotIndex < StandardEmotions.Length)
                    {
                        emotionName = StandardEmotions[slotIndex];
                    }
                    else if (slotIndex - 20 >= 0 && slotIndex - 20 < StandardEmotions.Length)
                    {
                        emotionName = StandardEmotions[slotIndex - 20] + "_Flip";
                    }
                    else
                    {
                        emotionName = $"Emotion_{slotIndex:D2}";
                    }

                    specs.Add(($"Portrait_{emotionName}", new Rect(x, y, cellSize, cellSize), new Vector2(0.5f, 0.5f)));
                    emotionOrder.Add(emotionName);
                }
            }

            ApplySpriteRectsSafe(texImporter, specs);

            Dictionary<string, Sprite> spriteByName = AssetDatabase
                .LoadAllAssetsAtPath(assetPath)
                .OfType<Sprite>()
                .ToDictionary(s => s.name, s => s);

            foreach (string emotion in emotionOrder)
            {
                if (spriteByName.TryGetValue($"Portrait_{emotion}", out Sprite sp))
                {
                    entries.Add(new PMDPortraitEntry
                    {
                        emotionName = emotion,
                        sprite = sp
                    });
                }
            }

            return entries;
        }

        private static bool HasVisiblePixels(Texture2D tex, int startX, int startY, int width, int height)
        {
            if (tex == null || !tex.isReadable)
                return true;

            Color[] pixels = tex.GetPixels(startX, startY, width, height);
            for (int i = 0; i < pixels.Length; i++)
            {
                if (pixels[i].a > 0.01f)
                    return true;
            }
            return false;
        }

        private static void ConfigurePixelArtImporter(TextureImporter importer, int pixelsPerUnit, bool readable)
        {
            bool changed = false;

            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                changed = true;
            }
            if (importer.spriteImportMode != SpriteImportMode.Multiple)
            {
                importer.spriteImportMode = SpriteImportMode.Multiple;
                changed = true;
            }
            if (importer.filterMode != FilterMode.Point)
            {
                importer.filterMode = FilterMode.Point;
                changed = true;
            }
            if (importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                changed = true;
            }
            if (importer.maxTextureSize < 8192)
            {
                importer.maxTextureSize = 8192;
                changed = true;
            }
            if (Mathf.Abs(importer.spritePixelsPerUnit - pixelsPerUnit) > 0.01f)
            {
                importer.spritePixelsPerUnit = pixelsPerUnit;
                changed = true;
            }
            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                changed = true;
            }
            if (importer.isReadable != readable)
            {
                importer.isReadable = readable;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
            }
        }

        /// <summary>
        /// Safe Core Pattern for ISpriteEditorDataProvider with mandatory capability validation.
        /// </summary>
        private static void ApplySpriteRectsSafe(
            AssetImporter importer,
            List<(string name, Rect rect, Vector2 pivot)> specs)
        {
            var factory = new SpriteDataProviderFactories();
            factory.Init();

            var dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
            if (dataProvider == null)
            {
                throw new Exception($"ISpriteEditorDataProvider not supported for {importer.assetPath}. Operation aborted.");
            }

            dataProvider.InitSpriteEditorDataProvider();

            // Preserve existing GUIDs by name if re-slicing
            Dictionary<string, GUID> existingGuids = dataProvider
                .GetSpriteRects()
                .ToDictionary(r => r.name, r => r.spriteID);

            var spriteRects = new SpriteRect[specs.Count];
            var nameFileIdPairs = new List<SpriteNameFileIdPair>(specs.Count);

            for (int i = 0; i < specs.Count; i++)
            {
                var spec = specs[i];
                GUID guid = existingGuids.TryGetValue(spec.name, out GUID existing)
                    ? existing
                    : GUID.Generate();

                spriteRects[i] = new SpriteRect
                {
                    name = spec.name,
                    spriteID = guid,
                    rect = spec.rect,
                    alignment = (SpriteAlignment)(int)SpriteAlignment.Center,
                    pivot = spec.pivot,
                    border = Vector4.zero
                };

                nameFileIdPairs.Add(new SpriteNameFileIdPair(spec.name, guid));
            }

            dataProvider.SetSpriteRects(spriteRects);

            var nameFileIdProvider = dataProvider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            if (nameFileIdProvider != null)
            {
                nameFileIdProvider.SetNameFileIdPairs(nameFileIdPairs);
            }

            dataProvider.Apply();
            importer.SaveAndReimport();
        }
    }
}

