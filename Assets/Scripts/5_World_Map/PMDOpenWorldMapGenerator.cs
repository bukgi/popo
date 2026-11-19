using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace POKE.PMDCollab.OpenWorld
{
    public enum RoomEntranceSide
    {
        Center,
        North,
        South,
        East,
        West,
        Stairs,
        ReturnFromIndoor
    }

    public enum RegionBiomeStyle
    {
        MeadowVillage,
        DeepWoodland,
        RedRockCanyon,
        TropicalArchipelago,
        SakuraSanctuary,
        GlacialHighlands,
        AutumnGuildHighlands,
        StarlightApex,
        IndoorRoom
    }

    [Serializable]
    public class PMDMapZoneInfo
    {
        public string zoneId;
        public string displayName;
        public string mapFileName;
        public string frontOverlayFileName;
        public Vector2 worldCenter;
        public Vector2 worldSize;
        public Vector2 safeSpawnOffset;
        public string[] preferredSpecies;
        public bool hasHostileSpawns;
        public bool isIndoor;
        public RegionBiomeStyle biomeStyle;
        public int northZoneIndex;
        public int southZoneIndex;
        public int eastZoneIndex;
        public int westZoneIndex;
        public int baseSeed;
    }

    [Serializable]
    public struct ProceduralClearing
    {
        public Vector2 center;
        public float radiusX;
        public float radiusY;
        public string clearingName;
        public bool isCentralHub;
    }

    /// <summary>
    /// Generates Massive Procedurally-Generated Regions ("Vùng Đất Lớn 96x72") with Room-by-Room Transitions.
    /// Only ONE Region is loaded at a time, centered at (0, 0), with 9 interconnected clearings, winding roads,
    /// meandering rivers & wooden bridges, 4 directional room gateways, dungeon stairs, and full BoxCollider2D obstacles.
    /// </summary>
    [ExecuteAlways]
    public class PMDOpenWorldMapGenerator : MonoBehaviour
    {
        public static PMDOpenWorldMapGenerator Instance { get; private set; }

        private const float MapPPU = 24f;
        private const int LayoutVersion = 5;

        // Massive Procedural Room Size: 96 x 72 world units (approx 4.7x larger in area than TownBase!)
        public const int RoomGridWidth = 96;
        public const int RoomGridHeight = 72;
        public const float RoomHalfWidth = RoomGridWidth * 0.5f;   // 48.0f
        public const float RoomHalfHeight = RoomGridHeight * 0.5f; // 36.0f

        public enum BiomeTreeType
        {
            MeadowGreen,
            WoodlandMoss,
            DeepTeal,
            SakuraPink,
            GoldenAutumn,
            FrostTippedPine,
            GlacialSnow
        }

        public enum CustomPropType
        {
            BoulderMeadow,
            BoulderCanyon,
            BoulderSnow,
            BoulderPeak,
            LanternPost,
            BridgeHorizontal,
            BridgeVertical,
            BushMeadow,
            BushAutumn,
            BushTeal,
            BushSnow
        }

        private enum RoomTileType : byte
        {
            TreeOrCliffWall = 0,
            Water = 1,
            Grass = 2,
            Road = 3,
            Bridge = 4
        }

        [SerializeField] private int builtLayoutVersion;
        [SerializeField] private int activeZoneIndex;
        [SerializeField] private int currentSeed = 202601;

        private readonly List<PMDMapZoneInfo> _zones = new List<PMDMapZoneInfo>();
        private readonly List<PMDWorldItemPickup> _spawnedItems = new List<PMDWorldItemPickup>();
        private readonly List<ProceduralClearing> _activeClearings = new List<ProceduralClearing>();

        private RoomTileType[,] _currentGrid;
        private Vector2 _pendingOutdoorReturnPos = new Vector2(0f, -2.5f);
        private int _lastOutdoorZoneIndex;
        private int _lastOutdoorSeed = 202601;
        private Texture2D _activeRoomCanvasTexture;

        private Transform _mapsContainer;
        private Transform _objectsContainer;
        private Transform _itemsContainer;
        private Transform _collidersContainer;

        private static readonly Dictionary<BiomeTreeType, Texture2D> CachedTreeTextures = new Dictionary<BiomeTreeType, Texture2D>();
        private static readonly Dictionary<CustomPropType, Texture2D> CachedCustomProps = new Dictionary<CustomPropType, Texture2D>();
        private static readonly Dictionary<string, Sprite[]> CachedAnimatedFrames = new Dictionary<string, Sprite[]>();

        private static readonly float[,] Bayer4x4 =
        {
            {  0f / 16f,  8f / 16f,  2f / 16f, 10f / 16f },
            { 12f / 16f,  4f / 16f, 14f / 16f,  6f / 16f },
            {  3f / 16f, 11f / 16f,  1f / 16f,  9f / 16f },
            { 15f / 16f,  7f / 16f, 13f / 16f,  5f / 16f }
        };

        public IReadOnlyList<PMDMapZoneInfo> Zones => _zones;
        public IReadOnlyList<PMDWorldItemPickup> SpawnedItems => _spawnedItems;
        public IReadOnlyList<ProceduralClearing> ActiveClearings => _activeClearings;
        public int ActiveZoneIndex => activeZoneIndex;
        public int CurrentSeed => currentSeed;

        public PMDMapZoneInfo ActiveZone =>
            (_zones != null && _zones.Count > 0)
                ? _zones[Mathf.Clamp(activeZoneIndex, 0, _zones.Count - 1)]
                : null;

        public static string LastTransitionBannerText { get; private set; } = string.Empty;
        public static float LastTransitionBannerTime { get; private set; } = -99f;

        public int LastOutdoorZoneIndex => _lastOutdoorZoneIndex;
        public int LastOutdoorSeed => _lastOutdoorSeed;

        private void Awake()
        {
            Instance = this;
        }

        public Sprite CreateObjectSprite(string fileName)
        {
            Texture2D tex = LoadObjectTexture(fileName);
            if (tex == null) return null;
            int frameHeight = tex.height;
            int frameWidth = (tex.width > tex.height && tex.width % tex.height == 0) ? tex.height : tex.width;
            return Sprite.Create(tex, new Rect(0, 0, frameWidth, frameHeight), new Vector2(0.5f, 0.15f), MapPPU);
        }

        public void SpawnDroppedItem(string itemFile, Vector2 pos)
        {
            pos = FindNearestWalkablePosition(pos);
            SpawnItemAt(itemFile, pos);
            PMDVfxPlayer.SpawnVfx("Circle_Yellow_Out.None", pos, 1.0f, 24f);
        }

        private void OnEnable()
        {
            Instance = this;
            if (_zones == null || _zones.Count == 0)
            {
                BuildZoneDefinitions();
            }
        }

        public bool HasValidSprites()
        {
            if (builtLayoutVersion != LayoutVersion || _zones == null || _zones.Count == 0 || transform.childCount < 4)
                return false;

            if (transform.Find("4_Map_Terrain_Obstacles_Colliders") == null)
                return false;

            var sr = GetComponentInChildren<SpriteRenderer>();
            return sr != null && sr.sprite != null && sr.sprite.texture != null;
        }

        public void SetPendingOutdoorReturnPosition(Vector2 pos)
        {
            _pendingOutdoorReturnPos = pos;
        }

        /// <summary>
        /// Builds definitions and generates the active Procedural Region (Room 0 by default).
        /// </summary>
        public void GenerateWorld()
        {
            BuildZoneDefinitions();
            activeZoneIndex = Mathf.Clamp(activeZoneIndex, 0, _zones.Count - 1);
            if (currentSeed == 0)
                currentSeed = _zones[activeZoneIndex].baseSeed;

            RebuildActiveRoomGeometry();

            if (PMDAtmosphere.Instance != null && ActiveZone != null)
            {
                PMDAtmosphere.Instance.ApplyBiome(ActiveZone.biomeStyle);
            }
        }

        /// <summary>
        /// Regenerates the current Region with a brand new random seed so the layout, clearings, rivers, bridges,
        /// obstacles, items, and monsters are freshly generated!
        /// </summary>
        public void RegenerateCurrentRegionWithNewSeed()
        {
            if (_zones == null || _zones.Count == 0)
                BuildZoneDefinitions();

            var zone = ActiveZone;
            if (zone != null && zone.isIndoor)
            {
                activeZoneIndex = _lastOutdoorZoneIndex;
            }

            int newSeed = UnityEngine.Random.Range(100000, 999999);
            TransitionToRegion(activeZoneIndex, RoomEntranceSide.Center, false, newSeed);
        }

        /// <summary>
        /// Transitions into a new Region ("Vùng Đất / Phòng Mới"), unloading the previous room, procedurally generating
        /// the new 96x72 region, placing the player + recruited companions at the corresponding entrance gate,
        /// and spawning wild Pokémon for the new room.
        /// </summary>
        public void TransitionToRegion(int targetIndex, RoomEntranceSide entranceSide = RoomEntranceSide.Center, bool randomizeSeed = false, int explicitSeed = -1)
        {
            if (_zones == null || _zones.Count == 0)
                BuildZoneDefinitions();

            targetIndex = ((targetIndex % _zones.Count) + _zones.Count) % _zones.Count;
            var targetZone = _zones[targetIndex];

            if (!targetZone.isIndoor)
            {
                _lastOutdoorZoneIndex = targetIndex;
                if (explicitSeed > 0)
                {
                    currentSeed = explicitSeed;
                }
                else if (randomizeSeed)
                {
                    currentSeed = UnityEngine.Random.Range(100000, 999999);
                }
                else
                {
                    currentSeed = targetZone.baseSeed;
                }
                _lastOutdoorSeed = currentSeed;
            }
            else
            {
                currentSeed = targetZone.baseSeed;
            }

            activeZoneIndex = targetIndex;
            PMDRoomGatewayTrigger.NotifyRoomJustLoaded();

            RebuildActiveRoomGeometry();
            Physics2D.SyncTransforms();

            if (PMDAtmosphere.Instance != null)
            {
                PMDAtmosphere.Instance.ApplyBiome(targetZone.biomeStyle);
            }

            Vector2 spawnPos = GetEntranceSpawnPosition(entranceSide, targetZone);
            spawnPos = FindNearestWalkablePosition(spawnPos);

            LastTransitionBannerText = targetZone.isIndoor
                ? $"🏠 {targetZone.displayName}"
                : $"🗺️ {targetZone.displayName}  •  Map Tự Sinh (96×72 | Seed #{currentSeed})";
            LastTransitionBannerTime = Time.time;

            var manager = FindFirstObjectByType<PMDOpenWorldManager>();
            if (manager != null)
            {
                manager.OnRegionChanged(targetZone, spawnPos);
            }
            else if (PMDPlayerController.Instance != null)
            {
                PMDPlayerController.Instance.transform.position = spawnPos;
                if (Camera.main != null)
                {
                    Camera.main.transform.position = new Vector3(spawnPos.x, spawnPos.y, -10f);
                }
            }

            PMDVfxPlayer.SpawnVfx("Circle_Blue_Out.None", spawnPos, 1.45f, 28f);
            PMDVfxPlayer.SpawnFloatingText(spawnPos + new Vector2(0f, 0.7f), $"➔ {targetZone.displayName}", new Color(0.45f, 0.95f, 1f), 1.4f);
        }

        private Vector2 GetEntranceSpawnPosition(RoomEntranceSide side, PMDMapZoneInfo zone)
        {
            if (zone.isIndoor)
            {
                return new Vector2(0f, -3.2f);
            }

            switch (side)
            {
                case RoomEntranceSide.North:
                    return new Vector2(0f, RoomHalfHeight - 7.5f); // (0, +28.5)
                case RoomEntranceSide.South:
                    return new Vector2(0f, -RoomHalfHeight + 7.5f); // (0, -28.5)
                case RoomEntranceSide.East:
                    return new Vector2(RoomHalfWidth - 8.5f, 0f); // (+39.5, 0)
                case RoomEntranceSide.West:
                    return new Vector2(-RoomHalfWidth + 8.5f, 0f); // (-39.5, 0)
                case RoomEntranceSide.ReturnFromIndoor:
                    return _pendingOutdoorReturnPos;
                case RoomEntranceSide.Stairs:
                case RoomEntranceSide.Center:
                default:
                    return new Vector2(0f, -1.5f);
            }
        }

        public Vector2 FindNearestWalkablePosition(Vector2 desiredPos)
        {
            if (IsWorldPointWalkable(desiredPos) && Physics2D.OverlapCircle(desiredPos, 0.36f) == null)
                return desiredPos;

            for (float r = 0.6f; r <= 8.0f; r += 0.6f)
            {
                for (int a = 0; a < 16; a++)
                {
                    float rad = a * (Mathf.PI * 2f / 16f);
                    Vector2 candidate = desiredPos + new Vector2(Mathf.Cos(rad) * r, Mathf.Sin(rad) * r);
                    if (IsWorldPointWalkable(candidate) && Physics2D.OverlapCircle(candidate, 0.36f) == null)
                        return candidate;
                }
            }

            return Vector2.zero;
        }

        public bool IsWorldPointWalkable(Vector2 worldPos)
        {
            var zone = ActiveZone;
            if (zone != null && zone.isIndoor)
            {
                return Mathf.Abs(worldPos.x) < 5.8f && Mathf.Abs(worldPos.y) < 4.2f;
            }

            if (_currentGrid == null)
                return true;

            int gx = Mathf.FloorToInt(worldPos.x + RoomHalfWidth);
            int gy = Mathf.FloorToInt(worldPos.y + RoomHalfHeight);
            if (gx < 2 || gx >= RoomGridWidth - 2 || gy < 2 || gy >= RoomGridHeight - 2)
                return false;

            RoomTileType t = _currentGrid[gx, gy];
            return t == RoomTileType.Grass || t == RoomTileType.Road || t == RoomTileType.Bridge;
        }

        private void BuildZoneDefinitions()
        {
            _zones.Clear();

            // 0. Vùng 1: Thảo Nguyên Thị Trấn Kho Báu (Central Hub & Emerald Meadow)
            _zones.Add(new PMDMapZoneInfo
            {
                zoneId = "Vung1_ThaoNguyenKhoBau",
                displayName = "Vùng 1: Thảo Nguyên Thị Trấn (96×72)",
                mapFileName = "TownBase",
                worldCenter = Vector2.zero,
                worldSize = new Vector2(RoomGridWidth, RoomGridHeight),
                safeSpawnOffset = new Vector2(0f, -1.5f),
                preferredSpecies = new[] { "0025_Pikachu", "0133_Eevee", "0001_Bulbasaur", "0007_Squirtle", "0143_Snorlax" },
                hasHostileSpawns = false,
                biomeStyle = RegionBiomeStyle.MeadowVillage,
                northZoneIndex = 4, // SakuraSanctuary
                southZoneIndex = 3, // TropicalArchipelago
                eastZoneIndex = 1,  // DeepWoodland
                westZoneIndex = 2,  // RedRockCanyon
                baseSeed = 101888
            });

            // 1. Vùng 2: Đại Ngàn Rừng Cổ Thụ (Deep Woodland & Fairy Glades)
            _zones.Add(new PMDMapZoneInfo
            {
                zoneId = "Vung2_DaiNganRungCoThu",
                displayName = "Vùng 2: Đại Ngàn Rừng Sâu (96×72)",
                mapFileName = "ForestCamp",
                worldCenter = Vector2.zero,
                worldSize = new Vector2(RoomGridWidth, RoomGridHeight),
                safeSpawnOffset = new Vector2(0f, -1.5f),
                preferredSpecies = new[] { "0252_Treecko", "0001_Bulbasaur", "0094_Gengar", "0282_Gardevoir", "0143_Snorlax" },
                hasHostileSpawns = true,
                biomeStyle = RegionBiomeStyle.DeepWoodland,
                northZoneIndex = 5, // GlacialHighlands
                southZoneIndex = 3, // TropicalArchipelago
                eastZoneIndex = 6,  // AutumnGuildHighlands
                westZoneIndex = 0,  // MeadowVillage
                baseSeed = 202777
            });

            // 2. Vùng 3: Hẻm Núi Đá Đỏ & Cao Nguyên Gió (Red-Rock Canyon & Savanna)
            _zones.Add(new PMDMapZoneInfo
            {
                zoneId = "Vung3_HemNuiDaDo",
                displayName = "Vùng 3: Hẻm Núi Đá Đỏ (96×72)",
                mapFileName = "CanyonCamp",
                worldCenter = Vector2.zero,
                worldSize = new Vector2(RoomGridWidth, RoomGridHeight),
                safeSpawnOffset = new Vector2(0f, -1.5f),
                preferredSpecies = new[] { "0004_Charmander", "0255_Torchic", "0059_Arcanine", "0248_Tyranitar", "0445_Garchomp" },
                hasHostileSpawns = true,
                biomeStyle = RegionBiomeStyle.RedRockCanyon,
                northZoneIndex = 4, // SakuraSanctuary
                southZoneIndex = 3, // TropicalArchipelago
                eastZoneIndex = 0,  // MeadowVillage
                westZoneIndex = 6,  // AutumnGuildHighlands
                baseSeed = 303666
            });

            // 3. Vùng 4: Quần Đảo Nhiệt Đới & Bờ Biển San Hô (Tropical Lagoon & Boardwalks)
            _zones.Add(new PMDMapZoneInfo
            {
                zoneId = "Vung4_QuanDaoNhietDoi",
                displayName = "Vùng 4: Quần Đảo Nhiệt Đới (96×72)",
                mapFileName = "BaseCamp",
                worldCenter = Vector2.zero,
                worldSize = new Vector2(RoomGridWidth, RoomGridHeight),
                safeSpawnOffset = new Vector2(0f, -1.5f),
                preferredSpecies = new[] { "0007_Squirtle", "0258_Mudkip", "0130_Gyarados", "0131_Lapras", "0658_Greninja" },
                hasHostileSpawns = true,
                biomeStyle = RegionBiomeStyle.TropicalArchipelago,
                northZoneIndex = 0, // MeadowVillage
                southZoneIndex = 7, // StarlightApex
                eastZoneIndex = 1,  // DeepWoodland
                westZoneIndex = 2,  // RedRockCanyon
                baseSeed = 404555
            });

            // 4. Vùng 5: Khu Vườn Anh Đào & Suối Ánh Sáng (Sakura Sanctuary & Luminous Spring)
            _zones.Add(new PMDMapZoneInfo
            {
                zoneId = "Vung5_VuonAnhDaoSuoiSang",
                displayName = "Vùng 5: Vườn Anh Đào & Suối Sáng (96×72)",
                mapFileName = "LuminousSpring",
                worldCenter = Vector2.zero,
                worldSize = new Vector2(RoomGridWidth, RoomGridHeight),
                safeSpawnOffset = new Vector2(0f, -1.5f),
                preferredSpecies = new[] { "0151_Mew", "0282_Gardevoir", "0149_Dragonite", "0094_Gengar", "0133_Eevee" },
                hasHostileSpawns = true,
                biomeStyle = RegionBiomeStyle.SakuraSanctuary,
                northZoneIndex = 7, // StarlightApex
                southZoneIndex = 0, // MeadowVillage
                eastZoneIndex = 5,  // GlacialHighlands
                westZoneIndex = 2,  // RedRockCanyon
                baseSeed = 505444
            });

            // 5. Vùng 6: Đỉnh Tuyết Vĩnh Cửu (Glacial Snowfield & Frozen Lakes)
            _zones.Add(new PMDMapZoneInfo
            {
                zoneId = "Vung6_DinhTuyetVinhCuu",
                displayName = "Vùng 6: Đỉnh Tuyết Vĩnh Cửu (96×72)",
                mapFileName = "SnowCamp",
                worldCenter = Vector2.zero,
                worldSize = new Vector2(RoomGridWidth, RoomGridHeight),
                safeSpawnOffset = new Vector2(0f, -1.5f),
                preferredSpecies = new[] { "0471_Glaceon", "0131_Lapras", "0143_Snorlax", "0150_Mewtwo", "0007_Squirtle" },
                hasHostileSpawns = true,
                biomeStyle = RegionBiomeStyle.GlacialHighlands,
                northZoneIndex = 7, // StarlightApex
                southZoneIndex = 1, // DeepWoodland
                eastZoneIndex = 6,  // AutumnGuildHighlands
                westZoneIndex = 4,  // SakuraSanctuary
                baseSeed = 606333
            });

            // 6. Vùng 7: Đèo Hoàng Hôn & Thành Cổ Hội Quán (Golden Autumn Guild Highlands)
            _zones.Add(new PMDMapZoneInfo
            {
                zoneId = "Vung7_ThanhCoHoiQuan",
                displayName = "Vùng 7: Thành Cổ Hội Quán (96×72)",
                mapFileName = "Summit",
                worldCenter = Vector2.zero,
                worldSize = new Vector2(RoomGridWidth, RoomGridHeight),
                safeSpawnOffset = new Vector2(0f, -1.5f),
                preferredSpecies = new[] { "0448_Lucario", "0059_Arcanine", "0445_Garchomp", "0248_Tyranitar", "0658_Greninja" },
                hasHostileSpawns = true,
                biomeStyle = RegionBiomeStyle.AutumnGuildHighlands,
                northZoneIndex = 7, // StarlightApex
                southZoneIndex = 5, // GlacialHighlands
                eastZoneIndex = 2,  // RedRockCanyon
                westZoneIndex = 1,  // DeepWoodland
                baseSeed = 707222
            });

            // 7. Vùng 8: Thung Lũng Sao Đêm & Đỉnh Tối Thượng (Starlight Apex Crags)
            _zones.Add(new PMDMapZoneInfo
            {
                zoneId = "Vung8_DinhNuiSaoDem",
                displayName = "Vùng 8: Đỉnh Núi Sao Đêm (96×72)",
                mapFileName = "MountainPeak",
                worldCenter = Vector2.zero,
                worldSize = new Vector2(RoomGridWidth, RoomGridHeight),
                safeSpawnOffset = new Vector2(0f, -1.5f),
                preferredSpecies = new[] { "0384_Rayquaza", "0150_Mewtwo", "0149_Dragonite", "0006_Charizard", "0130_Gyarados" },
                hasHostileSpawns = true,
                biomeStyle = RegionBiomeStyle.StarlightApex,
                northZoneIndex = 0, // MeadowVillage
                southZoneIndex = 4, // SakuraSanctuary
                eastZoneIndex = 6,  // AutumnGuildHighlands
                westZoneIndex = 5,  // GlacialHighlands
                baseSeed = 808111
            });

            // 8. Phòng 9: Nội Thất Bưu Điện (PostOffice Indoor Room)
            _zones.Add(new PMDMapZoneInfo
            {
                zoneId = "Phong9_PostOffice",
                displayName = "Phòng 9: Nội Thất Bưu Điện (Indoor)",
                mapFileName = "PostOffice",
                worldCenter = Vector2.zero,
                worldSize = new Vector2(16f, 13f),
                safeSpawnOffset = new Vector2(0f, -2.5f),
                preferredSpecies = new[] { "0025_Pikachu", "0007_Squirtle", "0133_Eevee" },
                hasHostileSpawns = false,
                isIndoor = true,
                biomeStyle = RegionBiomeStyle.IndoorRoom,
                baseSeed = 909001
            });

            // 9. Phòng 10: Nội Thất Nhà Trọ (InsideHut Indoor Room)
            _zones.Add(new PMDMapZoneInfo
            {
                zoneId = "Phong10_InsideHut",
                displayName = "Phòng 10: Nội Thất Nhà Trọ (Indoor)",
                mapFileName = "InsideHut",
                worldCenter = Vector2.zero,
                worldSize = new Vector2(16f, 13f),
                safeSpawnOffset = new Vector2(0f, -2.2f),
                preferredSpecies = new[] { "0133_Eevee", "0255_Torchic", "0001_Bulbasaur" },
                hasHostileSpawns = false,
                isIndoor = true,
                biomeStyle = RegionBiomeStyle.IndoorRoom,
                baseSeed = 909002
            });
        }

        // =========================================================================================
        // ROOM REBUILD PIPELINE (Unloads old room & generates the active Region at (0, 0))
        // =========================================================================================
        private void RebuildActiveRoomGeometry()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(transform.GetChild(i).gameObject);
            }

            if (_activeRoomCanvasTexture != null)
            {
                DestroyImmediate(_activeRoomCanvasTexture);
                _activeRoomCanvasTexture = null;
            }

            _spawnedItems.Clear();
            _activeClearings.Clear();

            builtLayoutVersion = LayoutVersion;

            _mapsContainer = CreateSubContainer("1_Active_Procedural_Region_Canvas");
            _objectsContainer = CreateSubContainer("2_Region_Trees_Buildings_And_Props");
            _itemsContainer = CreateSubContainer("3_Pickable_Items_Berries_And_Stones");
            _collidersContainer = CreateSubContainer("4_Map_Terrain_Obstacles_Colliders");

            var zone = ActiveZone;
            if (zone == null)
                return;

            if (zone.isIndoor)
            {
                BuildIndoorRoom(zone);
            }
            else
            {
                BuildProceduralOutdoorRegion(zone, currentSeed);
            }
        }

        private Transform CreateSubContainer(string containerName)
        {
            var go = new GameObject(containerName);
            go.transform.SetParent(transform, false);
            return go.transform;
        }

        // =========================================================================================
        // INDOOR ROOM BUILDER (PostOffice & InsideHut)
        // =========================================================================================
        private void BuildIndoorRoom(PMDMapZoneInfo zone)
        {
            _activeClearings.Add(new ProceduralClearing
            {
                center = Vector2.zero,
                radiusX = 5.2f,
                radiusY = 3.8f,
                clearingName = zone.displayName,
                isCentralHub = true
            });

            Texture2D tex = LoadMapTexture(zone.mapFileName);
            if (tex != null)
            {
                var roomGo = new GameObject($"IndoorMap_{zone.mapFileName}");
                roomGo.transform.SetParent(_mapsContainer, false);
                roomGo.transform.position = Vector3.zero;

                var sr = roomGo.AddComponent<SpriteRenderer>();
                sr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), MapPPU);
                sr.sortingOrder = -2000;

                PMDAtmosphere.AttachPointLight(roomGo, new Color(1.0f, 0.84f, 0.58f), 1.35f, 9.5f, false);
            }

            // Indoor boundary walls
            AddBoxColliderObstacle("Indoor_TopWall", new Vector2(0f, 3.8f), new Vector2(15f, 2.5f));
            AddBoxColliderObstacle("Indoor_BottomLeftWall", new Vector2(-5.2f, -5.2f), new Vector2(6.5f, 2.0f));
            AddBoxColliderObstacle("Indoor_BottomRightWall", new Vector2(5.2f, -5.2f), new Vector2(6.5f, 2.0f));
            AddBoxColliderObstacle("Indoor_LeftWall", new Vector2(-7.2f, 0f), new Vector2(2.0f, 13f));
            AddBoxColliderObstacle("Indoor_RightWall", new Vector2(7.2f, 0f), new Vector2(2.0f, 13f));

            // Indoor props
            SpawnMapObject("Stump_Table_Indoors.None", new Vector2(-3.2f, 0.8f), _objectsContainer, true);
            SpawnMapObject("Stump_Chair_Indoors.None", new Vector2(-2.0f, 0.8f), _objectsContainer, false);
            SpawnMapObject("Diary_Green_Opening.Flip", new Vector2(-3.2f, 1.0f), _objectsContainer, false);
            SpawnMapObject("Chest.None", new Vector2(3.6f, 1.4f), _objectsContainer, true);
            SpawnMapObject("Storage.Flip", new Vector2(-4.4f, 1.8f), _objectsContainer, true);
            SpawnMapObject("Flowerpot_Pink.Flip", new Vector2(-4.5f, -2.5f), _objectsContainer, true);
            SpawnMapObject("Flowerpot_White.Flip", new Vector2(4.5f, -2.5f), _objectsContainer, true);

            // Exit doorway mat at bottom center (0, -5.0) returning to the outdoor region
            SpawnMapObject("Portal_Small.None", new Vector2(0f, -4.8f), _objectsContainer, false);
            var exitGo = new GameObject("Gateway_Exit_To_Outdoor");
            exitGo.transform.SetParent(_objectsContainer, false);
            exitGo.transform.position = new Vector2(0f, -5.0f);
            var trigger = exitGo.AddComponent<PMDRoomGatewayTrigger>();
            trigger.targetZoneIndex = _lastOutdoorZoneIndex;
            trigger.arrivalEntranceSide = RoomEntranceSide.ReturnFromIndoor;
            trigger.gatewayLabel = $"Ra ngoài: {_zones[_lastOutdoorZoneIndex].displayName}";

            SpawnItemAt("Apple_Gold.None", new Vector2(2.6f, -0.8f));
            SpawnItemAt("Berry_Sitrus.None", new Vector2(-2.6f, -1.2f));
        }

        // =========================================================================================
        // MASSIVE 96x72 PROCEDURAL OUTDOOR REGION GENERATOR
        // =========================================================================================
        private void BuildProceduralOutdoorRegion(PMDMapZoneInfo zone, int seed)
        {
            var rng = new System.Random(seed);
            _currentGrid = new RoomTileType[RoomGridWidth, RoomGridHeight];

            // 1. Generate 9 Interconnected Clearings (Central Hub + 4 Gate Clearings + 4 Corner Clearings)
            GenerateRoomClearings(zone, rng);

            // 2. Carve Clearings, Winding Road Network, Meandering River/Lagoon, and Wooden Bridges into 96x72 Grid
            PopulateProceduralTileGrid(zone, seed, rng);

            // 3. Render Crisp Pixel-Art 96x72 Region Canvas Texture (1152 x 864 px at 12 PPU)
            RenderProceduralRegionCanvas(zone, seed);

            // 4. Generate Merged BoxCollider2D Strips for All Water & Outer/Inner Forest/Cliff Walls
            BuildGridObstacleColliders();

            // 5. Spawn 4 Directional Room Gateways (North, South, East, West) + Dungeon Staircase Portal
            SpawnRegionGatewaysAndStaircase(zone, rng);

            // 6. Populate 3D Y-Sorted Perimeter Trees, Biome Boulders, Landmarks, Houses, Flowers & Items
            PopulateProceduralRegionScenery(zone, rng);
            PopulateProceduralRegionItems(rng);
        }

        private void GenerateRoomClearings(PMDMapZoneInfo zone, System.Random rng)
        {
            _activeClearings.Clear();

            // 0. Central Hub Plaza (Large 26x20 clearing at (0, 0))
            _activeClearings.Add(new ProceduralClearing
            {
                center = Vector2.zero,
                radiusX = 13.0f,
                radiusY = 10.2f,
                clearingName = "Quảng Trường Trung Tâm",
                isCentralHub = true
            });

            // 1. North Gate Clearing
            _activeClearings.Add(new ProceduralClearing
            {
                center = new Vector2(RandRange(rng, -4.0f, 4.0f), 21.5f),
                radiusX = 8.8f,
                radiusY = 6.8f,
                clearingName = "Trạm Gác Phía Bắc",
                isCentralHub = false
            });

            // 2. South Gate Clearing
            _activeClearings.Add(new ProceduralClearing
            {
                center = new Vector2(RandRange(rng, -4.0f, 4.0f), -21.5f),
                radiusX = 8.8f,
                radiusY = 6.8f,
                clearingName = "Trạm Gác Phía Nam",
                isCentralHub = false
            });

            // 3. East Gate Clearing
            _activeClearings.Add(new ProceduralClearing
            {
                center = new Vector2(29.0f, RandRange(rng, -4.0f, 4.0f)),
                radiusX = 8.8f,
                radiusY = 6.8f,
                clearingName = "Trạm Gác Phía Đông",
                isCentralHub = false
            });

            // 4. West Gate Clearing
            _activeClearings.Add(new ProceduralClearing
            {
                center = new Vector2(-29.0f, RandRange(rng, -4.0f, 4.0f)),
                radiusX = 8.8f,
                radiusY = 6.8f,
                clearingName = "Trạm Gác Phía Tây",
                isCentralHub = false
            });

            // 5. North-West Exploration Room
            _activeClearings.Add(new ProceduralClearing
            {
                center = new Vector2(-25.0f + RandRange(rng, -3.5f, 3.5f), 19.0f + RandRange(rng, -2.5f, 2.5f)),
                radiusX = 9.2f,
                radiusY = 7.2f,
                clearingName = "Thung Lũng Tây Bắc",
                isCentralHub = false
            });

            // 6. North-East Exploration Room
            _activeClearings.Add(new ProceduralClearing
            {
                center = new Vector2(25.0f + RandRange(rng, -3.5f, 3.5f), 19.0f + RandRange(rng, -2.5f, 2.5f)),
                radiusX = 9.2f,
                radiusY = 7.2f,
                clearingName = "Thung Lũng Đông Bắc",
                isCentralHub = false
            });

            // 7. South-West Exploration Room
            _activeClearings.Add(new ProceduralClearing
            {
                center = new Vector2(-25.0f + RandRange(rng, -3.5f, 3.5f), -19.0f + RandRange(rng, -2.5f, 2.5f)),
                radiusX = 9.2f,
                radiusY = 7.2f,
                clearingName = "Thung Lũng Tây Nam",
                isCentralHub = false
            });

            // 8. South-East Exploration Room
            _activeClearings.Add(new ProceduralClearing
            {
                center = new Vector2(25.0f + RandRange(rng, -3.5f, 3.5f), -19.0f + RandRange(rng, -2.5f, 2.5f)),
                radiusX = 9.2f,
                radiusY = 7.2f,
                clearingName = "Thung Lũng Đông Nam",
                isCentralHub = false
            });
        }

        private void PopulateProceduralTileGrid(PMDMapZoneInfo zone, int seed, System.Random rng)
        {
            float seedOffX = (seed % 997) * 0.17f;
            float seedOffY = ((seed / 997) % 997) * 0.17f;

            // Define Road Segments connecting the 9 clearings and the 4 outer Room Gateways
            var roadSegments = new List<(Vector2 a, Vector2 b, float halfWidth)>();

            Vector2 hub = _activeClearings[0].center;
            Vector2 cNorth = _activeClearings[1].center;
            Vector2 cSouth = _activeClearings[2].center;
            Vector2 cEast = _activeClearings[3].center;
            Vector2 cWest = _activeClearings[4].center;
            Vector2 cNW = _activeClearings[5].center;
            Vector2 cNE = _activeClearings[6].center;
            Vector2 cSW = _activeClearings[7].center;
            Vector2 cSE = _activeClearings[8].center;

            // 4 Gateway Exit Corridors right up to the room boundary gates
            Vector2 gateNorth = new Vector2(0f, RoomHalfHeight - 3.5f); // (0, +32.5)
            Vector2 gateSouth = new Vector2(0f, -RoomHalfHeight + 3.5f); // (0, -32.5)
            Vector2 gateEast = new Vector2(RoomHalfWidth - 4.0f, 0f); // (+44.0, 0)
            Vector2 gateWest = new Vector2(-RoomHalfWidth + 4.0f, 0f); // (-44.0, 0)

            // Main Cardinal Highways
            roadSegments.Add((hub, cNorth, 1.85f));
            roadSegments.Add((cNorth, gateNorth, 2.1f));

            roadSegments.Add((hub, cSouth, 1.85f));
            roadSegments.Add((cSouth, gateSouth, 2.1f));

            roadSegments.Add((hub, cEast, 1.85f));
            roadSegments.Add((cEast, gateEast, 2.1f));

            roadSegments.Add((hub, cWest, 1.85f));
            roadSegments.Add((cWest, gateWest, 2.1f));

            // Outer Perimeter Exploration Ring connecting all 8 outer clearings
            roadSegments.Add((cNorth, cNW, 1.45f));
            roadSegments.Add((cNW, cWest, 1.45f));
            roadSegments.Add((cWest, cSW, 1.45f));
            roadSegments.Add((cSW, cSouth, 1.45f));
            roadSegments.Add((cSouth, cSE, 1.45f));
            roadSegments.Add((cSE, cEast, 1.45f));
            roadSegments.Add((cEast, cNE, 1.45f));
            roadSegments.Add((cNE, cNorth, 1.45f));

            // Procedural Diagonal Shortcuts (varies by seed!)
            if (rng.NextDouble() > 0.45)
                roadSegments.Add((hub, cNW, 1.35f));
            else
                roadSegments.Add((hub, cNE, 1.35f));

            if (rng.NextDouble() > 0.45)
                roadSegments.Add((hub, cSE, 1.35f));
            else
                roadSegments.Add((hub, cSW, 1.35f));

            // River parameters (varies by seed!)
            float riverPhase = RandRange(rng, 0f, Mathf.PI * 2f);
            float riverOffset = RandRange(rng, -9.5f, 9.5f);
            if (Mathf.Abs(riverOffset) < 6.0f)
                riverOffset = riverOffset >= 0f ? 7.5f : -7.5f;

            bool horizontalRiver = rng.NextDouble() > 0.45;
            bool isArchipelago = zone.biomeStyle == RegionBiomeStyle.TropicalArchipelago;

            for (int gy = 0; gy < RoomGridHeight; gy++)
            {
                float wy = -RoomHalfHeight + gy + 0.5f;
                for (int gx = 0; gx < RoomGridWidth; gx++)
                {
                    float wx = -RoomHalfWidth + gx + 0.5f;

                    // Outer 3-tile border is always solid forest/cliff wall except around the 4 Gateways
                    bool nearNorthGate = Vector2.Distance(new Vector2(wx, wy), gateNorth) <= 3.6f;
                    bool nearSouthGate = Vector2.Distance(new Vector2(wx, wy), gateSouth) <= 3.6f;
                    bool nearEastGate = Vector2.Distance(new Vector2(wx, wy), gateEast) <= 3.6f;
                    bool nearWestGate = Vector2.Distance(new Vector2(wx, wy), gateWest) <= 3.6f;
                    bool inGateZone = nearNorthGate || nearSouthGate || nearEastGate || nearWestGate;

                    if (!inGateZone && (gx < 3 || gx >= RoomGridWidth - 3 || gy < 3 || gy >= RoomGridHeight - 3))
                    {
                        _currentGrid[gx, gy] = RoomTileType.TreeOrCliffWall;
                        continue;
                    }

                    float n1 = FractalNoise(wx * 0.09f + seedOffX, wy * 0.09f + seedOffY);
                    float n2 = FractalNoise(wx * 0.22f + seedOffY, wy * 0.22f + seedOffX);

                    // 1. Check Distance to Road Network (with organic Perlin wobble)
                    float wobbleX = wx + (n2 - 0.5f) * 1.6f;
                    float wobbleY = wy + (n1 - 0.5f) * 1.6f;
                    float minRoadDist = 999f;
                    for (int r = 0; r < roadSegments.Count; r++)
                    {
                        var seg = roadSegments[r];
                        float d = DistanceToSegment(wobbleX, wobbleY, seg.a.x, seg.a.y, seg.b.x, seg.b.y) - seg.halfWidth;
                        if (d < minRoadDist)
                            minRoadDist = d;
                    }

                    bool isOnRoad = minRoadDist <= 0f || inGateZone;

                    // 2. Check if Inside Any of the 9 Clearings (or Road Grassy Shoulder)
                    bool isInsideClearing = (minRoadDist <= 2.4f + (n1 - 0.5f) * 1.5f);
                    if (!isInsideClearing)
                    {
                        for (int c = 0; c < _activeClearings.Count; c++)
                        {
                            var cl = _activeClearings[c];
                            float dx = (wx - cl.center.x) / cl.radiusX;
                            float dy = (wy - cl.center.y) / cl.radiusY;
                            float normDist = Mathf.Sqrt(dx * dx + dy * dy) + (n1 - 0.5f) * 0.28f + (n2 - 0.5f) * 0.14f;
                            if (normDist <= 1.0f)
                            {
                                isInsideClearing = true;
                                break;
                            }
                        }
                    }

                    // 3. Check Procedural Meandering River / Lagoon Water
                    bool isWater = false;
                    float distFromHub = Vector2.Distance(new Vector2(wx, wy), hub);

                    if (!inGateZone && distFromHub > 7.8f)
                    {
                        if (horizontalRiver)
                        {
                            float riverCenterY = riverOffset + Mathf.Sin(wx * 0.11f + riverPhase) * 5.2f + (n1 - 0.5f) * 2.5f;
                            if (Mathf.Abs(wy - riverCenterY) < 1.85f)
                                isWater = true;
                        }
                        else
                        {
                            float riverCenterX = riverOffset + Mathf.Sin(wy * 0.11f + riverPhase) * 6.0f + (n1 - 0.5f) * 2.5f;
                            if (Mathf.Abs(wx - riverCenterX) < 1.95f)
                                isWater = true;
                        }

                        // Archipelago has additional tropical lagoon channels between island clearings
                        if (isArchipelago && minRoadDist > 2.2f && distFromHub > 11.5f && n1 > 0.54f)
                        {
                            isWater = true;
                        }
                    }

                    if (isWater)
                    {
                        // Wherever a Road crosses Water, automatically build a Walkable Wooden Bridge!
                        _currentGrid[gx, gy] = (minRoadDist <= 0.45f) ? RoomTileType.Bridge : RoomTileType.Water;
                    }
                    else if (isOnRoad)
                    {
                        _currentGrid[gx, gy] = RoomTileType.Road;
                    }
                    else if (isInsideClearing)
                    {
                        _currentGrid[gx, gy] = RoomTileType.Grass;
                    }
                    else
                    {
                        _currentGrid[gx, gy] = RoomTileType.TreeOrCliffWall;
                    }
                }
            }
        }

        // =========================================================================================
        // PIXEL-ART 96x72 REGION CANVAS RENDERER (1152 x 864 px at 12 PPU)
        // =========================================================================================
        private void RenderProceduralRegionCanvas(PMDMapZoneInfo zone, int seed)
        {
            const int canvasPPU = 12; // 12 pixels per tile -> 1152 x 864 crisp pixel-art texture
            int widthPx = RoomGridWidth * canvasPPU;
            int heightPx = RoomGridHeight * canvasPPU;

            _activeRoomCanvasTexture = new Texture2D(widthPx, heightPx, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = $"ProceduralRegion_{zone.zoneId}_{seed}"
            };

            // Sample authentic 64x64 hand-drawn PMD canopy pattern from TownBase.png
            Texture2D townTex = LoadMapTexture("TownBase");
            Color[] treePattern64 = (townTex != null && townTex.width >= 320 && townTex.height >= 96)
                ? townTex.GetPixels(256, 32, 64, 64)
                : null;

            Color[] pixels = new Color[widthPx * heightPx];
            GetBiomePalette(zone.biomeStyle, out Color grassLight, out Color grassDark, out Color grassAccent,
                out Color roadLight, out Color roadDark, out Color roadBorder,
                out Color waterDeep, out Color waterShallow, out Color waterFoam,
                out Color wallTint);

            for (int py = 0; py < heightPx; py++)
            {
                int gy = py / canvasPPU;
                int subY = py % canvasPPU;
                float wy = -RoomHalfHeight + (py + 0.5f) / canvasPPU;
                int tpy = ((py * 2) % 64 + 64) % 64;

                for (int px = 0; px < widthPx; px++)
                {
                    int gx = px / canvasPPU;
                    int subX = px % canvasPPU;
                    float wx = -RoomHalfWidth + (px + 0.5f) / canvasPPU;

                    RoomTileType tile = _currentGrid[gx, gy];
                    float bayer = Bayer4x4[px & 3, py & 3] - 0.5f;
                    float micro = Hash2D(px / 2 + seed % 131, py / 2 + seed % 197);

                    int tpx = ((px * 2) % 64 + 64) % 64;
                    Color rawTreeSample = treePattern64 != null ? treePattern64[tpy * 64 + tpx] : new Color(0.18f, 0.64f, 0.36f, 1f);

                    pixels[py * widthPx + px] = RenderTilePixel(
                        gx, gy, subX, subY, canvasPPU, wx, wy, tile, bayer, micro, rawTreeSample,
                        grassLight, grassDark, grassAccent,
                        roadLight, roadDark, roadBorder,
                        waterDeep, waterShallow, waterFoam, wallTint);
                }
            }

            _activeRoomCanvasTexture.SetPixels(pixels);
            _activeRoomCanvasTexture.Apply(false);

            var canvasGo = new GameObject($"RegionCanvas_{zone.zoneId}_Seed_{seed}");
            canvasGo.transform.SetParent(_mapsContainer, false);
            canvasGo.transform.position = Vector3.zero;

            var sr = canvasGo.AddComponent<SpriteRenderer>();
            sr.sprite = Sprite.Create(_activeRoomCanvasTexture, new Rect(0, 0, widthPx, heightPx), new Vector2(0.5f, 0.5f), canvasPPU);
            sr.sortingOrder = -2200;
        }

        private Color RenderTilePixel(
            int gx, int gy, int subX, int subY, int canvasPPU, float wx, float wy,
            RoomTileType tile, float bayer, float micro, Color rawTreeSample,
            Color grassLight, Color grassDark, Color grassAccent,
            Color roadLight, Color roadDark, Color roadBorder,
            Color waterDeep, Color waterShallow, Color waterFoam, Color wallTint)
        {
            switch (tile)
            {
                case RoomTileType.Bridge:
                {
                    // Wooden plank bridge with top/bottom handrails & nail studs
                    bool isRail = (subY <= 1 || subY >= canvasPPU - 2);
                    if (isRail)
                    {
                        bool isPost = (subX % 6 == 0);
                        return isPost ? new Color(0.20f, 0.12f, 0.07f, 1f) : new Color(0.50f, 0.32f, 0.16f, 1f);
                    }
                    bool plankSeam = (subX % 4 == 0);
                    if (plankSeam)
                        return new Color(0.28f, 0.17f, 0.10f, 1f);

                    if ((subY == 2 || subY == canvasPPU - 3) && (subX % 4 == 2))
                        return new Color(0.20f, 0.14f, 0.10f, 1f);

                    bool isAltPlank = ((subX / 4) % 2 == 0);
                    Color plankColor = isAltPlank
                        ? new Color(0.78f, 0.56f, 0.33f, 1f)
                        : new Color(0.68f, 0.46f, 0.26f, 1f);

                    if (micro > 0.65f)
                        plankColor = Color.Lerp(plankColor, new Color(0.38f, 0.22f, 0.12f, 1f), 0.18f);

                    return plankColor;
                }

                case RoomTileType.Water:
                {
                    // Check if adjacent to non-water for shoreline foam & shallow bevel
                    bool nearLand = IsNeighborNotWater(gx, gy);
                    if (nearLand)
                    {
                        bool edgeFoam = (subX <= 1 || subX >= canvasPPU - 2 || subY <= 1 || subY >= canvasPPU - 2) && (micro + bayer * 0.3f > 0.32f);
                        if (edgeFoam)
                            return waterFoam;
                        return waterShallow;
                    }

                    // Gentle undulating ripples in deep water + sun glint sparkles
                    int ripple = ((subX * 2 + subY + (gx + gy) * 3) % 11);
                    if (ripple == 0 && micro > 0.70f)
                    {
                        return Color.Lerp(waterShallow, waterFoam, 0.65f);
                    }
                    if (ripple <= 1)
                    {
                        return waterShallow;
                    }

                    return waterDeep;
                }

                case RoomTileType.Road:
                {
                    // Road borders against grass/wall
                    bool touchesGrass = IsNeighborGrassOrWall(gx, gy);
                    if (touchesGrass && (subX <= 1 || subX >= canvasPPU - 2 || subY <= 1 || subY >= canvasPPU - 2))
                    {
                        if (micro + bayer * 0.3f > 0.28f)
                            return roadBorder;
                    }

                    // Authentic PMD cobblestone paving pattern (staggered bricks 6x4)
                    int totalPxX = gx * canvasPPU + subX;
                    int totalPxY = gy * canvasPPU + subY;
                    int brickRow = totalPxY / 4;
                    int offsetX = (brickRow % 2 == 0) ? 0 : 3;
                    int brickCol = (totalPxX + offsetX) / 6;
                    int cellX = (totalPxX + offsetX) % 6;
                    int cellY = totalPxY % 4;

                    // Mortar seams between cobblestones
                    if (cellX == 0 || cellY == 0)
                    {
                        return roadBorder;
                    }

                    // Top-left highlight on individual cobblestone
                    if (cellX == 1 && cellY == 1)
                    {
                        return Color.Lerp(roadLight, Color.white, 0.22f);
                    }

                    // Bottom-right shadow bevel on stone
                    if (cellX == 5 || cellY == 3)
                    {
                        return Color.Lerp(roadDark, roadBorder, 0.40f);
                    }

                    // Per-cobblestone color variation
                    float stoneVariation = Hash2D(brickCol * 17 + 5, brickRow * 23 + 11);
                    Color baseStone = (stoneVariation > 0.5f) ? roadLight : roadDark;
                    if (micro > 0.80f)
                        baseStone = Color.Lerp(baseStone, roadBorder, 0.20f);

                    return baseStone;
                }

                case RoomTileType.TreeOrCliffWall:
                {
                    float lum = rawTreeSample.r * 0.299f + rawTreeSample.g * 0.587f + rawTreeSample.b * 0.114f;
                    Color tintedCanopy = new Color(
                        Mathf.Clamp01(lum * wallTint.r * 1.55f + wallTint.r * 0.15f),
                        Mathf.Clamp01(lum * wallTint.g * 1.55f + wallTint.g * 0.15f),
                        Mathf.Clamp01(lum * wallTint.b * 1.55f + wallTint.b * 0.15f),
                        1f);

                    // Add a soft shadow bevel along the bottom edge where wall meets walkable ground
                    if (gy > 0 && _currentGrid[gx, gy - 1] != RoomTileType.TreeOrCliffWall && subY < 3)
                    {
                        return Color.Lerp(tintedCanopy, new Color(0.08f, 0.10f, 0.14f, 1f), 0.45f);
                    }
                    return tintedCanopy;
                }

                case RoomTileType.Grass:
                default:
                {
                    // Ambient occlusion drop shadow just south of a forest/cliff wall for rich 3D depth
                    if (gy < RoomGridHeight - 1 && _currentGrid[gx, gy + 1] == RoomTileType.TreeOrCliffWall)
                    {
                        if (subY >= canvasPPU - 2)
                            return Color.Lerp(grassDark, new Color(0.06f, 0.10f, 0.08f, 1f), 0.45f);
                        if (subY == canvasPPU - 3)
                            return Color.Lerp(grassDark, new Color(0.06f, 0.10f, 0.08f, 1f), 0.28f);
                        if (subY == canvasPPU - 4)
                            return Color.Lerp(grassDark, new Color(0.06f, 0.10f, 0.08f, 1f), 0.14f);
                    }

                    // Crisp decorative grass blade tufts
                    if (micro > 0.82f)
                    {
                        bool isBlade = (subX == 5 && (subY == 4 || subY == 5)) ||
                                       (subX == 6 && subY == 6) ||
                                       (subX == 4 && subY == 6);
                        if (isBlade)
                            return grassAccent;
                        if (subX == 5 && subY == 3)
                            return Color.Lerp(grassDark, new Color(0.05f, 0.10f, 0.05f, 1f), 0.35f);
                    }

                    // Occasional tiny wild flower speckle
                    if (micro > 0.94f)
                    {
                        if (subX == 8 && subY == 7)
                            return Color.white;
                        if (subX == 8 && subY == 8)
                            return new Color(1.0f, 0.85f, 0.25f, 1f);
                    }

                    // Multi-tone fractal noise dithering
                    float nPatch = FractalNoise(wx * 0.18f, wy * 0.18f);
                    return (nPatch + bayer * 0.24f > 0.50f) ? grassLight : grassDark;
                }
            }
        }

        private bool IsNeighborNotWater(int gx, int gy)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0)
                        continue;
                    int nx = gx + dx;
                    int ny = gy + dy;
                    if (nx >= 0 && nx < RoomGridWidth && ny >= 0 && ny < RoomGridHeight)
                    {
                        var t = _currentGrid[nx, ny];
                        if (t != RoomTileType.Water && t != RoomTileType.Bridge)
                            return true;
                    }
                }
            }
            return false;
        }

        private bool IsNeighborGrassOrWall(int gx, int gy)
        {
            if (gx > 0 && _currentGrid[gx - 1, gy] == RoomTileType.Grass) return true;
            if (gx < RoomGridWidth - 1 && _currentGrid[gx + 1, gy] == RoomTileType.Grass) return true;
            if (gy > 0 && _currentGrid[gx, gy - 1] == RoomTileType.Grass) return true;
            if (gy < RoomGridHeight - 1 && _currentGrid[gx, gy + 1] == RoomTileType.Grass) return true;
            return false;
        }

        private static void GetBiomePalette(
            RegionBiomeStyle style,
            out Color grassLight, out Color grassDark, out Color grassAccent,
            out Color roadLight, out Color roadDark, out Color roadBorder,
            out Color waterDeep, out Color waterShallow, out Color waterFoam,
            out Color wallTint)
        {
            switch (style)
            {
                case RegionBiomeStyle.DeepWoodland:
                    grassLight = new Color(0.24f, 0.68f, 0.44f, 1f);
                    grassDark = new Color(0.17f, 0.56f, 0.38f, 1f);
                    grassAccent = new Color(0.42f, 0.85f, 0.55f, 1f);
                    roadLight = new Color(0.76f, 0.64f, 0.42f, 1f);
                    roadDark = new Color(0.66f, 0.54f, 0.35f, 1f);
                    roadBorder = new Color(0.50f, 0.40f, 0.25f, 1f);
                    waterDeep = new Color(0.10f, 0.40f, 0.56f, 1f);
                    waterShallow = new Color(0.20f, 0.62f, 0.72f, 1f);
                    waterFoam = new Color(0.82f, 0.96f, 0.98f, 1f);
                    wallTint = new Color(0.15f, 0.55f, 0.46f, 1f);
                    break;

                case RegionBiomeStyle.RedRockCanyon:
                    grassLight = new Color(0.80f, 0.74f, 0.36f, 1f);
                    grassDark = new Color(0.70f, 0.62f, 0.30f, 1f);
                    grassAccent = new Color(0.92f, 0.84f, 0.45f, 1f);
                    roadLight = new Color(0.86f, 0.56f, 0.34f, 1f);
                    roadDark = new Color(0.76f, 0.46f, 0.28f, 1f);
                    roadBorder = new Color(0.58f, 0.32f, 0.18f, 1f);
                    waterDeep = new Color(0.14f, 0.46f, 0.68f, 1f);
                    waterShallow = new Color(0.26f, 0.68f, 0.84f, 1f);
                    waterFoam = new Color(0.92f, 0.96f, 1.0f, 1f);
                    wallTint = new Color(0.68f, 0.38f, 0.22f, 1f);
                    break;

                case RegionBiomeStyle.TropicalArchipelago:
                    grassLight = new Color(0.97f, 0.88f, 0.60f, 1f);
                    grassDark = new Color(0.91f, 0.80f, 0.52f, 1f);
                    grassAccent = new Color(1.0f, 0.95f, 0.74f, 1f);
                    roadLight = new Color(0.82f, 0.62f, 0.38f, 1f);
                    roadDark = new Color(0.72f, 0.52f, 0.30f, 1f);
                    roadBorder = new Color(0.55f, 0.38f, 0.20f, 1f);
                    waterDeep = new Color(0.11f, 0.44f, 0.74f, 1f);
                    waterShallow = new Color(0.24f, 0.74f, 0.88f, 1f);
                    waterFoam = new Color(0.94f, 0.99f, 1.0f, 1f);
                    wallTint = new Color(0.24f, 0.68f, 0.38f, 1f);
                    break;

                case RegionBiomeStyle.SakuraSanctuary:
                    grassLight = new Color(0.38f, 0.78f, 0.52f, 1f);
                    grassDark = new Color(0.28f, 0.66f, 0.44f, 1f);
                    grassAccent = new Color(0.98f, 0.72f, 0.86f, 1f);
                    roadLight = new Color(0.82f, 0.80f, 0.84f, 1f);
                    roadDark = new Color(0.72f, 0.70f, 0.75f, 1f);
                    roadBorder = new Color(0.56f, 0.54f, 0.60f, 1f);
                    waterDeep = new Color(0.18f, 0.56f, 0.82f, 1f);
                    waterShallow = new Color(0.42f, 0.84f, 0.96f, 1f);
                    waterFoam = new Color(0.95f, 0.99f, 1.0f, 1f);
                    wallTint = new Color(0.82f, 0.46f, 0.66f, 1f);
                    break;

                case RegionBiomeStyle.GlacialHighlands:
                    grassLight = new Color(0.90f, 0.95f, 0.99f, 1f);
                    grassDark = new Color(0.80f, 0.88f, 0.95f, 1f);
                    grassAccent = new Color(1.0f, 1.0f, 1.0f, 1f);
                    roadLight = new Color(0.68f, 0.80f, 0.90f, 1f);
                    roadDark = new Color(0.58f, 0.72f, 0.84f, 1f);
                    roadBorder = new Color(0.45f, 0.58f, 0.72f, 1f);
                    waterDeep = new Color(0.22f, 0.52f, 0.80f, 1f);
                    waterShallow = new Color(0.55f, 0.82f, 0.96f, 1f);
                    waterFoam = new Color(0.96f, 0.99f, 1.0f, 1f);
                    wallTint = new Color(0.62f, 0.78f, 0.90f, 1f);
                    break;

                case RegionBiomeStyle.AutumnGuildHighlands:
                    grassLight = new Color(0.62f, 0.74f, 0.32f, 1f);
                    grassDark = new Color(0.50f, 0.62f, 0.26f, 1f);
                    grassAccent = new Color(0.96f, 0.68f, 0.24f, 1f);
                    roadLight = new Color(0.76f, 0.74f, 0.70f, 1f);
                    roadDark = new Color(0.65f, 0.63f, 0.59f, 1f);
                    roadBorder = new Color(0.48f, 0.46f, 0.43f, 1f);
                    waterDeep = new Color(0.14f, 0.42f, 0.66f, 1f);
                    waterShallow = new Color(0.28f, 0.64f, 0.82f, 1f);
                    waterFoam = new Color(0.92f, 0.97f, 1.0f, 1f);
                    wallTint = new Color(0.82f, 0.52f, 0.20f, 1f);
                    break;

                case RegionBiomeStyle.StarlightApex:
                    grassLight = new Color(0.34f, 0.32f, 0.48f, 1f);
                    grassDark = new Color(0.26f, 0.24f, 0.38f, 1f);
                    grassAccent = new Color(0.62f, 0.75f, 1.0f, 1f);
                    roadLight = new Color(0.54f, 0.58f, 0.78f, 1f);
                    roadDark = new Color(0.44f, 0.48f, 0.66f, 1f);
                    roadBorder = new Color(0.32f, 0.35f, 0.52f, 1f);
                    waterDeep = new Color(0.12f, 0.20f, 0.48f, 1f);
                    waterShallow = new Color(0.30f, 0.52f, 0.86f, 1f);
                    waterFoam = new Color(0.85f, 0.94f, 1.0f, 1f);
                    wallTint = new Color(0.24f, 0.22f, 0.40f, 1f);
                    break;

                case RegionBiomeStyle.MeadowVillage:
                default:
                    grassLight = new Color(0.42f, 0.80f, 0.36f, 1f);
                    grassDark = new Color(0.32f, 0.70f, 0.30f, 1f);
                    grassAccent = new Color(0.62f, 0.92f, 0.45f, 1f);
                    roadLight = new Color(0.92f, 0.82f, 0.56f, 1f);
                    roadDark = new Color(0.84f, 0.74f, 0.48f, 1f);
                    roadBorder = new Color(0.68f, 0.58f, 0.36f, 1f);
                    waterDeep = new Color(0.14f, 0.46f, 0.74f, 1f);
                    waterShallow = new Color(0.28f, 0.70f, 0.88f, 1f);
                    waterFoam = new Color(0.92f, 0.98f, 1.0f, 1f);
                    wallTint = new Color(0.22f, 0.64f, 0.34f, 1f);
                    break;
            }
        }

        // =========================================================================================
        // AUTOMATIC MERGED BOXCOLLIDER2D OBSTACLE GENERATOR FOR WATER & FOREST/CLIFF WALLS
        // =========================================================================================
        private void BuildGridObstacleColliders()
        {
            if (_currentGrid == null)
                return;

            for (int gy = 0; gy < RoomGridHeight; gy++)
            {
                int runStart = -1;
                for (int gx = 0; gx <= RoomGridWidth; gx++)
                {
                    bool blocked = false;
                    if (gx < RoomGridWidth)
                    {
                        RoomTileType t = _currentGrid[gx, gy];
                        blocked = (t == RoomTileType.TreeOrCliffWall || t == RoomTileType.Water);
                    }

                    if (blocked && runStart < 0)
                    {
                        runStart = gx;
                    }
                    else if (!blocked && runStart >= 0)
                    {
                        int runLen = gx - runStart;
                        float centerX = -RoomHalfWidth + runStart + runLen * 0.5f;
                        float centerY = -RoomHalfHeight + gy + 0.5f;
                        AddBoxColliderObstacle($"GridWall_Y{gy}_X{runStart}", new Vector2(centerX, centerY), new Vector2(runLen, 1.0f));
                        runStart = -1;
                    }
                }
            }

            // Outer safety perimeter colliders around the 96x72 room
            AddBoxColliderObstacle("RoomOuterBound_North", new Vector2(0f, RoomHalfHeight + 1.0f), new Vector2(RoomGridWidth + 4f, 2.0f));
            AddBoxColliderObstacle("RoomOuterBound_South", new Vector2(0f, -RoomHalfHeight - 1.0f), new Vector2(RoomGridWidth + 4f, 2.0f));
            AddBoxColliderObstacle("RoomOuterBound_East", new Vector2(RoomHalfWidth + 1.0f, 0f), new Vector2(2.0f, RoomGridHeight + 4f));
            AddBoxColliderObstacle("RoomOuterBound_West", new Vector2(-RoomHalfWidth - 1.0f, 0f), new Vector2(2.0f, RoomGridHeight + 4f));
        }

        private void AddBoxColliderObstacle(string name, Vector2 center, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_collidersContainer, false);
            go.transform.position = center;
            var col = go.AddComponent<BoxCollider2D>();
            col.size = size;
        }

        // =========================================================================================
        // 4 DIRECTIONAL ROOM GATEWAYS + PROCEDURAL DUNGEON STAIRCASE / PORTAL
        // =========================================================================================
        private void SpawnRegionGatewaysAndStaircase(PMDMapZoneInfo zone, System.Random rng)
        {
            // 1. North Gateway (at (0, +32.5) -> spawns player at South Entrance of North Region)
            CreateDirectionalGateway(
                "Gateway_North",
                new Vector2(0f, RoomHalfHeight - 3.5f),
                zone.northZoneIndex,
                RoomEntranceSide.South,
                $"⬆ Cổng Bắc ➔ {_zones[zone.northZoneIndex].displayName}",
                true);

            // 2. South Gateway (at (0, -32.5) -> spawns player at North Entrance of South Region)
            CreateDirectionalGateway(
                "Gateway_South",
                new Vector2(0f, -RoomHalfHeight + 3.5f),
                zone.southZoneIndex,
                RoomEntranceSide.North,
                $"⬇ Cổng Nam ➔ {_zones[zone.southZoneIndex].displayName}",
                true);

            // 3. East Gateway (at (+44.0, 0) -> spawns player at West Entrance of East Region)
            CreateDirectionalGateway(
                "Gateway_East",
                new Vector2(RoomHalfWidth - 4.0f, 0f),
                zone.eastZoneIndex,
                RoomEntranceSide.West,
                $"➡ Cổng Đông ➔ {_zones[zone.eastZoneIndex].displayName}",
                false);

            // 4. West Gateway (at (-44.0, 0) -> spawns player at East Entrance of West Region)
            CreateDirectionalGateway(
                "Gateway_West",
                new Vector2(-RoomHalfWidth + 4.0f, 0f),
                zone.westZoneIndex,
                RoomEntranceSide.East,
                $"⬅ Cổng Tây ➔ {_zones[zone.westZoneIndex].displayName}",
                false);

            // 5. Procedural Dungeon Staircase & Mystic Portal in one of the 4 Corner Clearings (indices 5..8)
            int cornerIdx = 5 + rng.Next(0, 4);
            Vector2 stairPos = _activeClearings[cornerIdx].center + new Vector2(0f, 1.2f);
            int nextDungeonZone = (activeZoneIndex + 1) % 8;

            SpawnMapObject("Stairs_Up.None", stairPos, _objectsContainer, false);
            SpawnMapObject("Portal.6", stairPos + new Vector2(2.2f, 0f), _objectsContainer, false);

            var stairTriggerGo = new GameObject("Gateway_Procedural_Dungeon_Stairs");
            stairTriggerGo.transform.SetParent(_objectsContainer, false);
            stairTriggerGo.transform.position = stairPos;
            var stairTrigger = stairTriggerGo.AddComponent<PMDRoomGatewayTrigger>();
            stairTrigger.targetZoneIndex = nextDungeonZone;
            stairTrigger.arrivalEntranceSide = RoomEntranceSide.Stairs;
            stairTrigger.randomizeSeedOnEnter = true;
            stairTrigger.gatewayLabel = $"🌀 Cầu Thang Bí Cảnh (Sinh Map Mới) ➔ {_zones[nextDungeonZone].displayName}";
        }

        private void CreateDirectionalGateway(
            string objName,
            Vector2 gatePos,
            int targetZoneIdx,
            RoomEntranceSide arrivalSide,
            string label,
            bool isHorizontalArch)
        {
            SpawnMapObject("Portal_Small.None", gatePos, _objectsContainer, false);

            if (isHorizontalArch)
            {
                SpawnCustomProp(CustomPropType.LanternPost, gatePos + new Vector2(-2.2f, 0.2f), _objectsContainer, true);
                SpawnCustomProp(CustomPropType.LanternPost, gatePos + new Vector2(2.2f, 0.2f), _objectsContainer, true);
                SpawnMapObject("Sign_Crossroads.Flip", gatePos + new Vector2(-3.2f, -0.4f), _objectsContainer, true);
            }
            else
            {
                SpawnCustomProp(CustomPropType.LanternPost, gatePos + new Vector2(0.2f, 2.0f), _objectsContainer, true);
                SpawnCustomProp(CustomPropType.LanternPost, gatePos + new Vector2(0.2f, -2.0f), _objectsContainer, true);
                SpawnMapObject("Sign_Crossroads.Flip", gatePos + new Vector2(-0.6f, 2.8f), _objectsContainer, true);
            }

            var triggerGo = new GameObject(objName);
            triggerGo.transform.SetParent(_objectsContainer, false);
            triggerGo.transform.position = gatePos;

            var trigger = triggerGo.AddComponent<PMDRoomGatewayTrigger>();
            trigger.targetZoneIndex = targetZoneIdx;
            trigger.arrivalEntranceSide = arrivalSide;
            trigger.randomizeSeedOnEnter = false;
            trigger.gatewayLabel = label;
        }

        // =========================================================================================
        // PROCEDURAL 3D Y-SORTED TREES, BUILDINGS, LANDMARKS, BOULDERS & FLOWERS
        // =========================================================================================
        private void PopulateProceduralRegionScenery(PMDMapZoneInfo zone, System.Random rng)
        {
            GetBiomeSceneryTypes(zone.biomeStyle, out BiomeTreeType primaryTree, out BiomeTreeType secondaryTree,
                out CustomPropType boulderType, out CustomPropType bushType, out string flowerPrefix);

            var placedPositions = new List<Vector2>();

            // 1. Spawn Central Hub Landmarks (Village Houses with Indoor Doors, Campfire, Guild Board, Storage, etc.)
            PopulateCentralHubLandmarks(zone, rng, placedPositions);

            // 2. Spawn 3D Y-Sorted Trees & Boulders along the Inner Forest/Cliff Edges of the 96x72 Grid
            for (int gy = 4; gy < RoomGridHeight - 4; gy += 3)
            {
                for (int gx = 4; gx < RoomGridWidth - 4; gx += 3)
                {
                    if (_currentGrid[gx, gy] != RoomTileType.TreeOrCliffWall)
                        continue;

                    // Only place 3D trees/boulders on wall tiles that border walkable Grass
                    if (!IsNeighborGrassOrWall(gx, gy))
                        continue;

                    float wx = -RoomHalfWidth + gx + 0.5f + RandRange(rng, -0.45f, 0.45f);
                    float wy = -RoomHalfHeight + gy + 0.5f + RandRange(rng, -0.45f, 0.45f);
                    Vector2 pos = new Vector2(wx, wy);

                    // Keep clear of the 4 Gateways
                    if (Mathf.Abs(wx) < 4.5f && Mathf.Abs(wy) > RoomHalfHeight - 8.5f)
                        continue;
                    if (Mathf.Abs(wy) < 4.5f && Mathf.Abs(wx) > RoomHalfWidth - 8.5f)
                        continue;

                    if (IsTooCloseToExisting(pos, placedPositions, 2.3f))
                        continue;

                    placedPositions.Add(pos);

                    if (zone.biomeStyle == RegionBiomeStyle.RedRockCanyon || zone.biomeStyle == RegionBiomeStyle.StarlightApex)
                    {
                        if (rng.NextDouble() < 0.58)
                            SpawnCustomProp(boulderType, pos, _objectsContainer, true);
                        else
                            SpawnBiomeTree(rng.NextDouble() < 0.7 ? primaryTree : secondaryTree, pos, _objectsContainer, true);
                    }
                    else
                    {
                        if (rng.NextDouble() < 0.82)
                            SpawnBiomeTree(rng.NextDouble() < 0.7 ? primaryTree : secondaryTree, pos, _objectsContainer, true);
                        else
                            SpawnCustomProp(boulderType, pos, _objectsContainer, true);
                    }
                }
            }

            // 3. Populate each of the 8 Outer Clearings with Procedural Sub-Camps, Shrines, Flowers, Bushes & Lanterns
            for (int c = 1; c < _activeClearings.Count; c++)
            {
                var cl = _activeClearings[c];
                Vector2 center = cl.center;

                // Place a centerpiece prop in each clearing (avoiding the staircase clearing if too close)
                Vector2 propPos = center + new Vector2(RandRange(rng, -2.8f, 2.8f), RandRange(rng, -2.2f, 2.2f));
                if (IsWorldPointWalkable(propPos) && !IsTooCloseToExisting(propPos, placedPositions, 2.5f))
                {
                    placedPositions.Add(propPos);
                    SpawnClearingFeatureProp(zone.biomeStyle, c, propPos, rng);
                }

                // Scatter bushes, flowers, and lantern posts around the clearing
                int decorCount = 7;
                for (int d = 0; d < decorCount; d++)
                {
                    Vector2 dPos = center + new Vector2(
                        RandRange(rng, -cl.radiusX * 0.75f, cl.radiusX * 0.75f),
                        RandRange(rng, -cl.radiusY * 0.75f, cl.radiusY * 0.75f));

                    if (!IsWorldPointWalkable(dPos) || IsTooCloseToExisting(dPos, placedPositions, 1.8f))
                        continue;

                    int gx = Mathf.Clamp(Mathf.FloorToInt(dPos.x + RoomHalfWidth), 0, RoomGridWidth - 1);
                    int gy = Mathf.Clamp(Mathf.FloorToInt(dPos.y + RoomHalfHeight), 0, RoomGridHeight - 1);
                    if (_currentGrid[gx, gy] == RoomTileType.Road || _currentGrid[gx, gy] == RoomTileType.Bridge)
                        continue;

                    placedPositions.Add(dPos);
                    double roll = rng.NextDouble();
                    if (roll < 0.50)
                    {
                        int flowerVariant = rng.Next(1, 6);
                        SpawnMapObject($"{flowerPrefix}_{flowerVariant}.Flip", dPos, _objectsContainer, false);
                    }
                    else if (roll < 0.82)
                    {
                        SpawnCustomProp(bushType, dPos, _objectsContainer, false);
                    }
                    else
                    {
                        SpawnCustomProp(CustomPropType.LanternPost, dPos, _objectsContainer, true);
                    }
                }
            }
        }

        private void PopulateCentralHubLandmarks(PMDMapZoneInfo zone, System.Random rng, List<Vector2> placedPositions)
        {
            // Keep (0, -1.5) clear for Player Spawn!
            placedPositions.Add(new Vector2(0f, -1.5f));
            placedPositions.Add(Vector2.zero);

            switch (zone.biomeStyle)
            {
                case RegionBiomeStyle.MeadowVillage:
                {
                    // Village Houses with working Indoor Room Doors!
                    Vector2 house1Pos = new Vector2(-6.5f, 4.2f);
                    Vector2 house2Pos = new Vector2(6.5f, 4.2f);
                    SpawnMapObject("House_Normal.None", house1Pos, _objectsContainer, true);
                    SpawnMapObject("House_Normal.None", house2Pos, _objectsContainer, true);
                    placedPositions.Add(house1Pos);
                    placedPositions.Add(house2Pos);

                    // Door 1 -> PostOffice (Index 8)
                    CreateIndoorDoorTrigger("Door_To_PostOffice", house1Pos + new Vector2(0f, -0.5f), 8,
                        house1Pos + new Vector2(0f, -2.0f), "🏠 Vào Bưu Điện Pelipper");

                    // Door 2 -> InsideHut (Index 9)
                    CreateIndoorDoorTrigger("Door_To_InsideHut", house2Pos + new Vector2(0f, -0.5f), 9,
                        house2Pos + new Vector2(0f, -2.0f), "🏠 Vào Nhà Trọ Khám Phá");

                    SpawnMapObject("Tent.Flip", new Vector2(-8.2f, -2.5f), _objectsContainer, true);
                    SpawnMapObject("Tent_Plain.Flip", new Vector2(8.2f, -2.5f), _objectsContainer, true);
                    SpawnMapObject("Campfire.Flip", new Vector2(0f, 2.2f), _objectsContainer, true);
                    SpawnMapObject("Mission_Board.None", new Vector2(-3.2f, 3.5f), _objectsContainer, true);
                    SpawnMapObject("Storage.Flip", new Vector2(3.2f, 3.5f), _objectsContainer, true);
                    SpawnMapObject("Berry_Basket_Red.None", new Vector2(-4.5f, -3.5f), _objectsContainer, true);
                    SpawnMapObject("Berry_Basket_Blue.None", new Vector2(4.5f, -3.5f), _objectsContainer, true);
                    SpawnMapObject("Assembly.Flip", new Vector2(0f, 5.2f), _objectsContainer, false);
                    break;
                }

                case RegionBiomeStyle.DeepWoodland:
                {
                    SpawnMapObject("Fairy_Ring.None", new Vector2(0f, 2.8f), _objectsContainer, false);
                    SpawnMapObject("Trunk_Large.None", new Vector2(-6.0f, 3.5f), _objectsContainer, true);
                    SpawnMapObject("Trunk_Large.None", new Vector2(6.0f, 3.5f), _objectsContainer, true);
                    SpawnMapObject("Tent.Flip", new Vector2(-5.5f, -3.2f), _objectsContainer, true);
                    SpawnMapObject("Campfire.Flip", new Vector2(0f, -3.8f), _objectsContainer, true);
                    SpawnMapObject("Logs_Large.Flip", new Vector2(5.2f, -3.2f), _objectsContainer, true);
                    SpawnMapObject("Berry_Basket_Blue.None", new Vector2(-3.2f, 2.2f), _objectsContainer, true);
                    break;
                }

                case RegionBiomeStyle.RedRockCanyon:
                {
                    SpawnMapObject("Campfire.Flip", new Vector2(0f, 2.5f), _objectsContainer, true);
                    SpawnMapObject("Tent.Flip", new Vector2(-6.2f, 3.2f), _objectsContainer, true);
                    SpawnMapObject("Tent_Plain.Flip", new Vector2(6.2f, 3.2f), _objectsContainer, true);
                    SpawnMapObject("Mission_Board.None", new Vector2(-3.5f, 3.8f), _objectsContainer, true);
                    SpawnMapObject("Chest.None", new Vector2(3.8f, 3.8f), _objectsContainer, true);
                    SpawnCustomProp(CustomPropType.BoulderCanyon, new Vector2(-7.5f, -3.5f), _objectsContainer, true);
                    SpawnCustomProp(CustomPropType.BoulderCanyon, new Vector2(7.5f, -3.5f), _objectsContainer, true);
                    break;
                }

                case RegionBiomeStyle.TropicalArchipelago:
                {
                    SpawnMapObject("Sea_Cradle.None", new Vector2(0f, 3.2f), _objectsContainer, true);
                    SpawnMapObject("BeachWaves", new Vector2(-5.5f, -4.5f), _objectsContainer, false);
                    SpawnMapObject("BeachWaves", new Vector2(5.5f, -4.5f), _objectsContainer, false);
                    SpawnMapObject("Tent.Flip", new Vector2(-6.2f, 2.5f), _objectsContainer, true);
                    SpawnMapObject("Campfire.Flip", new Vector2(5.2f, 2.2f), _objectsContainer, true);
                    SpawnMapObject("Chest_Open.None", new Vector2(0f, 5.5f), _objectsContainer, true);
                    break;
                }

                case RegionBiomeStyle.SakuraSanctuary:
                {
                    SpawnMapObject("LuminousSpringAnim", new Vector2(0f, 4.2f), _objectsContainer, true);
                    SpawnMapObject("Evolution.None", new Vector2(0f, 1.8f), _objectsContainer, false);
                    SpawnMapObject("Flowerpot_Pink.Flip", new Vector2(-4.2f, 2.5f), _objectsContainer, true);
                    SpawnMapObject("Flowerpot_White.Flip", new Vector2(4.2f, 2.5f), _objectsContainer, true);
                    SpawnMapObject("Fairy_Ring.None", new Vector2(-5.5f, -2.8f), _objectsContainer, false);
                    SpawnMapObject("Fairy_Ring.None", new Vector2(5.5f, -2.8f), _objectsContainer, false);
                    break;
                }

                case RegionBiomeStyle.GlacialHighlands:
                {
                    SpawnMapObject("Assembly_Snow.Flip", new Vector2(0f, 3.8f), _objectsContainer, false);
                    SpawnMapObject("Campfire.Flip", new Vector2(0f, 1.5f), _objectsContainer, true);
                    SpawnMapObject("Tent.Flip", new Vector2(-5.8f, 2.8f), _objectsContainer, true);
                    SpawnMapObject("Tent_Plain.Flip", new Vector2(5.8f, 2.8f), _objectsContainer, true);
                    SpawnMapObject("Logs_Stacked.Flip", new Vector2(-4.2f, -2.8f), _objectsContainer, true);
                    SpawnCustomProp(CustomPropType.BoulderSnow, new Vector2(5.5f, -3.2f), _objectsContainer, true);
                    break;
                }

                case RegionBiomeStyle.AutumnGuildHighlands:
                {
                    Vector2 guildHousePos = new Vector2(0f, 4.8f);
                    SpawnMapObject("House_Normal.None", guildHousePos, _objectsContainer, true);
                    CreateIndoorDoorTrigger("Door_Guild_To_PostOffice", guildHousePos + new Vector2(0f, -0.5f), 8,
                        guildHousePos + new Vector2(0f, -2.0f), "🏠 Vào Đại Sảnh Hội Quán");

                    SpawnMapObject("Block_Guild.None", new Vector2(-5.5f, 3.2f), _objectsContainer, true);
                    SpawnMapObject("Block_Vault.None", new Vector2(5.5f, 3.2f), _objectsContainer, true);
                    SpawnMapObject("Assembly.Flip", new Vector2(-3.2f, 2.0f), _objectsContainer, false);
                    SpawnMapObject("Mission_Board.None", new Vector2(3.2f, 2.0f), _objectsContainer, true);
                    SpawnMapObject("Chest.None", new Vector2(-4.8f, -3.0f), _objectsContainer, true);
                    SpawnMapObject("Chest_Open.None", new Vector2(4.8f, -3.0f), _objectsContainer, true);
                    break;
                }

                case RegionBiomeStyle.StarlightApex:
                {
                    SpawnMapObject("Updraft.None", new Vector2(0f, 3.2f), _objectsContainer, false);
                    SpawnMapObject("Evolution.None", new Vector2(-5.2f, 2.5f), _objectsContainer, false);
                    SpawnMapObject("Evolution.None", new Vector2(5.2f, 2.5f), _objectsContainer, false);
                    SpawnMapObject("Chest.None", new Vector2(-3.5f, 4.5f), _objectsContainer, true);
                    SpawnMapObject("Chest_Open.None", new Vector2(3.5f, 4.5f), _objectsContainer, true);
                    SpawnCustomProp(CustomPropType.BoulderPeak, new Vector2(-6.5f, -3.0f), _objectsContainer, true);
                    SpawnCustomProp(CustomPropType.BoulderPeak, new Vector2(6.5f, -3.0f), _objectsContainer, true);
                    break;
                }
            }
        }

        private void CreateIndoorDoorTrigger(string name, Vector2 doorPos, int indoorZoneIndex, Vector2 returnPos, string label)
        {
            SpawnMapObject("Portal_Small.None", doorPos, _objectsContainer, false);
            var go = new GameObject(name);
            go.transform.SetParent(_objectsContainer, false);
            go.transform.position = doorPos;

            var trigger = go.AddComponent<PMDRoomGatewayTrigger>();
            trigger.targetZoneIndex = indoorZoneIndex;
            trigger.arrivalEntranceSide = RoomEntranceSide.Center;
            trigger.returnOutdoorPos = returnPos;
            trigger.gatewayLabel = label;
        }

        private void SpawnClearingFeatureProp(RegionBiomeStyle style, int clearingIndex, Vector2 pos, System.Random rng)
        {
            switch (clearingIndex % 4)
            {
                case 0:
                    SpawnMapObject("Campfire.Flip", pos, _objectsContainer, true);
                    SpawnMapObject("Stump_Table.None", pos + new Vector2(1.8f, 0.4f), _objectsContainer, true);
                    SpawnMapObject("Stump_Chair.None", pos + new Vector2(2.8f, 0.4f), _objectsContainer, false);
                    break;
                case 1:
                    if (style == RegionBiomeStyle.GlacialHighlands)
                        SpawnMapObject("Assembly_Snow.Flip", pos, _objectsContainer, false);
                    else if (style == RegionBiomeStyle.TropicalArchipelago)
                        SpawnMapObject("Sea_Cradle.None", pos, _objectsContainer, true);
                    else
                        SpawnMapObject("Fairy_Ring.None", pos, _objectsContainer, false);
                    break;
                case 2:
                    SpawnMapObject("Tent_Plain.Flip", pos, _objectsContainer, true);
                    SpawnMapObject("Chest.None", pos + new Vector2(2.0f, -0.4f), _objectsContainer, true);
                    break;
                case 3:
                    SpawnMapObject("Trunk_Small.None", pos, _objectsContainer, true);
                    SpawnMapObject("Berry_Basket_Red.None", pos + new Vector2(1.6f, -0.3f), _objectsContainer, true);
                    break;
            }
        }

        private static void GetBiomeSceneryTypes(
            RegionBiomeStyle style,
            out BiomeTreeType primaryTree,
            out BiomeTreeType secondaryTree,
            out CustomPropType boulderType,
            out CustomPropType bushType,
            out string flowerPrefix)
        {
            switch (style)
            {
                case RegionBiomeStyle.DeepWoodland:
                    primaryTree = BiomeTreeType.DeepTeal;
                    secondaryTree = BiomeTreeType.WoodlandMoss;
                    boulderType = CustomPropType.BoulderMeadow;
                    bushType = CustomPropType.BushTeal;
                    flowerPrefix = "Flowers_Town";
                    break;

                case RegionBiomeStyle.RedRockCanyon:
                    primaryTree = BiomeTreeType.GoldenAutumn;
                    secondaryTree = BiomeTreeType.WoodlandMoss;
                    boulderType = CustomPropType.BoulderCanyon;
                    bushType = CustomPropType.BushAutumn;
                    flowerPrefix = "Flowers_Tropical";
                    break;

                case RegionBiomeStyle.TropicalArchipelago:
                    primaryTree = BiomeTreeType.MeadowGreen;
                    secondaryTree = BiomeTreeType.GoldenAutumn;
                    boulderType = CustomPropType.BoulderCanyon;
                    bushType = CustomPropType.BushMeadow;
                    flowerPrefix = "Flowers_Tropical";
                    break;

                case RegionBiomeStyle.SakuraSanctuary:
                    primaryTree = BiomeTreeType.SakuraPink;
                    secondaryTree = BiomeTreeType.MeadowGreen;
                    boulderType = CustomPropType.BoulderMeadow;
                    bushType = CustomPropType.BushMeadow;
                    flowerPrefix = "Flowers_Town";
                    break;

                case RegionBiomeStyle.GlacialHighlands:
                    primaryTree = BiomeTreeType.GlacialSnow;
                    secondaryTree = BiomeTreeType.FrostTippedPine;
                    boulderType = CustomPropType.BoulderSnow;
                    bushType = CustomPropType.BushSnow;
                    flowerPrefix = "Flowers_Town";
                    break;

                case RegionBiomeStyle.AutumnGuildHighlands:
                    primaryTree = BiomeTreeType.GoldenAutumn;
                    secondaryTree = BiomeTreeType.MeadowGreen;
                    boulderType = CustomPropType.BoulderCanyon;
                    bushType = CustomPropType.BushAutumn;
                    flowerPrefix = "Flowers_Town";
                    break;

                case RegionBiomeStyle.StarlightApex:
                    primaryTree = BiomeTreeType.FrostTippedPine;
                    secondaryTree = BiomeTreeType.DeepTeal;
                    boulderType = CustomPropType.BoulderPeak;
                    bushType = CustomPropType.BushTeal;
                    flowerPrefix = "Flowers_Tropical";
                    break;

                case RegionBiomeStyle.MeadowVillage:
                default:
                    primaryTree = BiomeTreeType.MeadowGreen;
                    secondaryTree = BiomeTreeType.WoodlandMoss;
                    boulderType = CustomPropType.BoulderMeadow;
                    bushType = CustomPropType.BushMeadow;
                    flowerPrefix = "Flowers_Town";
                    break;
            }
        }

        private void PopulateProceduralRegionItems(System.Random rng)
        {
            string[] itemFiles =
            {
                "Apple_Red.None",
                "Apple_Gold.None",
                "Berry_Oran.None",
                "Berry_Sitrus.None",
                "Berry_Lum.None",
                "Apricorn_Red.None",
                "Apricorn_Blue.None",
                "Apricorn_Yellow.None",
                "Stone_Blue.None",
                "Stone_Red.None",
                "Stone_Gold.None",
                "Stone_LightBlue.None"
            };

            // Spawn 2 pickable items in each of the 9 clearings (18 items per region)
            for (int c = 0; c < _activeClearings.Count; c++)
            {
                var cl = _activeClearings[c];
                for (int i = 0; i < 2; i++)
                {
                    Vector2 candidate = cl.center + new Vector2(
                        RandRange(rng, -cl.radiusX * 0.55f, cl.radiusX * 0.55f),
                        RandRange(rng, -cl.radiusY * 0.55f, cl.radiusY * 0.55f));

                    candidate = FindNearestWalkablePosition(candidate);
                    string itemFile = itemFiles[(c * 2 + i + rng.Next(0, 3)) % itemFiles.Length];
                    SpawnItemAt(itemFile, candidate);
                }
            }
        }

        private void SpawnItemAt(string itemFile, Vector2 pos)
        {
            Texture2D tex = LoadItemTexture(itemFile);
            if (tex == null)
                return;

            var go = new GameObject($"Item_{itemFile}");
            go.transform.SetParent(_itemsContainer, false);
            go.transform.position = pos;

            var sr = go.AddComponent<SpriteRenderer>();
            int frameW = Mathf.Min(tex.width, tex.height);
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, frameW, tex.height), new Vector2(0.5f, 0.5f), MapPPU);

            go.AddComponent<PMDYSorter>();
            var pickup = go.AddComponent<PMDWorldItemPickup>();
            pickup.itemName = itemFile.Replace(".None", "").Replace("_", " ");
            pickup.healAmount = itemFile.Contains("Gold") ? 75f : 35f;
            _spawnedItems.Add(pickup);
        }

        // =========================================================================================
        // BIOME TREES, CUSTOM PROPS & MULTI-FRAME OBJECT SPAWNERS
        // =========================================================================================
        private GameObject SpawnBiomeTree(BiomeTreeType treeType, Vector2 worldPos, Transform parent, bool addCollider)
        {
            Texture2D tex = GetBiomeTreeTexture(treeType);
            if (tex == null)
                return null;

            var go = new GameObject($"Tree_{treeType}");
            go.transform.SetParent(parent, false);
            go.transform.position = worldPos;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.14f), MapPPU);
            go.AddComponent<PMDYSorter>();

            if (addCollider)
            {
                var col = go.AddComponent<BoxCollider2D>();
                col.size = new Vector2(1.45f, 0.95f);
                col.offset = new Vector2(0f, 0.42f);
            }

            return go;
        }

        private GameObject SpawnCustomProp(CustomPropType propType, Vector2 worldPos, Transform parent, bool addCollider)
        {
            Texture2D tex = GetCustomPropTexture(propType);
            if (tex == null)
                return null;

            var go = new GameObject($"Prop_{propType}");
            go.transform.SetParent(parent, false);
            go.transform.position = worldPos;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.15f), MapPPU);
            go.AddComponent<PMDYSorter>();

            if (addCollider)
            {
                var col = go.AddComponent<BoxCollider2D>();
                float wUnits = Mathf.Max(0.65f, (tex.width / MapPPU) * 0.65f);
                float hUnits = Mathf.Max(0.45f, (tex.height / MapPPU) * 0.35f);
                col.size = new Vector2(wUnits, hUnits);
                col.offset = new Vector2(0f, hUnits * 0.45f);
            }

            if (propType == CustomPropType.LanternPost)
            {
                PMDAtmosphere.AttachPointLight(go, new Color(1.0f, 0.85f, 0.45f), 1.25f, 4.5f, true);
            }

            return go;
        }

        private static Texture2D GetBiomeTreeTexture(BiomeTreeType treeType)
        {
            if (CachedTreeTextures.TryGetValue(treeType, out Texture2D cached) && cached != null)
                return cached;

            Texture2D baseTree = LoadObjectTexture("Tree_Town.Flip");
            if (baseTree == null)
                return null;

            if (treeType == BiomeTreeType.MeadowGreen)
            {
                CachedTreeTextures[treeType] = baseTree;
                return baseTree;
            }

            int w = baseTree.width;
            int h = baseTree.height;
            Color[] pixels = baseTree.GetPixels();

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int idx = y * w + x;
                    Color c = pixels[idx];
                    if (c.a < 0.05f)
                        continue;

                    bool isFoliage = (c.g > c.r * 1.03f && c.g > c.b * 1.15f && c.g > 0.22f);
                    if (!isFoliage)
                        continue;

                    float lum = c.r * 0.299f + c.g * 0.587f + c.b * 0.114f;

                    switch (treeType)
                    {
                        case BiomeTreeType.WoodlandMoss:
                            c = new Color(
                                Mathf.Clamp01(c.r * 0.72f),
                                Mathf.Clamp01(c.g * 0.92f),
                                Mathf.Clamp01(c.b * 1.25f + 0.08f),
                                c.a);
                            break;

                        case BiomeTreeType.DeepTeal:
                            c = new Color(
                                Mathf.Clamp01(lum * 0.32f),
                                Mathf.Clamp01(lum * 0.88f + 0.06f),
                                Mathf.Clamp01(lum * 0.76f + 0.14f),
                                c.a);
                            break;

                        case BiomeTreeType.SakuraPink:
                            c = new Color(
                                Mathf.Clamp01(lum * 1.12f + 0.22f),
                                Mathf.Clamp01(lum * 0.58f + 0.08f),
                                Mathf.Clamp01(lum * 0.84f + 0.18f),
                                c.a);
                            break;

                        case BiomeTreeType.GoldenAutumn:
                            c = new Color(
                                Mathf.Clamp01(lum * 1.18f + 0.18f),
                                Mathf.Clamp01(lum * 0.74f + 0.04f),
                                Mathf.Clamp01(lum * 0.24f),
                                c.a);
                            break;

                        case BiomeTreeType.FrostTippedPine:
                            if (lum > 0.56f || y > h * 0.62f)
                            {
                                c = new Color(
                                    Mathf.Clamp01(0.78f + lum * 0.22f),
                                    Mathf.Clamp01(0.88f + lum * 0.12f),
                                    Mathf.Clamp01(0.95f + lum * 0.05f),
                                    c.a);
                            }
                            else
                            {
                                c = new Color(
                                    Mathf.Clamp01(lum * 0.38f),
                                    Mathf.Clamp01(lum * 0.72f + 0.05f),
                                    Mathf.Clamp01(lum * 0.68f + 0.10f),
                                    c.a);
                            }
                            break;

                        case BiomeTreeType.GlacialSnow:
                            c = new Color(
                                Mathf.Clamp01(0.72f + lum * 0.26f),
                                Mathf.Clamp01(0.84f + lum * 0.15f),
                                Mathf.Clamp01(0.92f + lum * 0.08f),
                                c.a);
                            break;
                    }

                    pixels[idx] = c;
                }
            }

            var outTex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = $"Tree_{treeType}"
            };
            outTex.SetPixels(pixels);
            outTex.Apply(false);
            CachedTreeTextures[treeType] = outTex;
            return outTex;
        }

        private static Texture2D GetCustomPropTexture(CustomPropType propType)
        {
            if (CachedCustomProps.TryGetValue(propType, out Texture2D cached) && cached != null)
                return cached;

            Texture2D tex;
            switch (propType)
            {
                case CustomPropType.LanternPost:
                    tex = BuildPixelArtLanternTexture();
                    break;
                case CustomPropType.BridgeHorizontal:
                    tex = BuildPixelArtBridgeTexture();
                    break;
                case CustomPropType.BushMeadow:
                case CustomPropType.BushAutumn:
                case CustomPropType.BushTeal:
                case CustomPropType.BushSnow:
                    tex = BuildPixelArtBushTexture(propType);
                    break;
                default:
                    tex = BuildPixelArtBoulderTexture(propType);
                    break;
            }

            CachedCustomProps[propType] = tex;
            return tex;
        }

        private static Texture2D BuildPixelArtLanternTexture()
        {
            int w = 24, h = 48;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "Custom_LanternPost"
            };
            Color[] px = new Color[w * h];
            Color outline = new Color(0.14f, 0.10f, 0.08f, 1f);
            Color woodDark = new Color(0.38f, 0.24f, 0.14f, 1f);
            Color woodLight = new Color(0.56f, 0.36f, 0.20f, 1f);
            Color glowCore = new Color(1.0f, 0.94f, 0.58f, 1f);
            Color glowMid = new Color(0.98f, 0.72f, 0.24f, 1f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dBase = Vector2.Distance(new Vector2(x, y), new Vector2(12f, 6f));
                    if (dBase < 9f)
                    {
                        px[y * w + x] = new Color(1.0f, 0.85f, 0.35f, (1f - dBase / 9f) * 0.28f);
                    }

                    if (y >= 4 && y <= 38 && x >= 10 && x <= 13)
                    {
                        px[y * w + x] = (x == 10 || x == 13 || y == 4) ? outline : (x == 11 ? woodLight : woodDark);
                    }
                    if (y >= 35 && y <= 38 && x >= 6 && x <= 18)
                    {
                        px[y * w + x] = (y == 38 || y == 35 || x == 6 || x == 18) ? outline : woodLight;
                    }
                    if (y >= 26 && y <= 34 && x >= 15 && x <= 20)
                    {
                        bool border = (x == 15 || x == 20 || y == 26 || y == 34);
                        px[y * w + x] = border ? outline : ((y >= 29 && y <= 31 && x >= 17 && x <= 18) ? glowCore : glowMid);
                    }
                }
            }

            tex.SetPixels(px);
            tex.Apply(false);
            return tex;
        }

        private static Texture2D BuildPixelArtBoulderTexture(CustomPropType type)
        {
            int w = 40, h = 36;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = $"Custom_{type}"
            };
            Color[] px = new Color[w * h];

            Color hi, mid, shd, outline = new Color(0.12f, 0.12f, 0.15f, 1f);
            switch (type)
            {
                case CustomPropType.BoulderCanyon:
                    hi = new Color(0.84f, 0.56f, 0.34f, 1f);
                    mid = new Color(0.68f, 0.42f, 0.24f, 1f);
                    shd = new Color(0.46f, 0.26f, 0.16f, 1f);
                    break;
                case CustomPropType.BoulderSnow:
                    hi = new Color(0.92f, 0.97f, 1.0f, 1f);
                    mid = new Color(0.68f, 0.80f, 0.90f, 1f);
                    shd = new Color(0.45f, 0.58f, 0.72f, 1f);
                    break;
                case CustomPropType.BoulderPeak:
                    hi = new Color(0.54f, 0.52f, 0.68f, 1f);
                    mid = new Color(0.38f, 0.36f, 0.50f, 1f);
                    shd = new Color(0.22f, 0.20f, 0.32f, 1f);
                    break;
                default:
                    hi = new Color(0.68f, 0.72f, 0.68f, 1f);
                    mid = new Color(0.50f, 0.54f, 0.52f, 1f);
                    shd = new Color(0.34f, 0.38f, 0.36f, 1f);
                    break;
            }

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float nx = (x - 20f) / 16f;
                    float ny = (y - 14f) / 13f;
                    float n = Hash2D(x / 3, y / 3) * 0.18f;
                    float dist = nx * nx + ny * ny - n;
                    if (dist > 1.0f || y < 3)
                        continue;

                    if (dist > 0.78f || y == 3)
                        px[y * w + x] = outline;
                    else if (y > 19 || (x < 18 && y > 14))
                        px[y * w + x] = hi;
                    else if (y > 9)
                        px[y * w + x] = mid;
                    else
                        px[y * w + x] = shd;
                }
            }

            tex.SetPixels(px);
            tex.Apply(false);
            return tex;
        }

        private static Texture2D BuildPixelArtBushTexture(CustomPropType type)
        {
            int w = 32, h = 28;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = $"Custom_{type}"
            };
            Color[] px = new Color[w * h];
            Color outline = new Color(0.10f, 0.16f, 0.12f, 1f);
            Color hi, mid, shd;

            switch (type)
            {
                case CustomPropType.BushAutumn:
                    hi = new Color(0.96f, 0.72f, 0.24f, 1f);
                    mid = new Color(0.82f, 0.48f, 0.18f, 1f);
                    shd = new Color(0.58f, 0.30f, 0.12f, 1f);
                    break;
                case CustomPropType.BushTeal:
                    hi = new Color(0.32f, 0.84f, 0.66f, 1f);
                    mid = new Color(0.18f, 0.64f, 0.52f, 1f);
                    shd = new Color(0.10f, 0.42f, 0.38f, 1f);
                    break;
                case CustomPropType.BushSnow:
                    hi = new Color(0.92f, 0.97f, 1.0f, 1f);
                    mid = new Color(0.68f, 0.84f, 0.92f, 1f);
                    shd = new Color(0.36f, 0.58f, 0.68f, 1f);
                    break;
                default:
                    hi = new Color(0.56f, 0.90f, 0.38f, 1f);
                    mid = new Color(0.34f, 0.74f, 0.30f, 1f);
                    shd = new Color(0.20f, 0.52f, 0.22f, 1f);
                    break;
            }

            for (int y = 2; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float d1 = Vector2.Distance(new Vector2(x, y), new Vector2(11f, 12f)) / 9.5f;
                    float d2 = Vector2.Distance(new Vector2(x, y), new Vector2(21f, 12f)) / 9.5f;
                    float d3 = Vector2.Distance(new Vector2(x, y), new Vector2(16f, 16f)) / 10.0f;
                    float d = Mathf.Min(d1, Mathf.Min(d2, d3));
                    if (d > 1.0f)
                        continue;

                    if (d > 0.82f || y == 2)
                        px[y * w + x] = outline;
                    else if (y > 15)
                        px[y * w + x] = hi;
                    else if (y > 8)
                        px[y * w + x] = mid;
                    else
                        px[y * w + x] = shd;
                }
            }

            tex.SetPixels(px);
            tex.Apply(false);
            return tex;
        }

        private static Texture2D BuildPixelArtBridgeTexture()
        {
            int w = 96, h = 48;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "Custom_BridgeHorizontal"
            };
            Color[] px = new Color[w * h];
            Color outline = new Color(0.18f, 0.12f, 0.08f, 1f);
            Color plankLight = new Color(0.76f, 0.54f, 0.32f, 1f);
            Color plankDark = new Color(0.62f, 0.42f, 0.24f, 1f);
            Color railCol = new Color(0.48f, 0.30f, 0.16f, 1f);

            for (int y = 4; y < h - 4; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (y == 4 || y == h - 5)
                    {
                        px[y * w + x] = outline;
                    }
                    else if (y < 9 || y > h - 10)
                    {
                        px[y * w + x] = (x % 16 < 3) ? outline : railCol;
                    }
                    else
                    {
                        bool seam = (x % 8 == 0);
                        px[y * w + x] = seam ? outline : (((x / 8) % 2 == 0) ? plankLight : plankDark);
                    }
                }
            }

            tex.SetPixels(px);
            tex.Apply(false);
            return tex;
        }

        private GameObject SpawnMapObject(string objectFileName, Vector2 worldPos, Transform parent, bool addCollider)
        {
            Texture2D tex = LoadObjectTexture(objectFileName);
            if (tex == null)
                return null;

            var go = new GameObject($"Obj_{objectFileName}");
            go.transform.SetParent(parent, false);
            go.transform.position = worldPos;

            int frameHeight = tex.height;
            int frameWidth = tex.width;
            bool isMultiFrameSheet = (tex.width > tex.height && tex.width % tex.height == 0);

            if (isMultiFrameSheet)
            {
                frameWidth = tex.height;
            }

            var sr = go.AddComponent<SpriteRenderer>();
            if (isMultiFrameSheet)
            {
                if (!CachedAnimatedFrames.TryGetValue(objectFileName, out Sprite[] frames) || frames == null || frames.Length == 0 || frames[0] == null)
                {
                    int count = tex.width / frameWidth;
                    frames = new Sprite[count];
                    for (int i = 0; i < count; i++)
                    {
                        frames[i] = Sprite.Create(tex, new Rect(i * frameWidth, 0, frameWidth, frameHeight), new Vector2(0.5f, 0.15f), MapPPU);
                    }
                    CachedAnimatedFrames[objectFileName] = frames;
                }

                sr.sprite = frames[0];
                var anim = go.AddComponent<PMDAmbientPropAnimator>();
                anim.frames = frames;
                anim.fps = objectFileName.StartsWith("Flowers", StringComparison.OrdinalIgnoreCase) ? 4.5f : 6.5f;
            }
            else
            {
                sr.sprite = Sprite.Create(tex, new Rect(0, 0, frameWidth, frameHeight), new Vector2(0.5f, 0.15f), MapPPU);
            }

            go.AddComponent<PMDYSorter>();

            if (addCollider)
            {
                var col = go.AddComponent<BoxCollider2D>();
                float wUnits = Mathf.Max(0.70f, (frameWidth / MapPPU) * 0.65f);
                float hUnits = Mathf.Max(0.50f, (frameHeight / MapPPU) * 0.35f);
                col.size = new Vector2(wUnits, hUnits);
                col.offset = new Vector2(0f, hUnits * 0.45f);
            }

            if (objectFileName.IndexOf("Campfire", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                PMDAtmosphere.AttachPointLight(go, new Color(1.0f, 0.58f, 0.18f), 1.55f, 5.8f, true);
            }
            else if (objectFileName.IndexOf("Portal", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     objectFileName.IndexOf("Stairs", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                PMDAtmosphere.AttachPointLight(go, new Color(0.40f, 0.75f, 1.0f), 1.35f, 4.8f, true);
            }
            else if (objectFileName.IndexOf("Assembly", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     objectFileName.IndexOf("Evolution", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     objectFileName.IndexOf("LuminousSpring", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                PMDAtmosphere.AttachPointLight(go, new Color(0.85f, 0.55f, 1.0f), 1.45f, 5.2f, true);
            }

            return go;
        }

        // =========================================================================================
        // MATH & TEXTURE LOADING HELPERS
        // =========================================================================================
        private static float RandRange(System.Random rng, float min, float max)
        {
            return min + (float)rng.NextDouble() * (max - min);
        }

        private static bool IsTooCloseToExisting(Vector2 pos, List<Vector2> existing, float minDist)
        {
            float minSq = minDist * minDist;
            for (int i = 0; i < existing.Count; i++)
            {
                if ((existing[i] - pos).sqrMagnitude < minSq)
                    return true;
            }
            return false;
        }

        private static float DistanceToSegment(float px, float py, float ax, float ay, float bx, float by)
        {
            float dx = bx - ax;
            float dy = by - ay;
            float lenSq = dx * dx + dy * dy;
            if (lenSq < 0.0001f)
                return Vector2.Distance(new Vector2(px, py), new Vector2(ax, ay));

            float t = Mathf.Clamp01(((px - ax) * dx + (py - ay) * dy) / lenSq);
            float projX = ax + t * dx;
            float projY = ay + t * dy;
            return Vector2.Distance(new Vector2(px, py), new Vector2(projX, projY));
        }

        private static float FractalNoise(float x, float y)
        {
            float n1 = Mathf.PerlinNoise(x, y);
            float n2 = Mathf.PerlinNoise(x * 2.13f + 31.7f, y * 2.13f + 19.3f) * 0.5f;
            return (n1 + n2) / 1.5f;
        }

        private static float Hash2D(int x, int y)
        {
            int h = x * 374761393 + y * 668265263;
            h = (h ^ (h >> 13)) * 1274126177;
            return ((h ^ (h >> 16)) & 0x7FFFFFFF) / (float)int.MaxValue;
        }

        private static Texture2D LoadMapTexture(string fileName)
        {
            string diskPath = Path.Combine(Application.dataPath, "Resources", "PMDCollab", "Maps", fileName + ".png");
            return PMDRuntimeCharacterLoader.LoadPixelTextureFromFile(diskPath, $"PMDCollab/Maps/{fileName}");
        }

        private static Texture2D LoadObjectTexture(string fileName)
        {
            string diskPath = Path.Combine(Application.dataPath, "Resources", "PMDCollab", "Objects", fileName + ".png");
            return PMDRuntimeCharacterLoader.LoadPixelTextureFromFile(diskPath, $"PMDCollab/Objects/{fileName}");
        }

        private static Texture2D LoadItemTexture(string fileName)
        {
            string diskPath = Path.Combine(Application.dataPath, "Resources", "PMDCollab", "Items", fileName + ".png");
            return PMDRuntimeCharacterLoader.LoadPixelTextureFromFile(diskPath, $"PMDCollab/Items/{fileName}");
        }
    }
}
