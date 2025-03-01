using System.Collections.Generic;
using UnityEngine;
using ArcCore.Parsing.Data;
using Unity.Mathematics;
using ArcCore.Math;
using ArcCore.Utilities.Extensions;
using ArcCore.Parsing;

namespace ArcCore.Gameplay.Behaviours
{
    [RequireComponent(typeof(Camera))]
    public class GameplayCamera : MonoBehaviour
    {
        /// <summary>
        /// All movements attached to this camera, assumed to be sorted by time.
        /// </summary>
        private CameraEvent[] cameraMovements;
        private int firstInactiveIndex;
        private List<int> activeIndices;

        /// <summary>
        /// Is the camera currently reset?
        /// </summary>
        private bool isReset;

        private int[] resetTimings;
        private int currentResetTiming;

        /// <summary>
        /// Current tilt value, affected by active arcs' x positions on the scene
        /// </summary>
        private float currentTilt;
        private float accumulativeArcX;
        public float AccumulativeArcX
        {
            set => accumulativeArcX = value;
        }

        private PosRot accumulate;

        /// <summary>
        /// The internal camera.
        /// </summary>
        [HideInInspector] public Camera innerCam;

        public float AspectRatio
            => (float)innerCam.pixelWidth / innerCam.pixelHeight;

        public const float Ratio4By3 = 4f / 3f;
        public const float Ratio16By9 = 16f / 9f;

        public float AspectRatioLerp
            => 1 - math.clamp((AspectRatio - Ratio4By3) / (Ratio16By9 - Ratio4By3), 0, 1);

        public float3 ResetPosition
            => math.lerp(
                new float3(0, 9, 9),
                new float3(0, 9, 8),
               AspectRatioLerp
            );
        public float3 ResetRotation
            => math.lerp(
                new float3(26.5f, 180, 0),
                new float3(27.4f, 180, 0),
                AspectRatioLerp
            );

        public PosRot ResetPosRot
            => new PosRot(ResetPosition, ResetRotation);

        public float FieldOfView
            => math.lerp(50, 65, AspectRatioLerp);

        public void SetupCamera(IChartParser parser)
        {
            cameraMovements = parser.Cameras.ToArray();
            resetTimings = parser.CameraResets.ToArray();
        }

        public void Reset()
        {
            accumulate = ResetPosRot;
            isReset = true;
        }
        public void Start()
        {
            innerCam = GetComponent<Camera>();
            innerCam.fieldOfView = FieldOfView;
            innerCam.nearClipPlane = 1;
            innerCam.farClipPlane = 5000;

            Reset();
            transform.SetPositionAndRotation(accumulate);

            activeIndices = new List<int>();
        }

        public void Update()
        {
            if (!PlayManager.IsUpdating) return;

            if (cameraMovements.Length > 0) UpdateMove();
            transform.SetPositionAndRotation(accumulate);

            if (isReset) UpdateTilt();
        }

        public void UpdateMove()
        {
            int time = PlayManager.ReceptorTime;

            //handle resets.
            bool needsReset = false;
            while (currentResetTiming < resetTimings.Length && resetTimings[currentResetTiming] < time)
            {
                needsReset = true;
                currentResetTiming++;
            }
            if (needsReset)
            {
                Reset();
            }

            //update active indices
            while (firstInactiveIndex < cameraMovements.Length && time > cameraMovements[firstInactiveIndex].Timing)
            {
                activeIndices.Add(firstInactiveIndex);

                //move on to the next movement.
                firstInactiveIndex++;
            }

            //handle active movements.
            var toRemove = new List<int>();
            foreach (int i in activeIndices)
            {
                //check if index has ended.
                if (cameraMovements[i].EndTiming < time)
                {
                    //add remaining if reset not encountered (to prevent bugginess)
                    if (!needsReset)
                    {
                        accumulate += cameraMovements[i].Remaining;
                    }

                    //mark for removal
                    toRemove.Add(i);
                } 
                else 
                {
                    //update all internal variables
                    cameraMovements[i].Update(time);

                    //add delta to accumulate
                    accumulate += cameraMovements[i].delta;
                }

                isReset = false;
            }

            //remove dead indices.
            foreach (int i in toRemove)
            {
                activeIndices.Remove(i);
            }
        }
        public void UpdateTilt()
        {
            //Taken from arcade. Might need tweaking
            float pos = Mathf.Clamp(-accumulativeArcX / 4.25f, -1, 1) * 0.05f;
            float delta = pos - currentTilt;
            float speed = PlayManager.IsUpdatingAndActive ? (accumulativeArcX == 0 ? 4f : 8f) : 0;
            currentTilt = currentTilt + speed * delta * Time.deltaTime;
            transform.LookAt(new Vector3(0, -5.5f, -20), new Vector3(currentTilt, 1 - currentTilt, 0)); 
        }
    }
}