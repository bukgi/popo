using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace POKE.PMDCollab.OpenWorld
{
    /// <summary>
    /// Manages the Player, Recruited Companions, and Dynamic Wild Pokémon Spawning for the active
    /// Procedurally-Generated Region ("Vùng Đất / Phòng").
    /// When the player transitions to a new Region (or generates a new seed), old unrecruited wild Pokémon
    /// are cleared, recruited companions follow the player to the new entrance gate, and 22 new wild Pokémon
    /// are spawned across the 9 procedural clearings of the new room.
    /// </summary>
    public class PMDOpenWorldManager : MonoBehaviour
    {
        public static PMDOpenWorldManager Instance { get; private set; }

        [SerializeField] private PMDOpenWorldMapGenerator mapGenerator;
        [SerializeField] private PMDPlayerController playerController;

        private List<PMDCharacterData> _loadedCharacters = new List<PMDCharacterData>();
        private Sprite _shadowSprite;
        private Transform _recruitedPartyContainer;
        private Transform _wildMonstersContainer;

        public IReadOnlyList<PMDCharacterData> LoadedCharacters => _loadedCharacters;
        public PMDPlayerController Player => playerController;
        public PMDOpenWorldMapGenerator MapGenerator => mapGenerator;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            BootstrapWorld();
        }

        private void Update()
        {
            if (!Application.isPlaying)
                return;

            // Hotkey [G]: Generate a brand new random procedural map for the current Region!
            if (Keyboard.current != null && Keyboard.current.gKey.wasPressedThisFrame)
            {
                if (mapGenerator != null)
                {
                    mapGenerator.RegenerateCurrentRegionWithNewSeed();
                }
            }
        }

        private bool HasValidCharacterSprites()
        {
            if (playerController == null)
                return false;

            var sr = playerController.GetComponent<SpriteRenderer>();
            return sr != null && sr.sprite != null && sr.sprite.texture != null;
        }

        public void BootstrapWorld()
        {
            if (mapGenerator == null)
                mapGenerator = FindFirstObjectByType<PMDOpenWorldMapGenerator>();

            if (mapGenerator != null && !mapGenerator.HasValidSprites())
            {
                mapGenerator.GenerateWorld();
            }

            _loadedCharacters = PMDRuntimeCharacterLoader.LoadAllAvailableCharacters();
            if (_loadedCharacters.Count == 0)
            {
                Debug.LogError("[PMDOpenWorldManager] No PMDCollab characters found in Resources/PMDCollab/Pokemon!");
                return;
            }

            _shadowSprite = CreateEllipseShadowSprite();
            SpawnInitialPlayerAndRoomEntities();
        }

        private void SpawnInitialPlayerAndRoomEntities()
        {
            if (playerController == null)
            {
                playerController = FindFirstObjectByType<PMDPlayerController>();
            }

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (playerController != null && child == playerController.transform)
                    continue;
                DestroyImmediate(child.gameObject);
            }

            _recruitedPartyContainer = new GameObject("1_Recruited_Party_Companions").transform;
            _recruitedPartyContainer.SetParent(transform, false);

            _wildMonstersContainer = new GameObject("2_Active_Room_Wild_Monsters").transform;
            _wildMonstersContainer.SetParent(transform, false);

            var leadChar = FindCharacter("0000") ?? FindCharacter("0025") ?? _loadedCharacters[0];
            Vector2 startSpawn = new Vector2(0f, -1.5f);
            if (mapGenerator != null)
            {
                startSpawn = mapGenerator.FindNearestWalkablePosition(startSpawn);
            }

            if (playerController == null)
            {
                playerController = CreatePlayerEntity(leadChar, startSpawn);
            }
            else
            {
                playerController.ClearParty();
                playerController.Animator.CharacterData = leadChar;
                playerController.transform.position = startSpawn;
            }

            playerController.SetAvailableCharacters(_loadedCharacters);

            var camFollow = FindFirstObjectByType<PMDCameraFollow>();
            if (camFollow != null)
            {
                camFollow.Target = playerController.transform;
            }

            // Starting Partner Pokémon (Pikachu) starts safely inside the Pokéball until the Trainer throws [R]
            var pikachu = FindCharacter("0025") ?? FindCharacter("0133") ?? _loadedCharacters[Mathf.Min(1, _loadedCharacters.Count - 1)];
            if (pikachu != null)
            {
                playerController.SetActivePokeballSpecies(pikachu);
            }

            Physics2D.SyncTransforms();

            if (mapGenerator != null && mapGenerator.ActiveZone != null)
            {
                SpawnMonstersForActiveRegion(mapGenerator.ActiveZone);
            }
        }

        /// <summary>
        /// Called by PMDOpenWorldMapGenerator whenever the player transitions into a new Region (or generates a new seed).
        /// </summary>
        public void OnRegionChanged(PMDMapZoneInfo activeZone, Vector2 playerSpawnPos)
        {
            if (_loadedCharacters == null || _loadedCharacters.Count == 0)
            {
                _loadedCharacters = PMDRuntimeCharacterLoader.LoadAllAvailableCharacters();
            }
            if (_shadowSprite == null)
            {
                _shadowSprite = CreateEllipseShadowSprite();
            }

            if (_recruitedPartyContainer == null)
            {
                var foundParty = transform.Find("1_Recruited_Party_Companions");
                if (foundParty != null)
                {
                    _recruitedPartyContainer = foundParty;
                }
                else
                {
                    _recruitedPartyContainer = new GameObject("1_Recruited_Party_Companions").transform;
                    _recruitedPartyContainer.SetParent(transform, false);
                }
            }

            // Rescue any newly recruited companions so they are inside _recruitedPartyContainer before clearing wild monsters
            var activeMons = PMDWildPokemonAI.ActiveMons;
            for (int i = 0; i < activeMons.Count; i++)
            {
                var mon = activeMons[i];
                if (mon != null && mon.IsRecruited && mon.transform.parent != _recruitedPartyContainer)
                {
                    mon.transform.SetParent(_recruitedPartyContainer, true);
                }
            }

            // Move Player & Camera to the entrance of the new Region
            if (playerController == null)
                playerController = PMDPlayerController.Instance;

            if (playerController != null)
            {
                playerController.transform.position = playerSpawnPos;
                var rb = playerController.GetComponent<Rigidbody2D>();
                if (rb != null)
                    rb.linearVelocity = Vector2.zero;

                // Teleport all recruited party members right beside the player at the new room entrance
                for (int i = 0; i < _recruitedPartyContainer.childCount; i++)
                {
                    Transform comp = _recruitedPartyContainer.GetChild(i);
                    if (comp != null)
                    {
                        float angle = (i + 1) * 1.1f;
                        comp.position = playerSpawnPos + new Vector2(Mathf.Cos(angle) * 1.35f, Mathf.Sin(angle) * 1.15f);
                    }
                }
            }

            if (Camera.main != null)
            {
                Camera.main.transform.position = new Vector3(playerSpawnPos.x, playerSpawnPos.y, -10f);
            }

            // Spawn fresh Wild Pokémon across the new Region's procedural clearings
            if (activeZone != null)
            {
                SpawnMonstersForActiveRegion(activeZone);
            }
        }

        public void AdoptCompanion(PMDWildPokemonAI mon)
        {
            if (mon != null && _recruitedPartyContainer != null)
            {
                mon.transform.SetParent(_recruitedPartyContainer, true);
            }
        }

        public PMDWildPokemonAI SpawnCompanionForTrainer(PMDCharacterData speciesData, Vector2 summonPos, int slotIndex = 1)
        {
            if (speciesData == null)
                return null;

            if (_recruitedPartyContainer == null)
            {
                _recruitedPartyContainer = new GameObject("1_Recruited_Party_Companions").transform;
                _recruitedPartyContainer.SetParent(transform, false);
            }

            var mon = SpawnWildPokemon(speciesData, summonPos, "Pokémon Ra Săn", false, _recruitedPartyContainer);
            if (mon != null && playerController != null)
            {
                mon.RecruitToParty(playerController, slotIndex);
            }
            return mon;
        }

        public PMDCharacterData FindCharacterByName(string speciesName)
        {
            if (_loadedCharacters == null || _loadedCharacters.Count == 0)
                _loadedCharacters = PMDRuntimeCharacterLoader.LoadAllAvailableCharacters();

            for (int i = 0; i < _loadedCharacters.Count; i++)
            {
                if (string.Equals(_loadedCharacters[i].pokemonName, speciesName, StringComparison.OrdinalIgnoreCase))
                    return _loadedCharacters[i];
            }
            return null;
        }

        public void RegisterAndSummonCharacter(PMDCharacterData data)
        {
            if (data == null)
                return;

            bool exists = false;
            for (int i = 0; i < _loadedCharacters.Count; i++)
            {
                if (_loadedCharacters[i] != null && string.Equals(_loadedCharacters[i].pokemonId, data.pokemonId, StringComparison.OrdinalIgnoreCase))
                {
                    _loadedCharacters[i] = data;
                    exists = true;
                    break;
                }
            }

            if (!exists)
                _loadedCharacters.Add(data);

            if (playerController != null)
            {
                playerController.SetAvailableCharacters(_loadedCharacters);
                playerController.SummonOrSwapHuntingPokemon(data);
            }
        }

        private void SpawnMonstersForActiveRegion(PMDMapZoneInfo zone)
        {
            if (_wildMonstersContainer != null)
            {
                DestroyImmediate(_wildMonstersContainer.gameObject);
            }

            _wildMonstersContainer = new GameObject($"2_WildMonsters_{zone.zoneId}_Seed_{mapGenerator.CurrentSeed}").transform;
            _wildMonstersContainer.SetParent(transform, false);

            if (_loadedCharacters == null || _loadedCharacters.Count == 0)
                return;

            var rng = new System.Random(mapGenerator != null ? mapGenerator.CurrentSeed + 77 : 2026);
            int baseZoneLevel = 1;
            if (mapGenerator != null && !zone.isIndoor)
            {
                baseZoneLevel = 1 + mapGenerator.ActiveZoneIndex * 3;
            }

            if (zone.isIndoor)
            {
                for (int i = 0; i < 2; i++)
                {
                    var charData = PickSpeciesForZone(zone, i, rng);
                    Vector2 pos = new Vector2(i == 0 ? -2.2f : 2.2f, 0.2f);
                    SpawnWildPokemon(charData, pos, zone.displayName, false, _wildMonstersContainer, baseZoneLevel, false);
                }
                return;
            }

            var clearings = mapGenerator != null ? mapGenerator.ActiveClearings : null;
            if (clearings == null || clearings.Count == 0)
                return;

            // Spawn 2 to 3 Wild Pokémon inside each of the 9 Procedural Clearings (~22 Pokémon per massive 96x72 Region!)
            int spawnCounter = 0;
            for (int c = 0; c < clearings.Count; c++)
            {
                var cl = clearings[c];
                int countInClearing = cl.isCentralHub ? 4 : 2;
                if (!cl.isCentralHub && (c % 2 == 1))
                    countInClearing = 3;

                // Clearing 8 (Corner) spawns a Regional Boss if not yet defeated in this session!
                bool isBossClearing = (c == clearings.Count - 1);
                if (isBossClearing)
                {
                    countInClearing = 1; // 1 powerful Boss with bodyguards
                }

                for (int i = 0; i < countInClearing; i++)
                {
                    var charData = PickSpeciesForZone(zone, spawnCounter, rng);
                    Vector2 rawPos = cl.center + new Vector2(
                        (float)(rng.NextDouble() * (cl.radiusX * 1.1) - cl.radiusX * 0.55),
                        (float)(rng.NextDouble() * (cl.radiusY * 1.1) - cl.radiusY * 0.55));

                    Vector2 safePos = FindWalkableSpawnPoint(rawPos, 3.5f, rng);

                    // Central Hub of Region 1 is friendly; outer clearings and hostile regions have aggressive wild Pokémon
                    bool hostile = zone.hasHostileSpawns && (!cl.isCentralHub || i >= 2) && (spawnCounter % 2 == 1);
                    if (!zone.hasHostileSpawns && !cl.isCentralHub && i == countInClearing - 1)
                    {
                        hostile = true; // Even in Region 1, outer exploration clearings have some wild enemies to test skills!
                    }

                    int monLvl = baseZoneLevel + rng.Next(-1, 2);
                    monLvl = Mathf.Max(1, monLvl);

                    if (isBossClearing)
                    {
                        // Distinct high-level boss per region
                        string bossSpecies = mapGenerator.ActiveZoneIndex switch
                        {
                            0 => "0448", // Lucario mini-boss
                            1 => "0252", // Treecko
                            2 => "0006", // Charizard
                            3 => "0258", // Mudkip
                            4 => "0151", // Mew
                            5 => "0471", // Glaceon
                            6 => "0448", // Lucario
                            _ => "0151"  // Mew Supreme Boss
                        };
                        var bossChar = FindCharacter(bossSpecies) ?? charData;
                        int bossLvl = baseZoneLevel + 4;
                        SpawnWildPokemon(bossChar, cl.center, $"Trùm {cl.clearingName}", true, _wildMonstersContainer, bossLvl, true);
                    }
                    else
                    {
                        SpawnWildPokemon(charData, safePos, cl.clearingName, hostile, _wildMonstersContainer, monLvl, false);
                    }

                    spawnCounter++;
                }
            }
        }

        private PMDCharacterData PickSpeciesForZone(PMDMapZoneInfo zone, int index, System.Random rng)
        {
            if (zone.preferredSpecies != null && zone.preferredSpecies.Length > 0 && rng.NextDouble() < 0.78)
            {
                string speciesFolder = zone.preferredSpecies[(index + rng.Next(0, zone.preferredSpecies.Length)) % zone.preferredSpecies.Length];
                string idPrefix = speciesFolder.Split('_')[0];
                var found = FindCharacter(idPrefix);
                if (found != null && found.pokemonId != "0000")
                    return found;
            }

            for (int attempt = 0; attempt < _loadedCharacters.Count; attempt++)
            {
                var candidate = _loadedCharacters[(index + attempt + rng.Next(0, _loadedCharacters.Count)) % _loadedCharacters.Count];
                if (candidate != null && candidate.pokemonId != "0000")
                    return candidate;
            }

            return _loadedCharacters[_loadedCharacters.Count - 1];
        }

        private Vector2 FindWalkableSpawnPoint(Vector2 center, float radius, System.Random rng)
        {
            for (int attempt = 0; attempt < 18; attempt++)
            {
                float rx = (float)(rng.NextDouble() * (radius * 2.0) - radius);
                float ry = (float)(rng.NextDouble() * (radius * 1.5) - (radius * 0.75));
                Vector2 candidate = center + new Vector2(rx, ry);

                bool walkable = mapGenerator == null || mapGenerator.IsWorldPointWalkable(candidate);
                if (walkable && Physics2D.OverlapCircle(candidate, 0.42f) == null)
                {
                    return candidate;
                }
            }

            return mapGenerator != null ? mapGenerator.FindNearestWalkablePosition(center) : center;
        }

        private PMDPlayerController CreatePlayerEntity(PMDCharacterData data, Vector2 pos)
        {
            var go = new GameObject($"Player_{data.pokemonName}");
            go.transform.position = pos;

            go.AddComponent<SpriteRenderer>();
            var anim = go.AddComponent<PMDSpriteAnimator>();
            anim.CharacterData = data;
            anim.Play("Idle", true, true);

            go.AddComponent<PMDYSorter>();

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;

            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.32f;
            col.offset = new Vector2(0f, 0.1f);

            AttachShadow(go.transform, data.shadowSize);

            return go.AddComponent<PMDPlayerController>();
        }

        private PMDWildPokemonAI SpawnWildPokemon(PMDCharacterData data, Vector2 pos, string biomeName, bool aggressive, Transform parentContainer, int monLevel = 1, bool isBoss = false)
        {
            var go = new GameObject($"Pokemon_{data.pokemonName}_{biomeName}{(isBoss ? "_BOSS" : "")}");
            go.transform.SetParent(parentContainer != null ? parentContainer : transform, false);
            go.transform.position = pos;

            go.AddComponent<SpriteRenderer>();
            var anim = go.AddComponent<PMDSpriteAnimator>();
            anim.CharacterData = data;
            anim.Play("Idle", true, true);

            go.AddComponent<PMDYSorter>();

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;

            var col = go.AddComponent<CircleCollider2D>();
            col.radius = isBoss ? 0.48f : 0.32f;
            col.offset = new Vector2(0f, 0.1f);

            if (isBoss)
            {
                go.transform.localScale = Vector3.one * 1.4f;
            }

            AttachShadow(go.transform, isBoss ? 3 : data.shadowSize);

            var ai = go.AddComponent<PMDWildPokemonAI>();
            ai.InitializeWild(data, pos, biomeName, aggressive, monLevel, isBoss);
            return ai;
        }

        private void AttachShadow(Transform parent, int shadowSize)
        {
            var shadowGo = new GameObject("Shadow");
            shadowGo.transform.SetParent(parent, false);
            shadowGo.transform.localPosition = new Vector3(0f, -0.05f, 0f);

            float scale = shadowSize switch
            {
                0 => 0.65f,
                1 => 0.9f,
                2 => 1.25f,
                _ => 1.5f
            };
            shadowGo.transform.localScale = new Vector3(scale, scale * 0.55f, 1f);

            var sr = shadowGo.AddComponent<SpriteRenderer>();
            sr.sprite = _shadowSprite;
            sr.color = new Color(0f, 0f, 0f, 0.38f);
            sr.sortingOrder = -950;
        }

        private PMDCharacterData FindCharacter(string idPrefix)
        {
            for (int i = 0; i < _loadedCharacters.Count; i++)
            {
                if (_loadedCharacters[i].pokemonId.StartsWith(idPrefix, StringComparison.OrdinalIgnoreCase))
                    return _loadedCharacters[i];
            }
            return null;
        }

        private static Sprite CreateEllipseShadowSprite()
        {
            const int w = 32;
            const int h = 16;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            Vector2 center = new Vector2(w * 0.5f, h * 0.5f);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = (x - center.x) / (w * 0.45f);
                    float dy = (y - center.y) / (h * 0.45f);
                    float dist = dx * dx + dy * dy;
                    tex.SetPixel(x, y, dist <= 1f ? Color.white : Color.clear);
                }
            }
            tex.Apply(false);
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 24f);
        }
    }
}
