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
            public EntityCommandBuffer MoveToGoalECB;

            public void Execute()
            {
                // Playback entity changes and output state transition info
                var entityManager = ExclusiveEntityTransaction;

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
            var MoveToGoalDataContext = StateManager.GetStateDataContext();
            var MoveToGoalECB = StateManager.GetEntityCommandBuffer();
            MoveToGoalDataContext.EntityCommandBuffer = MoveToGoalECB.ToConcurrent();

            var allActionJobs = new NativeArray<JobHandle>(2, Allocator.TempJob)
            {
                [0] = new MoveToGoal(MoveToGoalGuid, UnexpandedStates, MoveToGoalDataContext).Schedule(UnexpandedStates, 0, inputDeps),
                [1] = entityManager.ExclusiveEntityTransactionDependency
            };

            var allActionJobsHandle = JobHandle.CombineDependencies(allActionJobs);
            allActionJobs.Dispose();

            // Playback entity changes and output state transition info
            var playbackJob = new PlaybackECB()
            {
                ExclusiveEntityTransaction = StateManager.ExclusiveEntityTransaction,
                UnexpandedStates = UnexpandedStates,
                CreatedStateInfo = m_CreatedStateInfo,
                MoveToGoalECB = MoveToGoalECB,
            };

            var playbackJobHandle = playbackJob.Schedule(allActionJobsHandle);
            entityManager.ExclusiveEntityTransactionDependency = playbackJobHandle;

            return playbackJobHandle;
        }
    }
}
