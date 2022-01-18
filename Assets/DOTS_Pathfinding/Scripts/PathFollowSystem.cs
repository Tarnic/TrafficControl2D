using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Jobs;
using System;
using Unity.Collections;
using Random = Unity.Mathematics.Random;
public struct PositionInfo
{
    public float3 currentPosition;
    public float3 nextPosition;
    public Entity entity;

}

public class CheckEntityPositions : SystemBase
{
    public static NativeHashMap<int, PositionInfo> cellVsEntityCustom;
    protected override void OnCreate()
    {
        cellVsEntityCustom = new NativeHashMap<int, PositionInfo>(0, Allocator.Persistent);
    }
    public static int GetKeyFromPosition(float3 position, float cellSize, int width)
    {
        return (int)(math.floor(position.x / cellSize) + (width * math.floor(position.y / cellSize)));
    }

    protected override void OnUpdate()
    {
        float deltaTime = Time.DeltaTime;
        float cellSize = PathfindingGridSetup.Instance.pathfindingGrid.GetCellSize();
        int width = PathfindingGridSetup.Instance.pathfindingGrid.GetWidth();

        EntityQuery eq = GetEntityQuery(typeof(PathFollow));
        cellVsEntityCustom.Clear();
        if (eq.CalculateEntityCount() > cellVsEntityCustom.Capacity)
        {
            cellVsEntityCustom.Capacity = eq.CalculateEntityCount();
        }

        NativeHashMap<int, PositionInfo>.ParallelWriter cellEntityCustomParallel = cellVsEntityCustom.AsParallelWriter();

        Entities.ForEach((Entity entity, DynamicBuffer <PathPosition> pathPositionBuffer, ref Translation translation, ref PathFollow pathFollow) =>
        {
            float3 targetPosition = translation.Value;

            if (pathFollow.pathIndex != -1)
            {
                PathPosition pathPosition = pathPositionBuffer[pathFollow.pathIndex];
                targetPosition = new float3(pathPosition.position.x, pathPosition.position.y, 0);
            }

            PositionInfo positionInfo = new PositionInfo { currentPosition = translation.Value, nextPosition = targetPosition, entity = entity };
            cellEntityCustomParallel.TryAdd(GetKeyFromPosition(translation.Value + new float3(0.5f, 0.5f, 0f), cellSize, width), positionInfo);

        }).ScheduleParallel();

    }

    protected override void OnDestroy()
    {
        cellVsEntityCustom.Dispose();
    }
}

[UpdateAfter(typeof(CheckEntityPositions))]
public class PathFollowSystem : SystemBase {
    private Random random;

    protected override void OnCreate() {
        random = new Random(56);
    }

    public static int GetKeyFromPosition(float3 position, float cellSize, int width)
    {
        return (int)(math.floor(position.x / cellSize) + (width * math.floor(position.y / cellSize)));
    }

    public static int GetUniqueKeyForPosition(float3 position, float cellSize) {
        return (int)(19 * math.floor(position.x / cellSize) + (17 * math.floor(position.y / cellSize)));
    }

    protected override void OnUpdate() {
        float deltaTime = Time.DeltaTime;
        float cellSize = PathfindingGridSetup.Instance.pathfindingGrid.GetCellSize();
        int width = PathfindingGridSetup.Instance.pathfindingGrid.GetWidth();

        String seconds = DateTime.Now.ToString("ss");
        bool flagV = SemaphoreColorSystem.flagV;
        bool flagH = SemaphoreColorSystem.flagH;
        //int timeElapsed = int.Parse(seconds[1].ToString());
        bool collisions = PathfindingGridSetup.GetCollisions();
        NativeHashMap<int, PositionInfo> cellVsEntityPositionsForJob = CheckEntityPositions.cellVsEntityCustom;

        Entities
            .WithReadOnly(cellVsEntityPositionsForJob).WithNone<ParkingTimerComponent>()
            .ForEach((Entity entity, DynamicBuffer<PathPosition> pathPositionBuffer, ref Translation translation, ref PathFollow pathFollow) => {
                if (pathFollow.pathIndex >= 0) {
                    // Has path to follow
                    PathPosition pathPosition = pathPositionBuffer[pathFollow.pathIndex];
                    //PathPosition futurePosition;
                    float3 targetPosition = new float3(pathPosition.position.x, pathPosition.position.y, 0);
                    float3 moveDir = math.normalizesafe(targetPosition - translation.Value);
                    float moveSpeed = 2f;
                    bool checkPrec = false;

                    if (pathPosition.type > 10)
                    {
                        // calculate the remaining 3 position in the cross road if the car is just entering the cross road.
                        // if there are 3 positions taken in the cross road then the car won't move because it is highly probable that it would stuck inside the cross road and to create a stall.
                        float3 posA = new Vector3(0, 0, 0);
                        float3 posB = new Vector3(0, 0, 0);
                        float3 posC = new Vector3(0, 0, 0); 
                        if (moveDir.y > 0.5f && pathPosition.type == 14)
                        {
                            posA = targetPosition + new float3(0, 1f, 0);
                            posB = targetPosition + new float3(-1f, 1f, 0);
                            posC = targetPosition + new float3(-1f, 0, 0);
                            checkPrec = true;
                        }
                        else if (moveDir.y < -0.5f && pathPosition.type == 11)
                        {
                            posA = targetPosition + new float3(1f, 0, 0);
                            posB = targetPosition + new float3(0, -1f, 0);
                            posC = targetPosition + new float3(1f, -1f, 0);
                            checkPrec = true;
                        }
                        else if (moveDir.x > 0.5f && pathPosition.type == 12)
                        {
                            posA = targetPosition + new float3(0, 1f, 0);
                            posB = targetPosition + new float3(1f, 1f, 0);
                            posC = targetPosition + new float3(1f, 0, 0);
                            checkPrec = true;
                        }
                        else if (moveDir.x < -0.5f && pathPosition.type == 13)
                        {
                            posA = targetPosition + new float3(0, -1f, 0);
                            posB = targetPosition + new float3(-1f, 0, 0);
                            posC = targetPosition + new float3(-1f, -1f, 0);
                            checkPrec = true;
                        }

                        if (checkPrec)
                        {
                            PositionInfo tmp;
                            if (cellVsEntityPositionsForJob.TryGetValue(GetKeyFromPosition(posA + new float3(0.5f, 0.5f, 0f), cellSize, width), out tmp) &&
                                cellVsEntityPositionsForJob.TryGetValue(GetKeyFromPosition(posB + new float3(0.5f, 0.5f, 0f), cellSize, width), out tmp) &&
                                cellVsEntityPositionsForJob.TryGetValue(GetKeyFromPosition(posC + new float3(0.5f, 0.5f, 0f), cellSize, width), out tmp))
                            {
                               
                            }
                            else
                            {
                                checkPrec = false;
                            }
                        }
                        
                        
                    }


                    PositionInfo position;
                    float currentDistance = 2f;
                    int indexCustom = GetKeyFromPosition(targetPosition + new float3(0.5f, 0.5f, 0f), cellSize, width);
                    int countCollisions = 0;

                    // Check Traffic Lights, if the car is going to a cross road and the light is red, then the car should not move.
                    if (pathPosition.type > 10 
                        && math.distance(translation.Value, targetPosition) > .9f
                        && ((pathPosition.type == 11 && moveDir.y < -0.5f && !flagV) // CrossLeftUp
                        || (pathPosition.type == 12 && moveDir.x > 0.5f && !flagH)    // CrossLeftDown
                        || (pathPosition.type == 13 && moveDir.x < -0.5f && !flagH)   // CrossRightUp
                        || (pathPosition.type == 14 && moveDir.y > 0.5f && !flagV))) // CrossRightDown
                    { }
                    else {
                        if (cellVsEntityPositionsForJob.TryGetValue(indexCustom, out position)){
                            float3 positionToCheck = position.currentPosition;

                            if (math.sqrt(math.lengthsq(translation.Value - positionToCheck)) > 0.1f && currentDistance > math.sqrt(math.lengthsq(translation.Value - positionToCheck)))
                            {
                                countCollisions = 1;
                            }
                            else
                            {


                                if (math.sqrt(math.lengthsq(translation.Value - positionToCheck)) > 0.1f && math.sqrt(math.lengthsq(translation.Value - positionToCheck)) < 2.5f)
                                {
                                    //slow down when getting closer to another car
                                    moveSpeed = moveSpeed / 8;
                                }
                            }
                        }
                        if (countCollisions == 0 && !checkPrec) {
                            translation.Value += moveDir * moveSpeed * deltaTime;
                        }
                        else
                        {
                            if(pathPosition.type == 10)
                            {
                                pathFollow.pathIndex = -1;
                            }
                            if (!collisions)
                            {
                                translation.Value += moveDir * moveSpeed * deltaTime;
                            }
                        }
                    }

                
                
                    if (math.distance(translation.Value, targetPosition) < .1f && pathFollow.pathIndex != -1) {
                        // Next waypoint
                        pathFollow.pathIndex--;
                    }
                }
        }).ScheduleParallel();
    }

    private void ValidateGridPosition(ref int x, ref int y) {
        x = math.clamp(x, 0, PathfindingGridSetup.Instance.pathfindingGrid.GetWidth() - 1);
        y = math.clamp(y, 0, PathfindingGridSetup.Instance.pathfindingGrid.GetHeight() - 1);
    }

}



//[DisableAutoCreation]
[UpdateAfter(typeof(PathFollowSystem))]
public class PathFollowGetNewPathSystem : SystemBase {

    private Random random;
    private EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBufferSystem;

    protected override void OnCreate() {
        random = new Random();
        endSimulationEntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate() {
        NativeList<Vector3> busStops = PathfindingGridSetup.Instance.pathfindingGrid.GetBusStops();
        NativeList<Vector3> validPositions = PathfindingGridSetup.Instance.pathfindingGrid.GetValidPositions();
        //Random random = new Unity.Mathematics.Random(this.random.NextUInt(1, 10000));

        float cellSize = PathfindingGridSetup.Instance.pathfindingGrid.GetCellSize();
        int mapWidth = PathfindingGridSetup.Instance.pathfindingGrid.GetWidth();
        int mapHeight = PathfindingGridSetup.Instance.pathfindingGrid.GetHeight();

        EntityCommandBuffer.ParallelWriter entityCommandBuffer = endSimulationEntityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();

        // whenever a bus arrives to its target position, then a new busStop to reach is assigned.
        Entities
            .WithoutBurst()
            .WithReadOnly(busStops)
            .WithNone<PathfindingParams, ParkingTimerComponent>()
            .WithAll<BusComponent>()
            .ForEach((Entity entity, int entityInQueryIndex, in PathFollow pathFollow, in Translation translation) => {
                if (pathFollow.pathIndex == -1)
                {
                    Random random = Random.CreateFromIndex((uint) entityInQueryIndex+1);
                    AssignNewParams(entity, translation, busStops, cellSize, mapWidth, mapHeight, true, random, out int startX, out int startY, out int endX, out int endY);

                    GridNode gridNode = PathfindingGridSetup.Instance.pathfindingGrid.GetGridObject(startX, startY);

                    if (gridNode.GetType() == 5)
                    {
                        entityCommandBuffer.AddComponent(entityInQueryIndex, entity, new ParkingTimerComponent { timeOfDeparture = DateTime.Now.AddSeconds(random.NextInt(5, 200)) });
                    }

                    entityCommandBuffer.AddComponent(entityInQueryIndex, entity, new PathfindingParams
                    {
                        startPosition = new int2(startX, startY),
                        endPosition = new int2(endX, endY)
                    });
                }
            }).ScheduleParallel();

        Entities
            .WithoutBurst()
            .WithReadOnly(validPositions)
            .WithNone<PathfindingParams, BusComponent, ParkingTimerComponent>()
            .ForEach((Entity entity, int entityInQueryIndex, in PathFollow pathFollow, in Translation translation) => { 
                if (pathFollow.pathIndex == -1) {

                    Random random = Random.CreateFromIndex((uint)entityInQueryIndex+1);
                    AssignNewParams(entity, translation, validPositions, cellSize, mapWidth, mapHeight, false, random, out int startX, out int startY, out int endX, out int endY);

                    GridNode gridNode = PathfindingGridSetup.Instance.pathfindingGrid.GetGridObject(startX, startY);
                    if (gridNode.GetType() == 10)
                    {
                        entityCommandBuffer.AddComponent(entityInQueryIndex, entity, new ParkingTimerComponent { timeOfDeparture = DateTime.Now.AddSeconds(random.NextInt(10, 200)) });
                    }

                    entityCommandBuffer.AddComponent(entityInQueryIndex, entity, new PathfindingParams { 
                        startPosition = new int2(startX, startY), endPosition = new int2(endX, endY) 
                    });
                }
            }).ScheduleParallel();

        endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(this.Dependency);
    }

    public static void AssignNewParams(Entity entity, Translation translation, NativeList<Vector3> positions, float cellSize, int mapWidth, int mapHeight, bool isBus, Random random, out int startX, out int startY, out int endX, out int endY)
    {
        GetXY(translation.Value + new float3(1, 1, 0) * cellSize * +.5f, float3.zero, cellSize, out startX, out startY);
        ValidateGridPosition(ref startX, ref startY, mapWidth, mapHeight);

        if (isBus)
        {
            int busStop = random.NextInt(0, positions.Length);
            endX = (int)positions[busStop].x;
            endY = (int)positions[busStop].y;

        }
        else
        {
            int position = random.NextInt(0, positions.Length);
            endX = (int)positions[position].x;
            endY = (int)positions[position].y;
        }

    }

    private static void ValidateGridPosition(ref int x, ref int y, int width, int height) {
        x = math.clamp(x, 0, width - 1);
        y = math.clamp(y, 0, height - 1);
    }

    private static void GetXY(float3 worldPosition, float3 originPosition, float cellSize, out int x, out int y) {
        x = (int)math.floor((worldPosition - originPosition).x / cellSize);
        y = (int)math.floor((worldPosition - originPosition).y / cellSize);
    }

}
