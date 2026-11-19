using System.Collections.Generic;
using UnityEngine;

namespace POKE.PMDCollab.OpenWorld
{
    /// <summary>
    /// Room / Region Gateway Trigger: When the player walks into a directional gate (North/South/East/West),
    /// a procedural dungeon staircase/portal, or an indoor house doorway, transitions the entire scene
    /// into the target Region ("sang vùng đất mới như sang 1 phòng mới").
    /// </summary>
    public class PMDRoomGatewayTrigger : MonoBehaviour
    {
        public int targetZoneIndex;
        public RoomEntranceSide arrivalEntranceSide = RoomEntranceSide.Center;
        public bool randomizeSeedOnEnter;
        public string gatewayLabel;
        public Vector2 returnOutdoorPos;

        private static float _lastTransitionTime = -10f;
        public static readonly List<PMDRoomGatewayTrigger> ActiveGateways = new List<PMDRoomGatewayTrigger>();

        private void OnEnable()
        {
            if (!ActiveGateways.Contains(this))
                ActiveGateways.Add(this);
        }

        private void OnDisable()
        {
            ActiveGateways.Remove(this);
        }

        public static void NotifyRoomJustLoaded()
        {
            _lastTransitionTime = Time.time;
        }

        private void Update()
        {
            if (!Application.isPlaying)
                return;

            if (Time.time - _lastTransitionTime < 1.35f)
                return;

            var player = PMDPlayerController.Instance;
            if (player == null)
                return;

            if (Vector2.Distance(transform.position, player.transform.position) <= 1.45f)
            {
                _lastTransitionTime = Time.time;

                var mapGen = FindFirstObjectByType<PMDOpenWorldMapGenerator>();
                if (mapGen != null)
                {
                    if (returnOutdoorPos != Vector2.zero)
                    {
                        mapGen.SetPendingOutdoorReturnPosition(returnOutdoorPos);
                    }

                    mapGen.TransitionToRegion(targetZoneIndex, arrivalEntranceSide, randomizeSeedOnEnter);
                }
            }
        }
    }
}

