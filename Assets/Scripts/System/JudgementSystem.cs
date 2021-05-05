﻿using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using ArcCore.Data;
using ArcCore.MonoBehaviours;
using Unity.Rendering;
using ArcCore.Utility;
using ArcCore.Structs;
using Unity.Mathematics;
using ArcCore.MonoBehaviours.EntityCreation;
using ArcCore;
using ArcCore.Tags;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public class JudgementSystem : SystemBase
{
    public static JudgementSystem Instance { get; private set; }
    public EntityManager entityManager;
    public NativeArray<Rect2D> laneAABB2Ds;

    public bool IsReady => arcFingers.IsCreated;
    public EntityQuery tapQuery, arcQuery, arctapQuery, holdQuery;

    public const float arcLeniencyGeneral = 2f;
    public static readonly float2 arctapBoxExtents = new float2(4f, 1f); //DUMMY VALUES

    public NativeMatrIterator<ArcJudge> arcJudges;
    public NativeArray<ArcCompleteState> arcStates;
    public NativeArray<int> arcFingers;
    public NativeArray<AffArc> rawArcs;

    BeginSimulationEntityCommandBufferSystem beginSimulationEntityCommandBufferSystem;

    protected override void OnCreate()
    {
        Instance = this;
        var defaultWorld = World.DefaultGameObjectInjectionWorld;
        entityManager = defaultWorld.EntityManager;

        beginSimulationEntityCommandBufferSystem = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();

        laneAABB2Ds = new NativeArray<Rect2D>(
            new Rect2D[] {
                new Rect2D(new float2(ArccoreConvert.TrackToX(1), 0), new float2(Constants.LaneWidth, float.PositiveInfinity)),
                new Rect2D(new float2(ArccoreConvert.TrackToX(2), 0), new float2(Constants.LaneWidth, float.PositiveInfinity)),
                new Rect2D(new float2(ArccoreConvert.TrackToX(3), 0), new float2(Constants.LaneWidth, float.PositiveInfinity)),
                new Rect2D(new float2(ArccoreConvert.TrackToX(4), 0), new float2(Constants.LaneWidth, float.PositiveInfinity))
                    },
            Allocator.Persistent
            );

        holdQuery = GetEntityQuery(
                typeof(HoldFunnelPtr),
                typeof(ChartTime),
                typeof(Track),
                typeof(WithinJudgeRange),
                typeof(JudgeHoldPoint)
            );

        arctapQuery = GetEntityQuery(
                typeof(EntityReference),
                typeof(ChartTime),
                typeof(ChartPosition),
                typeof(WithinJudgeRange)
            );


        tapQuery = GetEntityQuery(
                typeof(EntityReference),
                typeof(ChartTime),
                typeof(Track),
                typeof(WithinJudgeRange)
            );

        arcQuery = GetEntityQuery(
                typeof(ArcFunnelPtr),
                typeof(LinearPosGroup),
                typeof(ColorID),
                typeof(StrictArcJudge),
                typeof(WithinJudgeRange)
            );
    }
    public void SetupColors()
    {
        arcFingers = new NativeArray<int>(utils.new_fill_aclen(-1), Allocator.Persistent);
        arcStates = new NativeArray<ArcCompleteState>(utils.new_fill_aclen(new ArcCompleteState(ArcState.Normal)), Allocator.Persistent);
    }
    protected override void OnDestroy()
    {
        laneAABB2Ds.Dispose();
    }
    protected override unsafe void OnUpdate()
    {
        //Only execute after full initialization
        if (!IsReady)
            return;

        //Get data from statics
        int currentTime = (int)(Conductor.Instance.receptorTime / 1000f);

        //Command buffering
        var commandBuffer = beginSimulationEntityCommandBufferSystem.CreateCommandBuffer();

        //Particles
        NativeList<SkyParticleAction>   skyParticleActions   = new NativeList<SkyParticleAction>  (Allocator.TempJob);
        NativeList<ComboParticleAction> comboParticleActions = new NativeList<ComboParticleAction>(Allocator.TempJob);
        NativeList<TrackParticleAction> trackParticleActions = new NativeList<TrackParticleAction>(Allocator.TempJob);

        //Score management data
        int maxPureCount = ScoreManager.Instance.maxPureCount,
            latePureCount = ScoreManager.Instance.latePureCount,
            earlyPureCount = ScoreManager.Instance.earlyPureCount,
            lateFarCount = ScoreManager.Instance.lateFarCount,
            earlyFarCount = ScoreManager.Instance.earlyFarCount,
            lostCount = ScoreManager.Instance.lostCount,
            combo = ScoreManager.Instance.currentCombo;

        void JudgeLost()
        {
            lostCount++;
            combo = 0;
        }
        void JudgeMaxPure()
        {
            maxPureCount++;
            combo++;
        }
        void Judge(int time)
        {
            int timeDiff = time - currentTime;
            if (timeDiff > Constants.FarWindow)
            {
                lostCount++;
                combo = 0;
            }
            else if (timeDiff > Constants.PureWindow)
            {
                earlyFarCount++;
                combo++;
            }
            else if (timeDiff > Constants.MaxPureWindow)
            {
                earlyPureCount++;
                combo++;
            }
            else if (timeDiff > -Constants.MaxPureWindow)
            {
                JudgeMaxPure();
            }
            else if (timeDiff > -Constants.PureWindow)
            {
                latePureCount++;
                combo++;
            }
            else
            {
                lateFarCount++;
                combo++;
            }
        }

        //Clean up arc fingers
        for (int c = 0; c < ArcEntityCreator.ColorCount; c++)
        {
            if (arcJudges.PeekAhead(c, 1).time > currentTime + Constants.FarWindow)
            {
                arcFingers[c] = -1;
                if (arcStates[c].state == ArcState.Red) 
                    arcStates[c] = new ArcCompleteState(arcStates[c], ArcState.Unheld);
            }
        }

        //Execute for each touch
        for (int i = 0; i < InputManager.MaxTouches; i++)
        {
            TouchPoint touch = InputManager.Get(i);
            bool tapped = false;

            //Track taps
            if (touch.TrackRangeValid) {

                //Hold notes
                Entities.WithAll<WithinJudgeRange>().ForEach(

                    (Entity entity, ref HoldLastJudge held, ref ChartHoldTime holdTime, ref HoldLastJudge lastJudge, in ChartTimeSpan span, in ChartPosition position)

                        =>

                    {
                        //Invalidate holds if they require a tap and this touch has been parsed as a tap already
                        if (!held.value && tapped) return;

                        //Invalidate holds out of time range
                        if (!holdTime.CheckStart(Constants.FarWindow)) return;

                        //Disable judgenotes
                        void Disable()
                        {
                            commandBuffer.RemoveComponent<WithinJudgeRange>(entity);
                            commandBuffer.AddComponent<PastJudgeRange>(entity);
                        }

                        //Increment or kill holds out of time for judging
                        if (holdTime.CheckOutOfRange(currentTime))
                        {
                            JudgeLost();
                            lastJudge.value = false;

                            if (!holdTime.Increment(span)) 
                                Disable();
                        }

                        //Invalidate holds not in range; should also rule out all invalid data, i.e. positions with a lane of -1
                        if (!touch.trackRange.Contains(position.lane)) return;

                        //Holds not requiring a tap
                        if(held.value)
                        {
                            //If valid:
                            if (touch.status != TouchPoint.Status.RELEASED)
                            {
                                JudgeMaxPure();
                                lastJudge.value = true;

                                if (!holdTime.Increment(span)) 
                                    Disable();
                            }
                            //If invalid:
                            else
                            {
                                held.value = false;
                            }
                        }
                        //Holds requiring a tap
                        else if(touch.status == TouchPoint.Status.TAPPED)
                        {
                            JudgeMaxPure();
                            lastJudge.value = true;

                            if (!holdTime.Increment(span)) 
                                Disable();

                            tapped = true;
                        }
                    }

                );

                if (!tapped) {
                    //Tap notes; no EntityReference, those only exist on arctaps
                    Entities.WithAll<WithinJudgeRange>().WithNone<EntityReference>().ForEach(

                        (Entity entity, in ChartTime time, in ChartPosition position)

                            =>

                        {
                            //Invalidate if already tapped
                            if (tapped) return;

                            //Increment or kill holds out of time for judging
                            if (time.CheckOutOfRange(currentTime))
                            {
                                JudgeLost();
                                entityManager.DestroyEntity(entity);
                            }

                            //Invalidate if not in range of a tap; should also rule out all invalid data, i.e. positions with a lane of -1
                            if (!touch.trackRange.Contains(position.lane)) return;

                            //Register tap lul
                            Judge(time.value);
                            tapped = true;

                            //Destroy tap
                            entityManager.DestroyEntity(entity);
                        }

                    );
                }

            }

            //Refuse to judge arctaps if above checks have found a tap already
            if (!tapped)
            {
                //Tap notes; no EntityReference, those only exist on arctaps
                Entities.WithAll<WithinJudgeRange>().ForEach(

                    ((Entity entity, in ChartTime time, in ChartPosition position, in EntityReference enRef)

                        =>

                    {
                        //Invalidate if already tapped
                        if (tapped) return;

                        //Increment or kill holds out of time for judging
                        if (time.CheckOutOfRange(currentTime))
                        {
                            JudgeLost();
                            entityManager.DestroyEntity(entity);
                            entityManager.DestroyEntity(enRef.Value);
                        }

                        //Invalidate if not in range of a tap; should also rule out all invalid data, i.e. positions with a lane of -1

/* Unmerged change from project 'Assembly-CSharp.Player'
Before:
                        if (!touch.inputPlane.CollidesWith(new AABB2D(position.xy - arctapBoxExtents, position.xy + arctapBoxExtents))) 
After:
                        if (!touch.inputPlane.CollidesWith(new ArcCore.Utility.AABB2D(position.xy - arctapBoxExtents, position.xy + arctapBoxExtents))) 
*/
                        if (!touch.inputPlane.CollidesWith((Rect2D)new Rect2D(position.xy - arctapBoxExtents, position.xy + arctapBoxExtents))) 
                            return;

                        //Register tap lul
                        Judge(time.value);
                        tapped = true;

                        //Destroy tap
                        entityManager.DestroyEntity(entity);
                    })

                );
            }

            // Handle all arcs //
            Job.WithCode(

                delegate ()
                {
                    for (int c = 0; c < arcJudges.RowCount; c++)
                    {
                        if()
                    }
                }

            ).Run();

        }

        // Repopulate managed data
        ScoreManager.Instance.currentCombo = combo;
        ScoreManager.Instance.maxPureCount = maxPureCount;
        ScoreManager.Instance.latePureCount = latePureCount;
        ScoreManager.Instance.lateFarCount = lateFarCount;
        ScoreManager.Instance.earlyPureCount = earlyPureCount;
        ScoreManager.Instance.earlyFarCount = earlyFarCount;
        ScoreManager.Instance.lostCount = lostCount;


    }
}
