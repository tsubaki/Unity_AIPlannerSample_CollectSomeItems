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
        
        const int k_BuggageIndex = 0;
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
            
            if (string.Equals(parameterName, "Buggage", StringComparison.OrdinalIgnoreCase))
                 return k_BuggageIndex;
            if (string.Equals(parameterName, "Item", StringComparison.OrdinalIgnoreCase))
                 return k_ItemIndex;

            return -1;
        }

        void GenerateArgumentPermutations(StateData stateData, NativeList<ActionKey> argumentPermutations)
        {
            var BuggageFilter = new NativeArray<ComponentType>(3, Allocator.Temp){[0] = ComponentType.ReadWrite<AI.Planner.Domains.Baggage>(),[1] = ComponentType.ReadWrite<Unity.AI.Planner.DomainLanguage.TraitBased.Location>(),[2] = ComponentType.ReadWrite<AI.Planner.Domains.Npc>(),  };
            var ItemFilter = new NativeArray<ComponentType>(2, Allocator.Temp){[0] = ComponentType.ReadWrite<Unity.AI.Planner.DomainLanguage.TraitBased.Location>(),[1] = ComponentType.ReadWrite<AI.Planner.Domains.Item>(),  };
            var BuggageObjectIndices = new NativeList<int>(2, Allocator.Temp);
            stateData.GetTraitBasedObjectIndices(BuggageObjectIndices, BuggageFilter);
            var ItemObjectIndices = new NativeList<int>(2, Allocator.Temp);
            stateData.GetTraitBasedObjectIndices(ItemObjectIndices, ItemFilter);
            var LocationBuffer = stateData.LocationBuffer;
            var BaggageBuffer = stateData.BaggageBuffer;
            
            for (int i0 = 0; i0 < BuggageObjectIndices.Length; i0++)
            {
                var BuggageIndex = BuggageObjectIndices[i0];
                var BuggageObject = stateData.TraitBasedObjects[BuggageIndex];
                
                
                if (!(BaggageBuffer[BuggageObject.BaggageIndex].ItemCount == 0))
                    continue;
            
            for (int i1 = 0; i1 < ItemObjectIndices.Length; i1++)
            {
                var ItemIndex = ItemObjectIndices[i1];
                var ItemObject = stateData.TraitBasedObjects[ItemIndex];
                
                if (!(LocationBuffer[BuggageObject.LocationIndex].Position == LocationBuffer[ItemObject.LocationIndex].Position))
                    continue;
                

                var actionKey = new ActionKey(k_MaxArguments) {
                                                        ActionGuid = ActionGuid,
                                                       [k_BuggageIndex] = BuggageIndex,
                                                       [k_ItemIndex] = ItemIndex,
                                                    };
                argumentPermutations.Add(actionKey);
            }
            }
            BuggageObjectIndices.Dispose();
            ItemObjectIndices.Dispose();
            BuggageFilter.Dispose();
            ItemFilter.Dispose();
        }

        StateTransitionInfoPair<StateEntityKey, ActionKey, StateTransitionInfo> ApplyEffects(ActionKey action, StateEntityKey originalStateEntityKey)
        {
            var originalState = m_StateDataContext.GetStateData(originalStateEntityKey);
            var originalStateObjectBuffer = originalState.TraitBasedObjects;
            var originalBuggageObject = originalStateObjectBuffer[action[k_BuggageIndex]];

            var newState = m_StateDataContext.CopyStateData(originalState);
            var newBaggageBuffer = newState.BaggageBuffer;
            {
                    var @Baggage = newBaggageBuffer[originalBuggageObject.BaggageIndex];
                    @Baggage.@ItemCount = 1;
                    newBaggageBuffer[originalBuggageObject.BaggageIndex] = @Baggage;
            }

            
            newState.RemoveTraitBasedObjectAtIndex(action[k_ItemIndex]);

            var reward = Reward(originalState, action, newState);
            var StateTransitionInfo = new StateTransitionInfo { Probability = 1f, TransitionUtilityValue = reward };
            var resultingStateKey = m_StateDataContext.GetStateDataKey(newState);

            return new StateTransitionInfoPair<StateEntityKey, ActionKey, StateTransitionInfo>(originalStateEntityKey, action, resultingStateKey, StateTransitionInfo);
        }

        float Reward(StateData originalState, ActionKey action, StateData newState)
        {
            var reward = -0.1f;

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

        
        public static T GetBuggageTrait<T>(StateData state, ActionKey action) where T : struct, ITrait
        {
            return state.GetTraitOnObjectAtIndex<T>(action[k_BuggageIndex]);
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


