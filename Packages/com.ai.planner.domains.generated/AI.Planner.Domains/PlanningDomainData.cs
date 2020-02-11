using System;
using System.Text;
using System.Collections.Generic;
using Unity.AI.Planner;
using Unity.AI.Planner.DomainLanguage.TraitBased;
using Unity.AI.Planner.Jobs;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace AI.Planner.Domains
{
    // Domains don't share key types to enforce that planners are domain specific
    public struct StateEntityKey : IEquatable<StateEntityKey>, IStateKey
    {
        public Entity Entity;
        public int HashCode;

        public bool Equals(StateEntityKey other) => Entity == other.Entity;

        public override int GetHashCode() => HashCode;

        public override string ToString() => $"StateEntityKey ({Entity} {HashCode})";
        public string Label => $"State{Entity}";
    }

    public static class TraitArrayIndex<T> where T : struct, ITrait
    {
        public static readonly int Index = -1;

        static TraitArrayIndex()
        {
            var typeIndex = TypeManager.GetTypeIndex<T>();
            if (typeIndex == TypeManager.GetTypeIndex<Goal>())
                Index = 0;
            else if (typeIndex == TypeManager.GetTypeIndex<Npc>())
                Index = 1;
            else if (typeIndex == TypeManager.GetTypeIndex<WayPoint>())
                Index = 2;
            else if (typeIndex == TypeManager.GetTypeIndex<Location>())
                Index = 3;
            else if (typeIndex == TypeManager.GetTypeIndex<Moveable>())
                Index = 4;
        }
    }

    public struct TraitBasedObject : ITraitBasedObject
    {
        public int Length => 5;

        public byte this[int i]
        {
            get
            {
                switch (i)
                {
                    case 0:
                        return GoalIndex;
                    case 1:
                        return NpcIndex;
                    case 2:
                        return WayPointIndex;
                    case 3:
                        return LocationIndex;
                    case 4:
                        return MoveableIndex;
                }

                return Unset;
            }
            set
            {
                switch (i)
                {
                    case 0:
                        GoalIndex = value;
                        break;
                    case 1:
                        NpcIndex = value;
                        break;
                    case 2:
                        WayPointIndex = value;
                        break;
                    case 3:
                        LocationIndex = value;
                        break;
                    case 4:
                        MoveableIndex = value;
                        break;
                }
            }
        }

        public static readonly byte Unset = Byte.MaxValue;

        public static TraitBasedObject Default => new TraitBasedObject
        {
            GoalIndex = Unset,
            NpcIndex = Unset,
            WayPointIndex = Unset,
            LocationIndex = Unset,
            MoveableIndex = Unset,
        };


        public byte GoalIndex;
        public byte NpcIndex;
        public byte WayPointIndex;
        public byte LocationIndex;
        public byte MoveableIndex;


        static readonly int s_GoalTypeIndex = TypeManager.GetTypeIndex<Goal>();
        static readonly int s_NpcTypeIndex = TypeManager.GetTypeIndex<Npc>();
        static readonly int s_WayPointTypeIndex = TypeManager.GetTypeIndex<WayPoint>();
        static readonly int s_LocationTypeIndex = TypeManager.GetTypeIndex<Location>();
        static readonly int s_MoveableTypeIndex = TypeManager.GetTypeIndex<Moveable>();

        public bool HasSameTraits(TraitBasedObject other)
        {
            for (var i = 0; i < Length; i++)
            {
                var traitIndex = this[i];
                var otherTraitIndex = other[i];
                if (traitIndex == Unset && otherTraitIndex != Unset || traitIndex != Unset && otherTraitIndex == Unset)
                    return false;
            }
            return true;
        }

        public bool HasTraitSubset(TraitBasedObject traitSubset)
        {
            for (var i = 0; i < Length; i++)
            {
                var requiredTrait = traitSubset[i];
                if (requiredTrait != Unset && this[i] == Unset)
                    return false;
            }
            return true;
        }

        // todo - replace with more efficient subset check
        public bool MatchesTraitFilter(NativeArray<ComponentType> componentTypes)
        {
            for (int i = 0; i < componentTypes.Length; i++)
            {
                var t = componentTypes[i];

                if (t.TypeIndex == s_GoalTypeIndex)
                {
                    if (t.AccessModeType == ComponentType.AccessMode.Exclude ^ GoalIndex == Unset)
                        return false;
                }
                else if (t.TypeIndex == s_NpcTypeIndex)
                {
                    if (t.AccessModeType == ComponentType.AccessMode.Exclude ^ NpcIndex == Unset)
                        return false;
                }
                else if (t.TypeIndex == s_WayPointTypeIndex)
                {
                    if (t.AccessModeType == ComponentType.AccessMode.Exclude ^ WayPointIndex == Unset)
                        return false;
                }
                else if (t.TypeIndex == s_LocationTypeIndex)
                {
                    if (t.AccessModeType == ComponentType.AccessMode.Exclude ^ LocationIndex == Unset)
                        return false;
                }
                else if (t.TypeIndex == s_MoveableTypeIndex)
                {
                    if (t.AccessModeType == ComponentType.AccessMode.Exclude ^ MoveableIndex == Unset)
                        return false;
                }
                else
                    throw new ArgumentException($"Incorrect trait type used in domain object query: {t}");
            }

            return true;
        }

        public bool MatchesTraitFilter(ComponentType[] componentTypes)
        {
            for (int i = 0; i < componentTypes.Length; i++)
            {
                var t = componentTypes[i];

                if (t.TypeIndex == s_GoalTypeIndex)
                {
                    if (t.AccessModeType == ComponentType.AccessMode.Exclude ^ GoalIndex == Unset)
                        return false;
                }
                else if (t.TypeIndex == s_NpcTypeIndex)
                {
                    if (t.AccessModeType == ComponentType.AccessMode.Exclude ^ NpcIndex == Unset)
                        return false;
                }
                else if (t.TypeIndex == s_WayPointTypeIndex)
                {
                    if (t.AccessModeType == ComponentType.AccessMode.Exclude ^ WayPointIndex == Unset)
                        return false;
                }
                else if (t.TypeIndex == s_LocationTypeIndex)
                {
                    if (t.AccessModeType == ComponentType.AccessMode.Exclude ^ LocationIndex == Unset)
                        return false;
                }
                else if (t.TypeIndex == s_MoveableTypeIndex)
                {
                    if (t.AccessModeType == ComponentType.AccessMode.Exclude ^ MoveableIndex == Unset)
                        return false;
                }
                else
                    throw new ArgumentException($"Incorrect trait type used in domain object query: {t}");
            }

            return true;
        }
    }

    public struct StateData : ITraitBasedStateData<TraitBasedObject, StateData>
    {
        public Entity StateEntity;
        public DynamicBuffer<TraitBasedObject> TraitBasedObjects;
        public DynamicBuffer<TraitBasedObjectId> TraitBasedObjectIds;

        public DynamicBuffer<Goal> GoalBuffer;
        public DynamicBuffer<Npc> NpcBuffer;
        public DynamicBuffer<WayPoint> WayPointBuffer;
        public DynamicBuffer<Location> LocationBuffer;
        public DynamicBuffer<Moveable> MoveableBuffer;

        static readonly int s_GoalTypeIndex = TypeManager.GetTypeIndex<Goal>();
        static readonly int s_NpcTypeIndex = TypeManager.GetTypeIndex<Npc>();
        static readonly int s_WayPointTypeIndex = TypeManager.GetTypeIndex<WayPoint>();
        static readonly int s_LocationTypeIndex = TypeManager.GetTypeIndex<Location>();
        static readonly int s_MoveableTypeIndex = TypeManager.GetTypeIndex<Moveable>();

        public StateData(JobComponentSystem system, Entity stateEntity, bool readWrite = false)
        {
            StateEntity = stateEntity;
            TraitBasedObjects = system.GetBufferFromEntity<TraitBasedObject>(!readWrite)[stateEntity];
            TraitBasedObjectIds = system.GetBufferFromEntity<TraitBasedObjectId>(!readWrite)[stateEntity];

            GoalBuffer = system.GetBufferFromEntity<Goal>(!readWrite)[stateEntity];
            NpcBuffer = system.GetBufferFromEntity<Npc>(!readWrite)[stateEntity];
            WayPointBuffer = system.GetBufferFromEntity<WayPoint>(!readWrite)[stateEntity];
            LocationBuffer = system.GetBufferFromEntity<Location>(!readWrite)[stateEntity];
            MoveableBuffer = system.GetBufferFromEntity<Moveable>(!readWrite)[stateEntity];
        }

        public StateData(int jobIndex, EntityCommandBuffer.Concurrent entityCommandBuffer, Entity stateEntity)
        {
            StateEntity = stateEntity;
            TraitBasedObjects = entityCommandBuffer.AddBuffer<TraitBasedObject>(jobIndex, stateEntity);
            TraitBasedObjectIds = entityCommandBuffer.AddBuffer<TraitBasedObjectId>(jobIndex, stateEntity);

            GoalBuffer = entityCommandBuffer.AddBuffer<Goal>(jobIndex, stateEntity);
            NpcBuffer = entityCommandBuffer.AddBuffer<Npc>(jobIndex, stateEntity);
            WayPointBuffer = entityCommandBuffer.AddBuffer<WayPoint>(jobIndex, stateEntity);
            LocationBuffer = entityCommandBuffer.AddBuffer<Location>(jobIndex, stateEntity);
            MoveableBuffer = entityCommandBuffer.AddBuffer<Moveable>(jobIndex, stateEntity);
        }

        public StateData Copy(int jobIndex, EntityCommandBuffer.Concurrent entityCommandBuffer)
        {
            var stateEntity = entityCommandBuffer.Instantiate(jobIndex, StateEntity);
            var traitBasedObjects = entityCommandBuffer.SetBuffer<TraitBasedObject>(jobIndex, stateEntity);
            traitBasedObjects.CopyFrom(TraitBasedObjects.AsNativeArray());
            var traitBasedObjectIds = entityCommandBuffer.SetBuffer<TraitBasedObjectId>(jobIndex, stateEntity);
            traitBasedObjectIds.CopyFrom(TraitBasedObjectIds.AsNativeArray());

            var Goals = entityCommandBuffer.SetBuffer<Goal>(jobIndex, stateEntity);
            Goals.CopyFrom(GoalBuffer.AsNativeArray());
            var Npcs = entityCommandBuffer.SetBuffer<Npc>(jobIndex, stateEntity);
            Npcs.CopyFrom(NpcBuffer.AsNativeArray());
            var WayPoints = entityCommandBuffer.SetBuffer<WayPoint>(jobIndex, stateEntity);
            WayPoints.CopyFrom(WayPointBuffer.AsNativeArray());
            var Locations = entityCommandBuffer.SetBuffer<Location>(jobIndex, stateEntity);
            Locations.CopyFrom(LocationBuffer.AsNativeArray());
            var Moveables = entityCommandBuffer.SetBuffer<Moveable>(jobIndex, stateEntity);
            Moveables.CopyFrom(MoveableBuffer.AsNativeArray());

            return new StateData
            {
                StateEntity = stateEntity,
                TraitBasedObjects = traitBasedObjects,
                TraitBasedObjectIds = traitBasedObjectIds,

                GoalBuffer = Goals,
                NpcBuffer = Npcs,
                WayPointBuffer = WayPoints,
                LocationBuffer = Locations,
                MoveableBuffer = Moveables,
            };
        }

        public void AddObject(NativeArray<ComponentType> types, out TraitBasedObject traitBasedObject, TraitBasedObjectId objectId, string name = null)
        {
            traitBasedObject = TraitBasedObject.Default;
#if DEBUG
            if (!string.IsNullOrEmpty(name))
                objectId.Name.CopyFrom(name);
#endif

            for (int i = 0; i < types.Length; i++)
            {
                var t = types[i];
                if (t.TypeIndex == s_GoalTypeIndex)
                {
                    GoalBuffer.Add(default);
                    traitBasedObject.GoalIndex = (byte) (GoalBuffer.Length - 1);
                }
                else if (t.TypeIndex == s_NpcTypeIndex)
                {
                    NpcBuffer.Add(default);
                    traitBasedObject.NpcIndex = (byte) (NpcBuffer.Length - 1);
                }
                else if (t.TypeIndex == s_WayPointTypeIndex)
                {
                    WayPointBuffer.Add(default);
                    traitBasedObject.WayPointIndex = (byte) (WayPointBuffer.Length - 1);
                }
                else if (t.TypeIndex == s_LocationTypeIndex)
                {
                    LocationBuffer.Add(default);
                    traitBasedObject.LocationIndex = (byte) (LocationBuffer.Length - 1);
                }
                else if (t.TypeIndex == s_MoveableTypeIndex)
                {
                    MoveableBuffer.Add(default);
                    traitBasedObject.MoveableIndex = (byte) (MoveableBuffer.Length - 1);
                }
            }

            TraitBasedObjectIds.Add(objectId);
            TraitBasedObjects.Add(traitBasedObject);
        }

        public void AddObject(NativeArray<ComponentType> types, out TraitBasedObject traitBasedObject, out TraitBasedObjectId objectId, string name = null)
        {
            objectId = new TraitBasedObjectId() { Id = ObjectId.GetNext() };
            AddObject(types, out traitBasedObject, objectId, name);
        }

        public void SetTraitOnObject(ITrait trait, ref TraitBasedObject traitBasedObject)
        {
            if (trait is Goal GoalTrait)
                SetTraitOnObject(GoalTrait, ref traitBasedObject);
            else if (trait is Npc NpcTrait)
                SetTraitOnObject(NpcTrait, ref traitBasedObject);
            else if (trait is WayPoint WayPointTrait)
                SetTraitOnObject(WayPointTrait, ref traitBasedObject);
            else if (trait is Location LocationTrait)
                SetTraitOnObject(LocationTrait, ref traitBasedObject);
            else if (trait is Moveable MoveableTrait)
                SetTraitOnObject(MoveableTrait, ref traitBasedObject);
            else 
                throw new ArgumentException($"Trait {trait} of type {trait.GetType()} is not supported in this domain.");
        }


        public TTrait GetTraitOnObject<TTrait>(TraitBasedObject traitBasedObject) where TTrait : struct, ITrait
        {
            var traitBasedObjectTraitIndex = TraitArrayIndex<TTrait>.Index;
            if (traitBasedObjectTraitIndex == -1)
                throw new ArgumentException($"Trait {typeof(TTrait)} not supported in this domain");

            var traitBufferIndex = traitBasedObject[traitBasedObjectTraitIndex];
            if (traitBufferIndex == TraitBasedObject.Unset)
                throw new ArgumentException($"Trait of type {typeof(TTrait)} does not exist on object {traitBasedObject}.");

            return GetBuffer<TTrait>()[traitBufferIndex];
        }

        public void SetTraitOnObject<TTrait>(TTrait trait, ref TraitBasedObject traitBasedObject) where TTrait : struct, ITrait
        {
            var objectIndex = GetTraitBasedObjectIndex(traitBasedObject);
            if (objectIndex == -1)
                throw new ArgumentException($"Object {traitBasedObject} does not exist within the state data {this}.");

            var traitIndex = TraitArrayIndex<TTrait>.Index;
            var traitBuffer = GetBuffer<TTrait>();

            var bufferIndex = traitBasedObject[traitIndex];
            if (bufferIndex == TraitBasedObject.Unset)
            {
                traitBuffer.Add(trait);
                traitBasedObject[traitIndex] = (byte) (traitBuffer.Length - 1);

                TraitBasedObjects[objectIndex] = traitBasedObject;
            }
            else
            {
                traitBuffer[bufferIndex] = trait;
            }
        }

        public bool RemoveTraitOnObject<TTrait>(ref TraitBasedObject traitBasedObject) where TTrait : struct, ITrait
        {
            var objectTraitIndex = TraitArrayIndex<TTrait>.Index;
            var traitBuffer = GetBuffer<TTrait>();

            var traitBufferIndex = traitBasedObject[objectTraitIndex];
            if (traitBufferIndex == TraitBasedObject.Unset)
                return false;

            // last index
            var lastBufferIndex = traitBuffer.Length - 1;

            // Swap back and remove
            var lastTrait = traitBuffer[lastBufferIndex];
            traitBuffer[lastBufferIndex] = traitBuffer[traitBufferIndex];
            traitBuffer[traitBufferIndex] = lastTrait;
            traitBuffer.RemoveAt(lastBufferIndex);

            // Update index for object with last trait in buffer
            for (int i = 0; i < TraitBasedObjects.Length; i++)
            {
                var otherTraitBasedObject = TraitBasedObjects[i];
                if (otherTraitBasedObject[objectTraitIndex] == lastBufferIndex)
                {
                    otherTraitBasedObject[objectTraitIndex] = traitBufferIndex;
                    TraitBasedObjects[i] = otherTraitBasedObject;
                    break;
                }
            }

            // Update traitBasedObject in buffer (ref is to a copy)
            for (int i = 0; i < TraitBasedObjects.Length; i++)
            {
                if (traitBasedObject.Equals(TraitBasedObjects[i]))
                {
                    traitBasedObject[objectTraitIndex] = TraitBasedObject.Unset;
                    TraitBasedObjects[i] = traitBasedObject;
                    return true;
                }
            }

            throw new ArgumentException($"TraitBasedObject {traitBasedObject} does not exist in the state container {this}.");
        }

        public bool RemoveObject(TraitBasedObject traitBasedObject)
        {
            var objectIndex = GetTraitBasedObjectIndex(traitBasedObject);
            if (objectIndex == -1)
                return false;


            RemoveTraitOnObject<Goal>(ref traitBasedObject);
            RemoveTraitOnObject<Npc>(ref traitBasedObject);
            RemoveTraitOnObject<WayPoint>(ref traitBasedObject);
            RemoveTraitOnObject<Location>(ref traitBasedObject);
            RemoveTraitOnObject<Moveable>(ref traitBasedObject);

            TraitBasedObjects.RemoveAt(objectIndex);
            TraitBasedObjectIds.RemoveAt(objectIndex);

            return true;
        }


        public TTrait GetTraitOnObjectAtIndex<TTrait>(int traitBasedObjectIndex) where TTrait : struct, ITrait
        {
            var traitBasedObjectTraitIndex = TraitArrayIndex<TTrait>.Index;
            if (traitBasedObjectTraitIndex == -1)
                throw new ArgumentException($"Trait {typeof(TTrait)} not supported in this domain");

            var traitBasedObject = TraitBasedObjects[traitBasedObjectIndex];
            var traitBufferIndex = traitBasedObject[traitBasedObjectTraitIndex];
            if (traitBufferIndex == TraitBasedObject.Unset)
                throw new Exception($"Trait index for {typeof(TTrait)} is not set for domain object {traitBasedObject}");

            return GetBuffer<TTrait>()[traitBufferIndex];
        }

        public void SetTraitOnObjectAtIndex<T>(T trait, int traitBasedObjectIndex) where T : struct, ITrait
        {
            var traitBasedObjectTraitIndex = TraitArrayIndex<T>.Index;
            if (traitBasedObjectTraitIndex == -1)
                throw new ArgumentException($"Trait {typeof(T)} not supported in this domain");

            var traitBasedObject = TraitBasedObjects[traitBasedObjectIndex];
            var traitBufferIndex = traitBasedObject[traitBasedObjectTraitIndex];
            var traitBuffer = GetBuffer<T>();
            if (traitBufferIndex == TraitBasedObject.Unset)
            {
                traitBuffer.Add(trait);
                traitBufferIndex = (byte)(traitBuffer.Length - 1);
                traitBasedObject[traitBasedObjectTraitIndex] = traitBufferIndex;
                TraitBasedObjects[traitBasedObjectIndex] = traitBasedObject;
            }
            else
            {
                traitBuffer[traitBufferIndex] = trait;
            }
        }

        public bool RemoveTraitOnObjectAtIndex<TTrait>(int traitBasedObjectIndex) where TTrait : struct, ITrait
        {
            var objectTraitIndex = TraitArrayIndex<TTrait>.Index;
            var traitBuffer = GetBuffer<TTrait>();

            var traitBasedObject = TraitBasedObjects[traitBasedObjectIndex];
            var traitBufferIndex = traitBasedObject[objectTraitIndex];
            if (traitBufferIndex == TraitBasedObject.Unset)
                return false;

            // last index
            var lastBufferIndex = traitBuffer.Length - 1;

            // Swap back and remove
            var lastTrait = traitBuffer[lastBufferIndex];
            traitBuffer[lastBufferIndex] = traitBuffer[traitBufferIndex];
            traitBuffer[traitBufferIndex] = lastTrait;
            traitBuffer.RemoveAt(lastBufferIndex);

            // Update index for object with last trait in buffer
            for (int i = 0; i < TraitBasedObjects.Length; i++)
            {
                var otherTraitBasedObject = TraitBasedObjects[i];
                if (otherTraitBasedObject[objectTraitIndex] == lastBufferIndex)
                {
                    otherTraitBasedObject[objectTraitIndex] = traitBufferIndex;
                    TraitBasedObjects[i] = otherTraitBasedObject;
                    break;
                }
            }

            traitBasedObject[objectTraitIndex] = TraitBasedObject.Unset;
            TraitBasedObjects[traitBasedObjectIndex] = traitBasedObject;

            return true;
        }

        public bool RemoveTraitBasedObjectAtIndex(int traitBasedObjectIndex)
        {
            RemoveTraitOnObjectAtIndex<Goal>(traitBasedObjectIndex);
            RemoveTraitOnObjectAtIndex<Npc>(traitBasedObjectIndex);
            RemoveTraitOnObjectAtIndex<WayPoint>(traitBasedObjectIndex);
            RemoveTraitOnObjectAtIndex<Location>(traitBasedObjectIndex);
            RemoveTraitOnObjectAtIndex<Moveable>(traitBasedObjectIndex);

            TraitBasedObjects.RemoveAt(traitBasedObjectIndex);
            TraitBasedObjectIds.RemoveAt(traitBasedObjectIndex);

            return true;
        }


        public NativeArray<int> GetTraitBasedObjectIndices(NativeList<int> traitBasedObjectIndices, NativeArray<ComponentType> traitFilter)
        {
            for (var i = 0; i < TraitBasedObjects.Length; i++)
            {
                var traitBasedObject = TraitBasedObjects[i];
                if (traitBasedObject.MatchesTraitFilter(traitFilter))
                    traitBasedObjectIndices.Add(i);
            }

            return traitBasedObjectIndices.AsArray();
        }

        public NativeArray<int> GetTraitBasedObjectIndices(NativeList<int> traitBasedObjectIndices, params ComponentType[] traitFilter)
        {
            for (var i = 0; i < TraitBasedObjects.Length; i++)
            {
                var traitBasedObject = TraitBasedObjects[i];
                if (traitBasedObject.MatchesTraitFilter(traitFilter))
                    traitBasedObjectIndices.Add(i);
            }

            return traitBasedObjectIndices.AsArray();
        }

        public int GetTraitBasedObjectIndex(TraitBasedObject traitBasedObject)
        {
            for (int objectIndex = 0; objectIndex < TraitBasedObjects.Length; objectIndex++)
            {
                bool match = true;
                var other = TraitBasedObjects[objectIndex];
                for (int i = 0; i < traitBasedObject.Length && match; i++)
                {
                    match &= traitBasedObject[i] == other[i];
                }

                if (match)
                    return objectIndex;
            }

            return -1;
        }

        public int GetTraitBasedObjectIndex(TraitBasedObjectId traitBasedObjectId)
        {
            var objectIndex = -1;
            for (int i = 0; i < TraitBasedObjectIds.Length; i++)
            {
                if (TraitBasedObjectIds[i].Equals(traitBasedObjectId))
                {
                    objectIndex = i;
                    break;
                }
            }

            return objectIndex;
        }

        public TraitBasedObjectId GetTraitBasedObjectId(TraitBasedObject traitBasedObject)
        {
            var index = GetTraitBasedObjectIndex(traitBasedObject);
            return TraitBasedObjectIds[index];
        }

        public TraitBasedObjectId GetTraitBasedObjectId(int traitBasedObjectIndex)
        {
            return TraitBasedObjectIds[traitBasedObjectIndex];
        }

        public TraitBasedObject GetTraitBasedObject(TraitBasedObjectId traitBasedObject)
        {
            var index = GetTraitBasedObjectIndex(traitBasedObject);
            return TraitBasedObjects[index];
        }


        DynamicBuffer<T> GetBuffer<T>() where T : struct, ITrait
        {
            var index = TraitArrayIndex<T>.Index;
            switch (index)
            {
                case 0:
                    return GoalBuffer.Reinterpret<T>();
                case 1:
                    return NpcBuffer.Reinterpret<T>();
                case 2:
                    return WayPointBuffer.Reinterpret<T>();
                case 3:
                    return LocationBuffer.Reinterpret<T>();
                case 4:
                    return MoveableBuffer.Reinterpret<T>();
            }

            return default;
        }

        public bool Equals(StateData rhsState)
        {
            if (StateEntity == rhsState.StateEntity)
                return true;

            // Easy check is to make sure each state has the same number of domain objects
            if (TraitBasedObjects.Length != rhsState.TraitBasedObjects.Length
                || GoalBuffer.Length != rhsState.GoalBuffer.Length
                || NpcBuffer.Length != rhsState.NpcBuffer.Length
                || WayPointBuffer.Length != rhsState.WayPointBuffer.Length
                || LocationBuffer.Length != rhsState.LocationBuffer.Length
                || MoveableBuffer.Length != rhsState.MoveableBuffer.Length)
                return false;

            var objectMap = new ObjectCorrespondence(TraitBasedObjectIds.Length, Allocator.Temp);
            bool statesEqual = TryGetObjectMapping(rhsState, objectMap);
            objectMap.Dispose();

            return statesEqual;
        }

        bool ITraitBasedStateData<TraitBasedObject, StateData>.TryGetObjectMapping(StateData rhsState, ObjectCorrespondence objectMap)
        {
            return TryGetObjectMapping(rhsState, objectMap);
        }

        bool TryGetObjectMapping(StateData rhsState, ObjectCorrespondence objectMap)
        {
            objectMap.Initialize(TraitBasedObjectIds, rhsState.TraitBasedObjectIds);

            bool statesEqual = true;
            for (int lhsIndex = 0; lhsIndex < TraitBasedObjects.Length; lhsIndex++)
            {
                var lhsId = TraitBasedObjectIds[lhsIndex].Id;
                if (objectMap.TryGetValue(lhsId, out _)) // already matched
                    continue;

                // todo lhsIndex to start? would require swapping rhs on assignments, though
                bool matchFound = true;
                for (var rhsIndex = 0; rhsIndex < rhsState.TraitBasedObjects.Length; rhsIndex++)
                {
                    var rhsId = rhsState.TraitBasedObjectIds[rhsIndex].Id;
                    if (objectMap.ContainsRHS(rhsId)) // skip if already assigned todo optimize this
                        continue;

                    objectMap.BeginNewTraversal();
                    objectMap.Add(lhsId, rhsId);

                    // Traversal comparing all reachable objects
                    matchFound = true;
                    while (objectMap.Next(out var lhsIdToEvaluate, out var rhsIdToEvaluate))
                    {
                        // match objects, queueing as needed
                        var lhsTraitBasedObject = TraitBasedObjects[objectMap.GetLHSIndex(lhsIdToEvaluate)];
                        var rhsTraitBasedObject = rhsState.TraitBasedObjects[objectMap.GetRHSIndex(rhsIdToEvaluate)];

                        if (!ObjectsMatchAttributes(lhsTraitBasedObject, rhsTraitBasedObject, rhsState) ||
                            !CheckRelationsAndQueueObjects(lhsTraitBasedObject, rhsTraitBasedObject, rhsState, objectMap))
                        {
                            objectMap.RevertTraversalChanges();

                            matchFound = false;
                            break;
                        }
                    }

                    if (matchFound)
                        break;
                }

                if (!matchFound)
                {
                    statesEqual = false;
                    break;
                }
            }

            return statesEqual;
        }

        bool ObjectsMatchAttributes(TraitBasedObject traitBasedObjectLHS, TraitBasedObject traitBasedObjectRHS, StateData rhsState)
        {
            if (!traitBasedObjectLHS.HasSameTraits(traitBasedObjectRHS))
                return false;




            if (traitBasedObjectLHS.LocationIndex != TraitBasedObject.Unset
                && !LocationTraitAttributesEqual(LocationBuffer[traitBasedObjectLHS.LocationIndex], rhsState.LocationBuffer[traitBasedObjectRHS.LocationIndex]))
                return false;



            return true;
        }
        
        bool LocationTraitAttributesEqual(Location one, Location two)
        {
            return
                    one.Position == two.Position && 
                    one.Forward == two.Forward;
        }
        
        bool CheckRelationsAndQueueObjects(TraitBasedObject traitBasedObjectLHS, TraitBasedObject traitBasedObjectRHS, StateData rhsState, ObjectCorrespondence objectMap)
        {

            return true;
        }

        public override int GetHashCode()
        {
            // h = 3860031 + (h+y)*2779 + (h*y*2)   // from How to Hash a Set by Richard O’Keefe
            var stateHashValue = 0;

            var objectIds = TraitBasedObjectIds;
            for (int i = 0; i < objectIds.Length; i++)
            {
                var element = objectIds[i];
                var value = element.GetHashCode();
                stateHashValue = 3860031 + (stateHashValue + value) * 2779 + (stateHashValue * value * 2);
            }

            for (int i = 0; i < GoalBuffer.Length; i++)
            {
                var element = GoalBuffer[i];
                var value = 397;
                stateHashValue = 3860031 + (stateHashValue + value) * 2779 + (stateHashValue * value * 2);
            }
            for (int i = 0; i < NpcBuffer.Length; i++)
            {
                var element = NpcBuffer[i];
                var value = 397;
                stateHashValue = 3860031 + (stateHashValue + value) * 2779 + (stateHashValue * value * 2);
            }
            for (int i = 0; i < WayPointBuffer.Length; i++)
            {
                var element = WayPointBuffer[i];
                var value = 397;
                stateHashValue = 3860031 + (stateHashValue + value) * 2779 + (stateHashValue * value * 2);
            }
            for (int i = 0; i < LocationBuffer.Length; i++)
            {
                var element = LocationBuffer[i];
                var value = 397
                    ^ element.Position.GetHashCode()
                    ^ element.Forward.GetHashCode();
                stateHashValue = 3860031 + (stateHashValue + value) * 2779 + (stateHashValue * value * 2);
            }
            for (int i = 0; i < MoveableBuffer.Length; i++)
            {
                var element = MoveableBuffer[i];
                var value = 397;
                stateHashValue = 3860031 + (stateHashValue + value) * 2779 + (stateHashValue * value * 2);
            }

            return stateHashValue;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            for (var traitBasedObjectIndex = 0; traitBasedObjectIndex < TraitBasedObjects.Length; traitBasedObjectIndex++)
            {
                var traitBasedObject = TraitBasedObjects[traitBasedObjectIndex];
                sb.AppendLine(TraitBasedObjectIds[traitBasedObjectIndex].ToString());

                var i = 0;

                var traitIndex = traitBasedObject[i++];
                if (traitIndex != TraitBasedObject.Unset)
                    sb.AppendLine(GoalBuffer[traitIndex].ToString());

                traitIndex = traitBasedObject[i++];
                if (traitIndex != TraitBasedObject.Unset)
                    sb.AppendLine(NpcBuffer[traitIndex].ToString());

                traitIndex = traitBasedObject[i++];
                if (traitIndex != TraitBasedObject.Unset)
                    sb.AppendLine(WayPointBuffer[traitIndex].ToString());

                traitIndex = traitBasedObject[i++];
                if (traitIndex != TraitBasedObject.Unset)
                    sb.AppendLine(LocationBuffer[traitIndex].ToString());

                traitIndex = traitBasedObject[i++];
                if (traitIndex != TraitBasedObject.Unset)
                    sb.AppendLine(MoveableBuffer[traitIndex].ToString());

                sb.AppendLine();
            }

            return sb.ToString();
        }
    }

    public struct StateDataContext : ITraitBasedStateDataContext<TraitBasedObject, StateEntityKey, StateData>
    {
        internal EntityCommandBuffer.Concurrent EntityCommandBuffer;
        internal EntityArchetype m_StateArchetype;
        internal int JobIndex; //todo assign

        [ReadOnly] public BufferFromEntity<TraitBasedObject> TraitBasedObjects;
        [ReadOnly] public BufferFromEntity<TraitBasedObjectId> TraitBasedObjectIds;

        [ReadOnly] public BufferFromEntity<Goal> GoalData;
        [ReadOnly] public BufferFromEntity<Npc> NpcData;
        [ReadOnly] public BufferFromEntity<WayPoint> WayPointData;
        [ReadOnly] public BufferFromEntity<Location> LocationData;
        [ReadOnly] public BufferFromEntity<Moveable> MoveableData;

        public StateDataContext(JobComponentSystem system, EntityArchetype stateArchetype)
        {
            EntityCommandBuffer = default;
            TraitBasedObjects = system.GetBufferFromEntity<TraitBasedObject>(true);
            TraitBasedObjectIds = system.GetBufferFromEntity<TraitBasedObjectId>(true);

            GoalData = system.GetBufferFromEntity<Goal>(true);
            NpcData = system.GetBufferFromEntity<Npc>(true);
            WayPointData = system.GetBufferFromEntity<WayPoint>(true);
            LocationData = system.GetBufferFromEntity<Location>(true);
            MoveableData = system.GetBufferFromEntity<Moveable>(true);

            m_StateArchetype = stateArchetype;
            JobIndex = 0; // todo set on all actions
        }

        public StateData GetStateData(StateEntityKey stateKey)
        {
            var stateEntity = stateKey.Entity;

            return new StateData
            {
                StateEntity = stateEntity,
                TraitBasedObjects = TraitBasedObjects[stateEntity],
                TraitBasedObjectIds = TraitBasedObjectIds[stateEntity],

                GoalBuffer = GoalData[stateEntity],
                NpcBuffer = NpcData[stateEntity],
                WayPointBuffer = WayPointData[stateEntity],
                LocationBuffer = LocationData[stateEntity],
                MoveableBuffer = MoveableData[stateEntity],
            };
        }

        public StateData CopyStateData(StateData stateData)
        {
            return stateData.Copy(JobIndex, EntityCommandBuffer);
        }

        public StateEntityKey GetStateDataKey(StateData stateData)
        {
            return new StateEntityKey { Entity = stateData.StateEntity, HashCode = stateData.GetHashCode()};
        }

        public void DestroyState(StateEntityKey stateKey)
        {
            EntityCommandBuffer.DestroyEntity(JobIndex, stateKey.Entity);
        }

        public StateData CreateStateData()
        {
            return new StateData(JobIndex, EntityCommandBuffer, EntityCommandBuffer.CreateEntity(JobIndex, m_StateArchetype));
        }

        public bool Equals(StateData x, StateData y)
        {
            return x.Equals(y);
        }

        public int GetHashCode(StateData obj)
        {
            return obj.GetHashCode();
        }
    }

    [DisableAutoCreation]
    public class StateManager : JobComponentSystem, ITraitBasedStateManager<TraitBasedObject, StateEntityKey, StateData, StateDataContext>
    {
        public ExclusiveEntityTransaction ExclusiveEntityTransaction;
        public event Action Destroying;

        List<EntityCommandBuffer> m_EntityCommandBuffers;
        EntityArchetype m_StateArchetype;

        protected override void OnCreate()
        {
            m_StateArchetype = EntityManager.CreateArchetype(typeof(State), typeof(TraitBasedObject), typeof(TraitBasedObjectId), typeof(HashCode),
                typeof(Goal),
                typeof(Npc),
                typeof(WayPoint),
                typeof(Location),
                typeof(Moveable));

            BeginEntityExclusivity();
            m_EntityCommandBuffers = new List<EntityCommandBuffer>();
        }

        protected override void OnDestroy()
        {
            Destroying?.Invoke();
            EndEntityExclusivity();
            base.OnDestroy();
        }

        public EntityCommandBuffer GetEntityCommandBuffer()
        {
            var ecb = new EntityCommandBuffer(Allocator.Persistent);
            m_EntityCommandBuffers.Add(ecb);
            return ecb;
        }

        public StateData CreateStateData()
        {
            EndEntityExclusivity();
            var stateEntity = EntityManager.CreateEntity(m_StateArchetype);
            BeginEntityExclusivity();
            return new StateData(this, stateEntity, true);
        }

        public StateData GetStateData(StateEntityKey stateKey, bool readWrite = false)
        {
            return !Enabled ? default : new StateData(this, stateKey.Entity, readWrite);
        }

        public void DestroyState(StateEntityKey stateKey)
        {
            if (EntityManager != null && EntityManager.IsCreated)
            {
                EndEntityExclusivity();
                EntityManager.DestroyEntity(stateKey.Entity);
                BeginEntityExclusivity();
            }
        }

        public StateDataContext GetStateDataContext()
        {
            return new StateDataContext(this, m_StateArchetype);
        }

        public StateEntityKey GetStateDataKey(StateData stateData)
        {
            return new StateEntityKey { Entity = stateData.StateEntity, HashCode = stateData.GetHashCode()};
        }

        public StateData CopyStateData(StateData stateData)
        {
            EndEntityExclusivity();
            var copyStateEntity = EntityManager.Instantiate(stateData.StateEntity);
            BeginEntityExclusivity();
            return new StateData(this, copyStateEntity, true);
        }

        public StateEntityKey CopyState(StateEntityKey stateKey)
        {
            EndEntityExclusivity();
            var copyStateEntity = EntityManager.Instantiate(stateKey.Entity);
            BeginEntityExclusivity();
            var stateData = GetStateData(stateKey);
            return new StateEntityKey { Entity = copyStateEntity, HashCode = stateData.GetHashCode()};
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps) => JobHandle.CombineDependencies(inputDeps, EntityManager.ExclusiveEntityTransactionDependency);

        public bool Equals(StateData x, StateData y)
        {
            return x.Equals(y);
        }

        public int GetHashCode(StateData obj)
        {
            return obj.GetHashCode();
        }

        void BeginEntityExclusivity()
        {
            ExclusiveEntityTransaction = EntityManager.BeginExclusiveEntityTransaction();
        }

        void EndEntityExclusivity()
        {
            EntityManager.EndExclusiveEntityTransaction();

            foreach (var ecb in m_EntityCommandBuffers)
            {
                if (ecb.IsCreated)
                    ecb.Dispose();
            }
            m_EntityCommandBuffers.Clear();
        }
    }

    struct DestroyStatesJobScheduler : IDestroyStatesScheduler<StateEntityKey, StateData, StateDataContext, StateManager>
    {
        public StateManager StateManager { private get; set; }
        public NativeQueue<StateEntityKey> StatesToDestroy { private get; set; }

        public JobHandle Schedule(JobHandle inputDeps)
        {
            var entityManager = StateManager.EntityManager;
            inputDeps = JobHandle.CombineDependencies(inputDeps, entityManager.ExclusiveEntityTransactionDependency);

            var stateDataContext = StateManager.GetStateDataContext();
            var ecb = StateManager.GetEntityCommandBuffer();
            stateDataContext.EntityCommandBuffer = ecb.ToConcurrent();
            var destroyStatesJobHandle = new DestroyStatesJob<StateEntityKey, StateData, StateDataContext>()
            {
                StateDataContext = stateDataContext,
                StatesToDestroy = StatesToDestroy
            }.Schedule(inputDeps);

            var playbackECBJobHandle = new PlaybackSingleECBJob()
            {
                ExclusiveEntityTransaction = StateManager.ExclusiveEntityTransaction,
                EntityCommandBuffer = ecb
            }.Schedule(destroyStatesJobHandle);

            entityManager.ExclusiveEntityTransactionDependency = playbackECBJobHandle;
            return playbackECBJobHandle;
        }
    }
}
