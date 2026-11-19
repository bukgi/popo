using POKE.PMDCollab.OpenWorld;
using UnityEditor;
using UnityEngine;

namespace POKE.PMDCollab.Editor
{
    /// <summary>
    /// Automatically populates the active Scene with all 16 GitHub PMDCollab Maps, Objects, Items & Pokémon
    /// on Editor load / script reload so the user sees the real GitHub world immediately.
    /// </summary>
    [InitializeOnLoad]
    public static class PMDOpenWorldEditorAutoScene
    {
        private static bool _rebuiltThisSession;

        static PMDOpenWorldEditorAutoScene()
        {
            EditorApplication.delayCall += EnsureScenePopulated;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode || state == PlayModeStateChange.EnteredEditMode)
            {
                _rebuiltThisSession = false;
                EditorApplication.delayCall += EnsureScenePopulated;
            }
        }

        private static void EnsureScenePopulated()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += EnsureScenePopulated;
                return;
            }

            var mapGen = Object.FindFirstObjectByType<PMDOpenWorldMapGenerator>();
            bool needsRebuild = !_rebuiltThisSession ||
                                mapGen == null ||
                                !mapGen.HasValidSprites() ||
                                Object.FindFirstObjectByType<PMDPlayerController>() == null;

            if (needsRebuild)
            {
                _rebuiltThisSession = true;
                PMDOpenWorldAutoBootstrap.EnsureOpenWorldOnPlay();

                if (!Application.isPlaying && SceneView.lastActiveSceneView != null)
                {
                    SceneView.lastActiveSceneView.LookAt(Vector3.zero);
                    SceneView.lastActiveSceneView.Repaint();
                }
            }
        }
    }
}
