using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.AI.Planner;
using Unity.AI.Planner.DomainLanguage.TraitBased;
using Unity.Burst;
using AI.Planner.Domains;

namespace AI.Planner.Actions.MyPlan
{
    [BurstCompile]
    struct TurnOnGate : IJobParallelForDefer
    {
        public Guid ActionGuid;
        
        const int k_NPCIndex = 0;
        const int k_GateIndex = 1;
        const int k_GameManagerIndex = 2;
        const int k_MaxArguments = 3;

        [ReadOnly] NativeArray<StateEntityKey> m_StatesToExpand;
        StateDataContext m_StateDataContext;

        internal TurnOnGate(Guid guid, NativeList<StateEntityKey> statesToExpand, StateDataContext stateDataContext)
        {
            ActionGuid = guid;
            m_StatesToExpand = statesToExpand.AsDeferredJobArray();
            m_StateDataContext = stateDataContext;
        }

        public static int GetIndexForParameterName(string parameterName)
        {
            
            if (string.Equals(parameterName, "NPC", StringComparison.OrdinalIgnoreCase))
                 return k_NPCIndex;
            if (string.Equals(parameterName, "Gate", StringComparison.OrdinalIgnoreCase))
                 return k_GateIndex;
            if (string.Equals(parameterName, "GameManager", StringComparison.OrdinalIgnoreCase))
                 return k_GameManagerIndex;

            return -1;
        }

        void GenerateArgumentPermutations(StateData stateData, NativeList<ActionKey> argumentPermutations)
        {
            var NPCFilter = new NativeArray<ComponentType>(3, Allocator.Temp){[0] = ComponentType.ReadWrite<AI.Planner.Domains.Npc>(),[1] = ComponentType.ReadWrite<Unity.AI.Planner.DomainLanguage.TraitBased.Location>(),[2] = ComponentType.ReadWrite<AI.Planner.Domains.Baggage>(),  };
            var GateFilter = new NativeArray<ComponentType>(2, Allocator.Temp){[0] = ComponentType.ReadWrite<AI.Planner.Domains.Gate>(),[1] = ComponentType.ReadWrite<Unity.AI.Planner.DomainLanguage.TraitBased.Location>(),  };
            var GameManagerFilter = new NativeArray<ComponentType>(1, Allocator.Temp){[0] = ComponentType.ReadWrite<AI.Planner.Domains.GateSwitch>(),  };
            var NPCObjectIndices = new NativeList<int>(2, Allocator.Temp);
            stateData.GetTraitBasedObjectIndices(NPCObjectIndices, NPCFilter);
            var GateObjectIndices = new NativeList<int>(2, Allocator.Temp);
            stateData.GetTraitBasedObjectIndices(GateObjectIndices, GateFilter);
            var GameManagerObjectIndices = new NativeList<int>(2, Allocator.Temp);
            stateData.GetTraitBasedObjectIndices(GameManagerObjectIndices, GameManagerFilter);
            var LocationBuffer = stateData.LocationBuffer;
            var BaggageBuffer = stateData.BaggageBuffer;
            
            for (int i0 = 0; i0 < NPCObjectIndices.Length; i0++)
            {
                var NPCIndex = NPCObjectIndices[i0];
                var NPCObject = stateData.TraitBasedObjects[NPCIndex];
                
                
                if (!(BaggageBuffer[NPCObject.BaggageIndex].ItemCount == 1))
                    continue;
            
            for (int i1 = 0; i1 < GateObjectIndices.Length; i1++)
            {
                var GateIndex = GateObjectIndices[i1];
                var GateObject = stateData.TraitBasedObjects[GateIndex];
                
                if (!(LocationBuffer[NPCObject.LocationIndex].Position == LocationBuffer[GateObject.LocationIndex].Position))
                    continue;
                
            
            for (int i2 = 0; i2 < GameManagerObjectIndices.Length; i2++)
            {
                var GameManagerIndex = GameManagerObjectIndices[i2];
                var GameManagerObject = stateData.TraitBasedObjects[GameManagerIndex];
                
                

                var actionKey = new ActionKey(k_MaxArguments) {
                                                        ActionGuid = ActionGuid,
                                                       [k_NPCIndex] = NPCIndex,
                                                       [k_GateIndex] = GateIndex,
                                                       [k_GameManagerIndex] = GameManagerIndex,
                                                    };
                argumentPermutations.Add(actionKey);
            }
            }
            }
            NPCObjectIndices.Dispose();
            GateObjectIndices.Dispose();
            GameManagerObjectIndices.Dispose();
            NPCFilter.Dispose();
            GateFilter.Dispose();
            GameManagerFilter.Dispose();
        }

        StateTransitionInfoPair<StateEntityKey, ActionKey, StateTransitionInfo> ApplyEffects(ActionKey action, StateEntityKey originalStateEntityKey)
        {
            var originalState = m_StateDataContext.GetStateData(originalStateEntityKey);
            var originalStateObjectBuffer = originalState.TraitBasedObjects;
            var originalGameManagerObject = originalStateObjectBuffer[action[k_GameManagerIndex]];
            var originalNPCObject = originalStateObjectBuffer[action[k_NPCIndex]];

            var newState = m_StateDataContext.CopyStateData(originalState);
            var newGateSwitchBuffer = newState.GateSwitchBuffer;
            var newBaggageBuffer = newState.BaggageBuffer;
            {
                    var @GateSwitch = newGateSwitchBuffer[originalGameManagerObject.GateSwitchIndex];
                    @GateSwitch.@OpenCount += 1;
                    newGateSwitchBuffer[originalGameManagerObject.GateSwitchIndex] = @GateSwitch;
            }
            {
                    var @Baggage = newBaggageBuffer[originalNPCObject.BaggageIndex];
                    @Baggage.@ItemCount -= 1;
                    newBaggageBuffer[originalNPCObject.BaggageIndex] = @Baggage;
            }

            
            newState.RemoveTraitBasedObjectAtIndex(action[k_GateIndex]);

            var reward = Reward(originalState, action, newState);
            var StateTransitionInfo = new StateTransitionInfo { Probability = 1f, TransitionUtilityValue = reward };
            var resultingStateKey = m_StateDataContext.GetStateDataKey(newState);

            return new StateTransitionInfoPair<StateEntityKey, ActionKey, StateTransitionInfo>(originalStateEntityKey, action, resultingStateKey, StateTransitionInfo);
        }

        float Reward(StateData originalState, ActionKey action, StateData newState)
        {
            var reward = 1f;

            return reward;
        }

        public void Execute(int jobIndex)
        {
            m_StateDataContext.JobIndex = jobIndex; //todo check that all actions set the job index

            var stateEntityKey = m_StatesToExpand[jobIndex];
            var stateData = m_StateDataContext.GetStateData(stateEntityKey);

            var argumentPermutations = new NativeList<ActionKey>(4, Allocator.Temp);
            GenerateArgumentPermutations(stateData, argumentPermutations);

            var transitionInfo = new NativeArray<TurnOnGateFixupReference>(argumentPermutations.Length, Allocator.Temp);
            for (var i = 0; i < argumentPermutations.Length; i++)
            {
                transitionInfo[i] = new TurnOnGateFixupReference { TransitionInfo = ApplyEffects(argumentPermutations[i], stateEntityKey) };
            }

            // fixups
            var stateEntity = stateEntityKey.Entity;
            var fixupBuffer = m_StateDataContext.EntityCommandBuffer.AddBuffer<TurnOnGateFixupReference>(jobIndex, stateEntity);
            fixupBuffer.CopyFrom(transitionInfo);

            transitionInfo.Dispose();
            argumentPermutations.Dispose();
        }

        
        public static T GetNPCTrait<T>(StateData state, ActionKey action) where T : struct, ITrait
        {
            return state.GetTraitOnObjectAtIndex<T>(action[k_NPCIndex]);
        }
        
        public static T GetGateTrait<T>(StateData state, ActionKey action) where T : struct, ITrait
        {
            return state.GetTraitOnObjectAtIndex<T>(action[k_GateIndex]);
        }
        
        public static T GetGameManagerTrait<T>(StateData state, ActionKey action) where T : struct, ITrait
        {
            return state.GetTraitOnObjectAtIndex<T>(action[k_GameManagerIndex]);
        }
        
    }

    public struct TurnOnGateFixupReference : IBufferElementData
    {
        internal StateTransitionInfoPair<StateEntityKey, ActionKey, StateTransitionInfo> TransitionInfo;
    }
}


