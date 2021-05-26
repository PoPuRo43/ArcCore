﻿using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using ArcCore.Gameplay.Utility;
using ArcCore.Gameplay.Components;
using ArcCore.Parsing.Aff;
using ArcCore.Gameplay.Components.Chunk;
using ArcCore.Utilities.Extensions;

namespace ArcCore.Gameplay.Behaviours.EntityCreation
{
    public class HoldEntityCreator : ECSMonoBehaviour
    {
        public static HoldEntityCreator Instance { get; private set; }
        [SerializeField] private GameObject holdNotePrefab;
        private Entity holdNoteEntityPrefab;
        private void Awake()
        {
            Instance = this;
            holdNoteEntityPrefab = GameObjectConversionSettings.ConvertToNote(holdNotePrefab, EntityManager);
        }

        public unsafe void CreateEntities(List<AffHold> affHoldList)
        {
            affHoldList.Sort((item1, item2) => { return item1.timing.CompareTo(item2.timing); });

            foreach (AffHold hold in affHoldList)
            {
                //Main entity
                Entity holdEntity = EntityManager.Instantiate(holdNoteEntityPrefab);

                float x = Conversion.TrackToX(hold.track);
                const float y = 0;
                const float z = 0;

                const float scalex = 1;
                const float scaley = 1;

                float endFloorPosition = Conductor.Instance.GetFloorPositionFromTiming(hold.endTiming, hold.timingGroup);
                float startFloorPosition = Conductor.Instance.GetFloorPositionFromTiming(hold.timing, hold.timingGroup);
                float scalez = - endFloorPosition + startFloorPosition;

                EntityManager.SetComponentData<Translation>(holdEntity, new Translation(){
                    Value = new float3(x, y, z)
                });
                EntityManager.AddComponentData<NonUniformScale>(holdEntity, new NonUniformScale(){
                    Value = new float3(scalex, scaley, scalez)
                });

                EntityManager.SetComponentData<FloorPosition>(holdEntity, new FloorPosition(startFloorPosition));
                EntityManager.SetComponentData<TimingGroup>(holdEntity, new TimingGroup(hold.timingGroup));
                EntityManager.SetComponentData<ChartTime >(holdEntity, new ChartTime{value = hold.timing});
                /*entityManager.SetComponentData<ShaderCutoff>(holdEntity, new ShaderCutoff()
                {
                    Value = 1f
                });*/

                EntityManager.SetComponentData(holdEntity, new ChartLane(hold.track));

                //Appear and disappear time
                int t1 = Conductor.Instance.GetFirstTimingFromFloorPosition(startFloorPosition + Constants.RenderFloorPositionRange, 0);
                int t2 = Conductor.Instance.GetFirstTimingFromFloorPosition(endFloorPosition - Constants.RenderFloorPositionRange, 0);
                int appearTime = (t1 < t2) ? t1 : t2;
                int disappearTime = (t1 < t2) ? t2 : t1;

                EntityManager.SetComponentData<AppearTime>(holdEntity, new AppearTime(appearTime));
                EntityManager.SetComponentData<DisappearTime>(holdEntity, new DisappearTime(disappearTime));

                //Judge entities
                float startBpm = Conductor.Instance.GetTimingEventFromTiming(hold.timing, hold.timingGroup).bpm;
                EntityManager.SetComponentData(holdEntity, ChartIncrTime.FromBpm(hold.timing, hold.endTiming, startBpm, out int comboCount));

                //Add combo
                ScoreManager.Instance.maxCombo += comboCount;
            }
        }
    }

}