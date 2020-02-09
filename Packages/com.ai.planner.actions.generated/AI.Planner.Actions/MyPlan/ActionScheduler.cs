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

            var allActionJobs = new NativeArray<JobHandle>(4, Allocator.TempJob)
            {
                [0] = new MoveToItem(MoveToItemGuid, UnexpandedStates, MoveToItemDataContext).Schedule(UnexpandedStates, 0, inputDeps),
                [1] = new TakeAItem(TakeAItemGuid, UnexpandedStates, TakeAItemDataContext).Schedule(UnexpandedStates, 0, inputDeps),
                [2] = new MoveToGoal(MoveToGoalGuid, UnexpandedStates, MoveToGoalDataContext).Schedule(UnexpandedStates, 0, inputDeps),
                [3] = entityManager.ExclusiveEntityTransactionDependency
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
            };

            var playbackJobHandle = playbackJob.Schedule(allActionJobsHandle);
            entityManager.ExclusiveEntityTransactionDependency = playbackJobHandle;

            return playbackJobHandle;
        }
    }
}
