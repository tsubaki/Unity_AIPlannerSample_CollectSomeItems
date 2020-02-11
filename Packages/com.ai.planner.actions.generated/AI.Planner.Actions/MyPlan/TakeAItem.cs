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
    struct TakeAItem : IJobParallelForDefer
    {
        public Guid ActionGuid;
        
        const int k_NPCIndex = 0;
        const int k_ItemIndex = 1;
        const int k_MaxArguments = 2;

        [ReadOnly] NativeArray<StateEntityKey> m_StatesToExpand;
        StateDataContext m_StateDataContext;

        internal TakeAItem(Guid guid, NativeList<StateEntityKey> statesToExpand, StateDataContext stateDataContext)
        {
            ActionGuid = guid;
            m_StatesToExpand = statesToExpand.AsDeferredJobArray();
            m_StateDataContext = stateDataContext;
        }

        public static int GetIndexForParameterName(string parameterName)
        {
            
            if (string.Equals(parameterName, "NPC", StringComparison.OrdinalIgnoreCase))
                 return k_NPCIndex;
            if (string.Equals(parameterName, "Item", StringComparison.OrdinalIgnoreCase))
                 return k_ItemIndex;

            return -1;
        }

        void GenerateArgumentPermutations(StateData stateData, NativeList<ActionKey> argumentPermutations)
        {
            var NPCFilter = new NativeArray<ComponentType>(2, Allocator.Temp){[0] = ComponentType.ReadWrite<AI.Planner.Domains.Baggage>(),[1] = ComponentType.ReadWrite<Unity.AI.Planner.DomainLanguage.TraitBased.Location>(),  };
            var ItemFilter = new NativeArray<ComponentType>(2, Allocator.Temp){[0] = ComponentType.ReadWrite<Unity.AI.Planner.DomainLanguage.TraitBased.Location>(),[1] = ComponentType.ReadWrite<AI.Planner.Domains.Item>(),  };
            var NPCObjectIndices = new NativeList<int>(2, Allocator.Temp);
            stateData.GetTraitBasedObjectIndices(NPCObjectIndices, NPCFilter);
            var ItemObjectIndices = new NativeList<int>(2, Allocator.Temp);
            stateData.GetTraitBasedObjectIndices(ItemObjectIndices, ItemFilter);
            var LocationBuffer = stateData.LocationBuffer;
            var BaggageBuffer = stateData.BaggageBuffer;
            
            for (int i0 = 0; i0 < NPCObjectIndices.Length; i0++)
            {
                var NPCIndex = NPCObjectIndices[i0];
                var NPCObject = stateData.TraitBasedObjects[NPCIndex];
                
                
                if (!(BaggageBuffer[NPCObject.BaggageIndex].HasItem == false))
                    continue;
            
            for (int i1 = 0; i1 < ItemObjectIndices.Length; i1++)
            {
                var ItemIndex = ItemObjectIndices[i1];
                var ItemObject = stateData.TraitBasedObjects[ItemIndex];
                
                if (!(LocationBuffer[NPCObject.LocationIndex].Position == LocationBuffer[ItemObject.LocationIndex].Position))
                    continue;
                

                var actionKey = new ActionKey(k_MaxArguments) {
                                                        ActionGuid = ActionGuid,
                                                       [k_NPCIndex] = NPCIndex,
                                                       [k_ItemIndex] = ItemIndex,
                                                    };
                argumentPermutations.Add(actionKey);
            }
            }
            NPCObjectIndices.Dispose();
            ItemObjectIndices.Dispose();
            NPCFilter.Dispose();
            ItemFilter.Dispose();
        }

        StateTransitionInfoPair<StateEntityKey, ActionKey, StateTransitionInfo> ApplyEffects(ActionKey action, StateEntityKey originalStateEntityKey)
        {
            var originalState = m_StateDataContext.GetStateData(originalStateEntityKey);
            var originalStateObjectBuffer = originalState.TraitBasedObjects;
            var originalNPCObject = originalStateObjectBuffer[action[k_NPCIndex]];

            var newState = m_StateDataContext.CopyStateData(originalState);
            var newBaggageBuffer = newState.BaggageBuffer;
            {
                    var @Baggage = newBaggageBuffer[originalNPCObject.BaggageIndex];
                    @Baggage.@HasItem = true;
                    newBaggageBuffer[originalNPCObject.BaggageIndex] = @Baggage;
            }

            
            newState.RemoveTraitBasedObjectAtIndex(action[k_ItemIndex]);

            var reward = Reward(originalState, action, newState);
            var StateTransitionInfo = new StateTransitionInfo { Probability = 1f, TransitionUtilityValue = reward };
            var resultingStateKey = m_StateDataContext.GetStateDataKey(newState);

            return new StateTransitionInfoPair<StateEntityKey, ActionKey, StateTransitionInfo>(originalStateEntityKey, action, resultingStateKey, StateTransitionInfo);
        }

        float Reward(StateData originalState, ActionKey action, StateData newState)
        {
            var reward = 0f;

            return reward;
        }

        public void Execute(int jobIndex)
        {
            m_StateDataContext.JobIndex = jobIndex; //todo check that all actions set the job index

            var stateEntityKey = m_StatesToExpand[jobIndex];
            var stateData = m_StateDataContext.GetStateData(stateEntityKey);

            var argumentPermutations = new NativeList<ActionKey>(4, Allocator.Temp);
            GenerateArgumentPermutations(stateData, argumentPermutations);

            var transitionInfo = new NativeArray<TakeAItemFixupReference>(argumentPermutations.Length, Allocator.Temp);
            for (var i = 0; i < argumentPermutations.Length; i++)
            {
                transitionInfo[i] = new TakeAItemFixupReference { TransitionInfo = ApplyEffects(argumentPermutations[i], stateEntityKey) };
            }

            // fixups
            var stateEntity = stateEntityKey.Entity;
            var fixupBuffer = m_StateDataContext.EntityCommandBuffer.AddBuffer<TakeAItemFixupReference>(jobIndex, stateEntity);
            fixupBuffer.CopyFrom(transitionInfo);

            transitionInfo.Dispose();
            argumentPermutations.Dispose();
        }

        
        public static T GetNPCTrait<T>(StateData state, ActionKey action) where T : struct, ITrait
        {
            return state.GetTraitOnObjectAtIndex<T>(action[k_NPCIndex]);
        }
        
        public static T GetItemTrait<T>(StateData state, ActionKey action) where T : struct, ITrait
        {
            return state.GetTraitOnObjectAtIndex<T>(action[k_ItemIndex]);
        }
        
    }

    public struct TakeAItemFixupReference : IBufferElementData
    {
        internal StateTransitionInfoPair<StateEntityKey, ActionKey, StateTransitionInfo> TransitionInfo;
    }
}


