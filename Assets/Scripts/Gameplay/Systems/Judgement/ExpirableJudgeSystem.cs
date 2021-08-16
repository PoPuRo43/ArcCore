﻿using ArcCore;
using ArcCore.Gameplay.Behaviours;
using ArcCore.Gameplay.Components;
using ArcCore.Gameplay.Components.Tags;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using ArcCore.Gameplay.Data;
using Unity.Mathematics;
using ArcCore.Gameplay.Utility;
using ArcCore.Utilities.Extensions;

namespace ArcCore.Gameplay.Systems
{

    [UpdateInGroup(typeof(JudgementSystemGroup))]
    public class ExpirableJudgeSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            if (!PlayManager.IsUpdatingAndActive) return;

            var tracker = PlayManager.ScoreHandler.tracker;
            int currentTime = PlayManager.ReceptorTime;

            var commandBuffer = PlayManager.CommandBuffer;
            var particleBuffer = PlayManager.ParticleBuffer;

            //- TAPS -//
            Entities.WithAll<WithinJudgeRange>().WithNone<ChartIncrTime, EntityReference>().ForEach(
                (Entity en, in ChartTime chartTime, in ChartLane cl) =>
                {
                    if (currentTime - Constants.FarWindow > chartTime.value)
                    {
                        commandBuffer.AddComponent<PastJudgeRange>(en);
                        commandBuffer.DisableEntity(en);
                        tracker.AddJudge(JudgeType.Lost);

                        particleBuffer.PlayTapParticle(
                            new float2(Conversion.TrackToX(cl.lane), 0.5f),
                            ParticlePool.JudgeType.Lost,
                            ParticlePool.JudgeDetail.None,
                            1f
                        );
                    }
                }
            ).Run();

            //- ARCTAPS -//
            Entities.WithAll<WithinJudgeRange>().WithNone<ChartIncrTime>().ForEach(
                (Entity en, in ChartTime chartTime, in EntityReference enRef, in ChartPosition cp) =>
                {
                    if (currentTime - Constants.FarWindow > chartTime.value)
                    {
                        commandBuffer.AddComponent<PastJudgeRange>(en);
                        commandBuffer.DisableEntity(enRef.value);
                        commandBuffer.DisableEntity(en);
                        tracker.AddJudge(JudgeType.Lost);

                        particleBuffer.PlayTapParticle(
                            cp.xy,
                            ParticlePool.JudgeType.Lost,
                            ParticlePool.JudgeDetail.None,
                            1f 
                        );
                    }
                }
            ).Run();

            //- HOLDS -//
            Entities.WithAll<WithinJudgeRange, ChartTime>().ForEach(
                (Entity en, ref ChartIncrTime chartIncrTime, in ChartLane cl) =>
                {
                    if (currentTime - Constants.FarWindow > chartIncrTime.time)
                    {
                        chartIncrTime.UpdateJudgePointCache(currentTime, out int count);
                        tracker.AddJudge(JudgeType.Lost, count);
                        particleBuffer.PlayHoldParticle(cl.lane - 1, false);
                    }
                }
            ).Run();

            Entities.WithAll<WithinJudgeRange, ChartTime>().ForEach(
                (Entity en, in ChartIncrTime chartIncrTime, in ChartLane cl) =>
                {
                    if (currentTime - Constants.FarWindow > chartIncrTime.endTime)
                    {
                        commandBuffer.AddComponent<PastJudgeRange>(en);
                        commandBuffer.DisableEntity(en);
                        particleBuffer.DisableLaneParticle(cl.lane - 1);
                    }
                }
            ).Run();

            //- ARCS -//
            //...

            /*
            //- DESTROY ON TIMING -//
            //WARNING: TEMPORARY SOLUTION
            Entities.WithAll<DestroyOnTiming>().ForEach(
                (Entity en, in ChartTime charttime) =>
                {
                    if (currentTime >= charttime.value)
                    {
                        commandBuffer.DisableEntity(en);
                        commandBuffer.AddComponent<PastJudgeRange>(en);
                    }
                }
            ).Run();*/

            PlayManager.ScoreHandler.tracker = tracker;
        }
    }
}