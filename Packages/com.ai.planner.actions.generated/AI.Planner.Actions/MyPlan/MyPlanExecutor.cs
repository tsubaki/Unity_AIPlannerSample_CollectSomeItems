using System;
using System.Collections.Generic;
using System.Linq;
using AI.Planner.Domains;
using Unity.AI.Planner;
using Unity.AI.Planner.DomainLanguage.TraitBased;
using UnityEngine.AI.Planner.Controller;
using UnityEngine.AI.Planner.DomainLanguage.TraitBased;

namespace AI.Planner.Actions.MyPlan
{
    public struct DefaultHeuristic : IHeuristic<StateData>
    {
        public BoundedValue Evaluate(StateData state)
        {
            return new BoundedValue(-100, 0, 100);
        }
    }

    public struct TerminationEvaluator : ITerminationEvaluator<StateData>
    {
        public bool IsTerminal(StateData state, out float terminalReward)
        {
            terminalReward = 0f;
            var terminal = false;
            
            var GoalCompleteInstance = new GoalComplete();
            if (GoalCompleteInstance.IsTerminal(state))
            {
                terminal = true;
                terminalReward += GoalCompleteInstance.TerminalReward(state);
            }
            return terminal;
        }
    }

    class MyPlanExecutor : BasePlanExecutor<TraitBasedObject, StateEntityKey, StateData, StateDataContext, ActionScheduler, DefaultHeuristic, TerminationEvaluator, StateManager, ActionKey, DestroyStatesJobScheduler>
    {
        static Dictionary<Guid, string> s_ActionGuidToNameLookup = new Dictionary<Guid,string>()
        {
            { ActionScheduler.TakeAItemGuid, nameof(TakeAItem) },
            { ActionScheduler.MoveToGuid, nameof(MoveTo) },
            { ActionScheduler.PutOnItemGuid, nameof(PutOnItem) },
        };

        public override string GetActionName(IActionKey actionKey)
        {
            s_ActionGuidToNameLookup.TryGetValue(((IActionKeyWithGuid)actionKey).ActionGuid, out var name);
            return name;
        }

        protected override void RegisterOnDestroyCallback()
        {
            m_StateManager.Destroying += () => PlannerScheduler.CurrentJobHandle.Complete();
        }

        public override void Act(DecisionController controller)
        {
            var actionKey = GetBestAction();
            var stateData = m_StateManager.GetStateData(CurrentStateKey, false);
            var actionName = string.Empty;

            switch (actionKey.ActionGuid)
            {
                case var actionGuid when actionGuid == ActionScheduler.TakeAItemGuid:
                    actionName = nameof(TakeAItem);
                    break;
                case var actionGuid when actionGuid == ActionScheduler.MoveToGuid:
                    actionName = nameof(MoveTo);
                    break;
                case var actionGuid when actionGuid == ActionScheduler.PutOnItemGuid:
                    actionName = nameof(PutOnItem);
                    break;
            }

            var executeInfos = controller.GetExecutionInfo(actionName);
            if (executeInfos == null)
                return;

            var argumentMapping = executeInfos.GetArgumentValues();
            var arguments = new object[argumentMapping.Count()];
            var i = 0;
            foreach (var argument in argumentMapping)
            {
                var split = argument.Split('.');

                int parameterIndex = -1;
                var traitBasedObjectName = split[0];

                if (string.IsNullOrEmpty(traitBasedObjectName))
                    throw new ArgumentException($"An argument to the '{actionName}' callback on '{controller.name}' DecisionController is invalid");

                switch (actionName)
                {
                    case nameof(TakeAItem):
                        parameterIndex = TakeAItem.GetIndexForParameterName(traitBasedObjectName);
                        break;
                    case nameof(MoveTo):
                        parameterIndex = MoveTo.GetIndexForParameterName(traitBasedObjectName);
                        break;
                    case nameof(PutOnItem):
                        parameterIndex = PutOnItem.GetIndexForParameterName(traitBasedObjectName);
                        break;
                }

                var traitBasedObjectIndex = actionKey[parameterIndex];
                if (split.Length > 1)
                {
                    switch (split[1])
                    {
                        case nameof(Baggage):
                            var traitBaggage = stateData.GetTraitOnObjectAtIndex<AI.Planner.Domains.Baggage>(traitBasedObjectIndex);
                            arguments[i] = split.Length == 3 ? traitBaggage.GetField(split[2]) : traitBaggage;
                            break;
                        case nameof(Location):
                            var traitLocation = stateData.GetTraitOnObjectAtIndex<Unity.AI.Planner.DomainLanguage.TraitBased.Location>(traitBasedObjectIndex);
                            arguments[i] = split.Length == 3 ? traitLocation.GetField(split[2]) : traitLocation;
                            break;
                        case nameof(Item):
                            var traitItem = stateData.GetTraitOnObjectAtIndex<AI.Planner.Domains.Item>(traitBasedObjectIndex);
                            arguments[i] = split.Length == 3 ? traitItem.GetField(split[2]) : traitItem;
                            break;
                        case nameof(Npc):
                            var traitNpc = stateData.GetTraitOnObjectAtIndex<AI.Planner.Domains.Npc>(traitBasedObjectIndex);
                            arguments[i] = split.Length == 3 ? traitNpc.GetField(split[2]) : traitNpc;
                            break;
                        case nameof(WayPoint):
                            var traitWayPoint = stateData.GetTraitOnObjectAtIndex<AI.Planner.Domains.WayPoint>(traitBasedObjectIndex);
                            arguments[i] = split.Length == 3 ? traitWayPoint.GetField(split[2]) : traitWayPoint;
                            break;
                        case nameof(Gate):
                            var traitGate = stateData.GetTraitOnObjectAtIndex<AI.Planner.Domains.Gate>(traitBasedObjectIndex);
                            arguments[i] = split.Length == 3 ? traitGate.GetField(split[2]) : traitGate;
                            break;
                        case nameof(Goal):
                            var traitGoal = stateData.GetTraitOnObjectAtIndex<AI.Planner.Domains.Goal>(traitBasedObjectIndex);
                            arguments[i] = split.Length == 3 ? traitGoal.GetField(split[2]) : traitGoal;
                            break;
                    }
                }
                else
                {
                    var planStateId = stateData.GetTraitBasedObjectId(traitBasedObjectIndex);
                    ITraitBasedObjectData dataSource;
                    if (m_PlanStateToGameStateIdLookup.TryGetValue(planStateId.Id, out var gameStateId))
                        dataSource = m_DomainData.GetDataSource(new TraitBasedObjectId { Id = gameStateId });
                    else
                        dataSource = m_DomainData.GetDataSource(planStateId);

                    Type expectedType = executeInfos.GetParameterType(i);
                    if (typeof(ITraitBasedObjectData).IsAssignableFrom(expectedType))
                    {
                        arguments[i] = dataSource;
                    }
                    else
                    {
                        arguments[i] = null;
                        var obj = dataSource.ParentObject;
                        if (obj != null && obj is UnityEngine.GameObject gameObject)
                        {
                            if (expectedType == typeof(UnityEngine.GameObject))
                                arguments[i] = gameObject;

                            if (typeof(UnityEngine.Component).IsAssignableFrom(expectedType))
                                arguments[i] = gameObject == null ? null : gameObject.GetComponent(expectedType);
                        }
                    }
                }

                i++;
            }

            CurrentActionKey = actionKey;
            controller.StartAction(executeInfos, arguments);
        }
    }
}
