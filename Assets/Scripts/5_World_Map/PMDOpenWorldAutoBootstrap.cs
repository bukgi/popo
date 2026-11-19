using UnityEngine;
using UnityEngine.SceneManagement;

namespace POKE.PMDCollab.OpenWorld
{
    /// <summary>
    /// Automatically initializes the PMDCollab Open World when entering Play Mode or Edit Mode,
    /// cleaning up any old or missing-script GameObjects first so there are never duplicate maps.
    /// </summary>
    public static class PMDOpenWorldAutoBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void EnsureOpenWorldOnPlay()
        {
            CleanupLegacySceneObjects();

            if (Application.isPlaying)
            {
                Time.timeScale = 1f;
                PMDAtmosphere.EnsureExists();

                var gs = Object.FindFirstObjectByType<PMDGameState>();
                if (gs == null)
                {
                    var gsGo = new GameObject("0_PMD_GameState");
                    gs = gsGo.AddComponent<PMDGameState>();
                }
                gs.IsMainMenuOpen = false;
                gs.IsPaused = false;
                Time.timeScale = 1f;

                var audio = Object.FindFirstObjectByType<PMDAudio>();
                if (audio == null)
                {
                    var audioGo = new GameObject("0_PMD_Audio");
                    audioGo.AddComponent<PMDAudio>();
                }
            }

            var mapGen = Object.FindFirstObjectByType<PMDOpenWorldMapGenerator>();
            if (mapGen == null)
            {
                var mapGo = new GameObject("1_PMD_OpenWorld_Map");
                mapGen = mapGo.AddComponent<PMDOpenWorldMapGenerator>();
            }
            else
            {
                mapGen.gameObject.name = "1_PMD_OpenWorld_Map";
            }
            mapGen.GenerateWorld();

            var manager = Object.FindFirstObjectByType<PMDOpenWorldManager>();
            if (manager == null)
            {
                var mgrGo = new GameObject("2_PMD_OpenWorld_Manager");
                manager = mgrGo.AddComponent<PMDOpenWorldManager>();
            }
            else
            {
                manager.gameObject.name = "2_PMD_OpenWorld_Manager";
            }
            manager.BootstrapWorld();

            if (Object.FindFirstObjectByType<PMDOpenWorldHUD>() == null && manager != null)
            {
                manager.gameObject.AddComponent<PMDOpenWorldHUD>();
            }

            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.orthographic = true;
                mainCam.orthographicSize = 7.5f;
                mainCam.backgroundColor = new Color(0.11f, 0.29f, 0.16f, 1f);

                var follow = mainCam.GetComponent<PMDCameraFollow>();
                if (follow == null)
                    follow = mainCam.gameObject.AddComponent<PMDCameraFollow>();

                if (manager != null && manager.Player != null)
                {
                    follow.Target = manager.Player.transform;
                    mainCam.transform.position = new Vector3(
                        manager.Player.transform.position.x,
                        manager.Player.transform.position.y,
                        -10f);
                }
            }
        }

        private static void CleanupLegacySceneObjects()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid())
                return;

            GameObject[] roots = activeScene.GetRootGameObjects();
            var keepMapGen = Object.FindFirstObjectByType<PMDOpenWorldMapGenerator>();
            var keepMgr = Object.FindFirstObjectByType<PMDOpenWorldManager>();
            var keepPlayer = Object.FindFirstObjectByType<PMDPlayerController>();
            var keepGs = Object.FindFirstObjectByType<PMDGameState>();
            var keepAudio = Object.FindFirstObjectByType<PMDAudio>();
            var keepAtmosphere = Object.FindFirstObjectByType<PMDAtmosphere>();

            for (int i = roots.Length - 1; i >= 0; i--)
            {
                GameObject go = roots[i];
                if (go == null)
                    continue;

                if (go.GetComponent<Camera>() != null || go.name.Contains("Light"))
                    continue;

                if ((keepMapGen != null && go == keepMapGen.gameObject) ||
                    (keepMgr != null && go == keepMgr.gameObject) ||
                    (keepPlayer != null && go == keepPlayer.gameObject))
                {
                    continue;
                }

                if (Application.isPlaying &&
                    ((keepGs != null && go == keepGs.gameObject) ||
                     (keepAudio != null && go == keepAudio.gameObject) ||
                     (keepAtmosphere != null && go == keepAtmosphere.gameObject)))
                {
                    continue;
                }

                // Destroy any leftover or missing-script root objects from older sessions
                Object.DestroyImmediate(go);
            }
        }
    }
}
