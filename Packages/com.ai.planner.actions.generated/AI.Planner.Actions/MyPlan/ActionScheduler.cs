using System;
using System.Collections.Generic;
using AI.Planner.Domains;
using Unity.AI.Planner;
using Unity.AI.Planner.DomainLanguage.TraitBased;
using Unity.AI.Planner.Jobs;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace AI.Planner.Actions.MyPlan
{
    public struct ActionScheduler :
        ITraitBasedActionScheduler<TraitBasedObject, StateEntityKey, StateData, StateDataContext, StateManager, ActionKey>
    {
        public static readonly Guid MoveToItemGuid = Guid.NewGuid();
        public static readonly Guid TakeAItemGuid = Guid.NewGuid();
        public static readonly Guid MoveToGoalGuid = Guid.NewGuid();
        public static readonly Guid MoveToSwitchGuid = Guid.NewGuid();
        public static readonly Guid TurnOnGateGuid = Guid.NewGuid();

        // Input
        public NativeList<StateEntityKey> UnexpandedStates { get; set; }
        public StateManager StateManager { get; set; }

        // Output
        NativeQueue<StateTransitionInfoPair<StateEntityKey, ActionKey, StateTransitionInfo>> IActionScheduler<StateEntityKey, StateData, StateDataContext, StateManager, ActionKey>.CreatedStateInfo
        {
            set => m_CreatedStateInfo = value;
        }

        NativeQueue<StateTransitionInfoPair<StateEntityKey, ActionKey, StateTransitionInfo>> m_CreatedStateInfo;

        struct PlaybackECB : IJob
        {
            public ExclusiveEntityTransaction ExclusiveEntityTransaction;

            [ReadOnly]
            public NativeList<StateEntityKey> UnexpandedStates;
            public NativeQueue<StateTransitionInfoPair<StateEntityKey, ActionKey, StateTransitionInfo>> CreatedStateInfo;
            public EntityCommandBuffer MoveToItemECB;
            public EntityCommandBuffer TakeAItemECB;
            public EntityCommandBuffer MoveToGoalECB;
            public EntityCommandBuffer MoveToSwitchECB;
            public EntityCommandBuffer TurnOnGateECB;

            public void Execute()
            {
                // Playback entity changes and output state transition info
                var entityManager = ExclusiveEntityTransaction;

                MoveToItemECB.Playback(entityManager);
                for (int i = 0; i < UnexpandedStates.Length; i++)
                {
                    var stateEntity = UnexpandedStates[i].Entity;
                    var MoveToItemRefs = entityManager.GetBuffer<MoveToItemFixupReference>(stateEntity);
                    for (int j = 0; j < MoveToItemRefs.Length; j++)
                        CreatedStateInfo.Enqueue(MoveToItemRefs[j].TransitionInfo);
                    entityManager.RemoveComponent(stateEntity, typeof(MoveToItemFixupReference));
                }

                TakeAItemECB.Playback(entityManager);
                for (int i = 0; i < UnexpandedStates.Length; i++)
                {
                    var stateEntity = UnexpandedStates[i].Entity;
                    var TakeAItemRefs = entityManager.GetBuffer<TakeAItemFixupReference>(stateEntity);
                    for (int j = 0; j < TakeAItemRefs.Length; j++)
                        CreatedStateInfo.Enqueue(TakeAItemRefs[j].TransitionInfo);
                    entityManager.RemoveComponent(stateEntity, typeof(TakeAItemFixupReference));
                }

                MoveToGoalECB.Playback(entityManager);
                for (int i = 0; i < UnexpandedStates.Length; i++)
                {
                    var stateEntity = UnexpandedStates[i].Entity;
                    var MoveToGoalRefs = entityManager.GetBuffer<MoveToGoalFixupReference>(stateEntity);
                    for (int j = 0; j < MoveToGoalRefs.Length; j++)
                        CreatedStateInfo.Enqueue(MoveToGoalRefs[j].TransitionInfo);
                    entityManager.RemoveComponent(stateEntity, typeof(MoveToGoalFixupReference));
                }

                MoveToSwitchECB.Playback(entityManager);
                for (int i = 0; i < UnexpandedStates.Length; i++)
                {
                    var stateEntity = UnexpandedStates[i].Entity;
                    var MoveToSwitchRefs = entityManager.GetBuffer<MoveToSwitchFixupReference>(stateEntity);
                    for (int j = 0; j < MoveToSwitchRefs.Length; j++)
                        CreatedStateInfo.Enqueue(MoveToSwitchRefs[j].TransitionInfo);
                    entityManager.RemoveComponent(stateEntity, typeof(MoveToSwitchFixupReference));
                }

                TurnOnGateECB.Playback(entityManager);
                for (int i = 0; i < UnexpandedStates.Length; i++)
                {
                    var stateEntity = UnexpandedStates[i].Entity;
                    var TurnOnGateRefs = entityManager.GetBuffer<TurnOnGateFixupReference>(stateEntity);
                    for (int j = 0; j < TurnOnGateRefs.Length; j++)
                        CreatedStateInfo.Enqueue(TurnOnGateRefs[j].TransitionInfo);
                    entityManager.RemoveComponent(stateEntity, typeof(TurnOnGateFixupReference));
                }
            }
        }

        public JobHandle Schedule(JobHandle inputDeps)
        {
            var entityManager = StateManager.EntityManager;
            var MoveToItemDataContext = StateManager.GetStateDataContext();
            var MoveToItemECB = StateManager.GetEntityCommandBuffer();
            MoveToItemDataContext.EntityCommandBuffer = MoveToItemECB.ToConcurrent();
            var TakeAItemDataContext = StateManager.GetStateDataContext();
            var TakeAItemECB = StateManager.GetEntityCommandBuffer();
            TakeAItemDataContext.EntityCommandBuffer = TakeAItemECB.ToConcurrent();
            var MoveToGoalDataContext = StateManager.GetStateDataContext();
            var MoveToGoalECB = StateManager.GetEntityCommandBuffer();
            MoveToGoalDataContext.EntityCommandBuffer = MoveToGoalECB.ToConcurrent();
            var MoveToSwitchDataContext = StateManager.GetStateDataContext();
            var MoveToSwitchECB = StateManager.GetEntityCommandBuffer();
            MoveToSwitchDataContext.EntityCommandBuffer = MoveToSwitchECB.ToConcurrent();
            var TurnOnGateDataContext = StateManager.GetStateDataContext();
            var TurnOnGateECB = StateManager.GetEntityCommandBuffer();
            TurnOnGateDataContext.EntityCommandBuffer = TurnOnGateECB.ToConcurrent();

            var allActionJobs = new NativeArray<JobHandle>(6, Allocator.TempJob)
            {
                [0] = new MoveToItem(MoveToItemGuid, UnexpandedStates, MoveToItemDataContext).Schedule(UnexpandedStates, 0, inputDeps),
                [1] = new TakeAItem(TakeAItemGuid, UnexpandedStates, TakeAItemDataContext).Schedule(UnexpandedStates, 0, inputDeps),
                [2] = new MoveToGoal(MoveToGoalGuid, UnexpandedStates, MoveToGoalDataContext).Schedule(UnexpandedStates, 0, inputDeps),
                [3] = new MoveToSwitch(MoveToSwitchGuid, UnexpandedStates, MoveToSwitchDataContext).Schedule(UnexpandedStates, 0, inputDeps),
                [4] = new TurnOnGate(TurnOnGateGuid, UnexpandedStates, TurnOnGateDataContext).Schedule(UnexpandedStates, 0, inputDeps),
                [5] = entityManager.ExclusiveEntityTransactionDependency
            };

            var allActionJobsHandle = JobHandle.CombineDependencies(allActionJobs);
            allActionJobs.Dispose();

            // Playback entity changes and output state transition info
            var playbackJob = new PlaybackECB()
            {
                ExclusiveEntityTransaction = StateManager.ExclusiveEntityTransaction,
                UnexpandedStates = UnexpandedStates,
                CreatedStateInfo = m_CreatedStateInfo,
                MoveToItemECB = MoveToItemECB,
                TakeAItemECB = TakeAItemECB,
                MoveToGoalECB = MoveToGoalECB,
                MoveToSwitchECB = MoveToSwitchECB,
                TurnOnGateECB = TurnOnGateECB,
            };

            var playbackJobHandle = playbackJob.Schedule(allActionJobsHandle);
            entityManager.ExclusiveEntityTransactionDependency = playbackJobHandle;

            return playbackJobHandle;
        }
    }
}
