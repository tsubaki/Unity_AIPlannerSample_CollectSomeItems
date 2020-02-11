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
        public static readonly Guid TakeAItemGuid = Guid.NewGuid();
        public static readonly Guid MoveToGuid = Guid.NewGuid();

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
            public EntityCommandBuffer TakeAItemECB;
            public EntityCommandBuffer MoveToECB;

            public void Execute()
            {
                // Playback entity changes and output state transition info
                var entityManager = ExclusiveEntityTransaction;

                TakeAItemECB.Playback(entityManager);
                for (int i = 0; i < UnexpandedStates.Length; i++)
                {
                    var stateEntity = UnexpandedStates[i].Entity;
                    var TakeAItemRefs = entityManager.GetBuffer<TakeAItemFixupReference>(stateEntity);
                    for (int j = 0; j < TakeAItemRefs.Length; j++)
                        CreatedStateInfo.Enqueue(TakeAItemRefs[j].TransitionInfo);
                    entityManager.RemoveComponent(stateEntity, typeof(TakeAItemFixupReference));
                }

                MoveToECB.Playback(entityManager);
                for (int i = 0; i < UnexpandedStates.Length; i++)
                {
                    var stateEntity = UnexpandedStates[i].Entity;
                    var MoveToRefs = entityManager.GetBuffer<MoveToFixupReference>(stateEntity);
                    for (int j = 0; j < MoveToRefs.Length; j++)
                        CreatedStateInfo.Enqueue(MoveToRefs[j].TransitionInfo);
                    entityManager.RemoveComponent(stateEntity, typeof(MoveToFixupReference));
                }
            }
        }

        public JobHandle Schedule(JobHandle inputDeps)
        {
            var entityManager = StateManager.EntityManager;
            var TakeAItemDataContext = StateManager.GetStateDataContext();
            var TakeAItemECB = StateManager.GetEntityCommandBuffer();
            TakeAItemDataContext.EntityCommandBuffer = TakeAItemECB.ToConcurrent();
            var MoveToDataContext = StateManager.GetStateDataContext();
            var MoveToECB = StateManager.GetEntityCommandBuffer();
            MoveToDataContext.EntityCommandBuffer = MoveToECB.ToConcurrent();

            var allActionJobs = new NativeArray<JobHandle>(3, Allocator.TempJob)
            {
                [0] = new TakeAItem(TakeAItemGuid, UnexpandedStates, TakeAItemDataContext).Schedule(UnexpandedStates, 0, inputDeps),
                [1] = new MoveTo(MoveToGuid, UnexpandedStates, MoveToDataContext).Schedule(UnexpandedStates, 0, inputDeps),
                [2] = entityManager.ExclusiveEntityTransactionDependency
            };

            var allActionJobsHandle = JobHandle.CombineDependencies(allActionJobs);
            allActionJobs.Dispose();

            // Playback entity changes and output state transition info
            var playbackJob = new PlaybackECB()
            {
                ExclusiveEntityTransaction = StateManager.ExclusiveEntityTransaction,
                UnexpandedStates = UnexpandedStates,
                CreatedStateInfo = m_CreatedStateInfo,
                TakeAItemECB = TakeAItemECB,
                MoveToECB = MoveToECB,
            };

            var playbackJobHandle = playbackJob.Schedule(allActionJobsHandle);
            entityManager.ExclusiveEntityTransactionDependency = playbackJobHandle;

            return playbackJobHandle;
        }
    }
}
