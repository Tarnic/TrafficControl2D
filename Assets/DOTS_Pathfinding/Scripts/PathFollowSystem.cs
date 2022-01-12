using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Jobs;
using System.Collections.Generic;
using System;
using Unity.Collections;
using Unity.Burst;

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

        Entities.ForEach((Entity entity,DynamicBuffer < PathPosition > pathPositionBuffer, ref Translation translation, ref PathFollow pathFollow) =>
        {
            PathPosition pathPosition = pathPositionBuffer[pathFollow.pathIndex];
            float3 targetPosition = new float3(pathPosition.position.x, pathPosition.position.y, 0);
            PositionInfo positionInfo = new PositionInfo{ currentPosition = translation.Value, nextPosition = targetPosition, entity = entity };
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
    //private static NativeMultiHashMap<int, float3> cellVsEntityPositions;
    private Unity.Mathematics.Random random;

    protected override void OnCreate() {
        random = new Unity.Mathematics.Random(56);
        //cellVsEntityPositions = new NativeMultiHashMap<int, float3>(0, Allocator.Persistent);
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
        int timeElapsed = int.Parse(seconds[1].ToString());

        NativeHashMap<int, PositionInfo> cellVsEntityPositionsForJob = CheckEntityPositions.cellVsEntityCustom;

        Entities
            .WithReadOnly(cellVsEntityPositionsForJob).WithNone<ParkingTimerComponent>()
            .ForEach((Entity entity, DynamicBuffer<PathPosition> pathPositionBuffer, ref Translation translation, ref PathFollow pathFollow) => {
                if (pathFollow.pathIndex >= 0) {
                    // Has path to follow
                    PathPosition pathPosition = pathPositionBuffer[pathFollow.pathIndex];
                    float3 targetPosition = new float3(pathPosition.position.x, pathPosition.position.y, 0);
                    float3 precedencePosition = targetPosition;
                    float3 moveDir = math.normalizesafe(targetPosition - translation.Value);
                    float moveSpeed = 7f;

                    if (moveDir.y > 0.5f)
                    {
                        precedencePosition += new float3(1f, 0, 0);
                    }
                    else if (moveDir.y < -0.5f)
                    {
                        precedencePosition += new float3(-1f, 0, 0);
                    }
                    else if (moveDir.x > 0.5f)
                    {
                        precedencePosition += new float3(0, -1f, 0);
                    }
                    else if (moveDir.x < -0.5f)
                    {
                        precedencePosition += new float3(0, 1f, 0);
                    }


                    PositionInfo position;
                    float currentDistance = 1f;
                    int indexCustom = GetKeyFromPosition(targetPosition + new float3(0.5f, 0.5f, 0f), cellSize, width);
                    int indexRight = GetKeyFromPosition(precedencePosition + new float3(0.5f, 0.5f, 0f), cellSize, width);
                    int countCollisions = 0;

                    if (pathPosition.type > 8 && math.distance(translation.Value, targetPosition) > .9f && ((pathPosition.type == 9 && moveDir.y < -0.5f && timeElapsed >= 5) || (pathPosition.type == 10 && moveDir.x > 0.5f && timeElapsed < 5)|| (pathPosition.type == 11 && moveDir.x < -0.5f && timeElapsed < 5)|| (pathPosition.type == 12 && moveDir.y > 0.5f && timeElapsed >= 5)))
                    {}
                    else {
                        if (cellVsEntityPositionsForJob.TryGetValue(indexCustom, out position)){
                            float3 positionToCheck = position.currentPosition;
                            if (math.sqrt(math.lengthsq(translation.Value - positionToCheck)) > 0.1f && currentDistance > math.sqrt(math.lengthsq(translation.Value - positionToCheck)))
                            {
                                countCollisions = 1;
                            }
                            else
                            {
                                if (math.sqrt(math.lengthsq(translation.Value - positionToCheck)) > 0.1f && math.sqrt(math.lengthsq(translation.Value - positionToCheck)) < 1.5f)
                                {
                                    moveSpeed = moveSpeed / 8;
                                }
                            }
                        }
                        if (countCollisions == 0) {
                            translation.Value += moveDir * moveSpeed * deltaTime;
                        }
                    }
                
                
                    if (math.distance(translation.Value, targetPosition) < .1f) {
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

    protected override void OnDestroy()
    {
        //cellVsEntityPositions.Dispose();
    }

}



//[DisableAutoCreation]
[UpdateAfter(typeof(PathFollowSystem))]
public class PathFollowGetNewPathSystem : SystemBase {

    private Unity.Mathematics.Random random;
    private EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBufferSystem;

    protected override void OnCreate() {
        random = new Unity.Mathematics.Random(56);
        
        endSimulationEntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate() {
        int mapWidth = PathfindingGridSetup.Instance.pathfindingGrid.GetWidth();
        int mapHeight = PathfindingGridSetup.Instance.pathfindingGrid.GetHeight();
        NativeList<Vector3> busStops = PathfindingGridSetup.Instance.pathfindingGrid.GetBusStops();
        

        float3 originPosition = float3.zero;
        float cellSize = PathfindingGridSetup.Instance.pathfindingGrid.GetCellSize();
        Unity.Mathematics.Random random = new Unity.Mathematics.Random(this.random.NextUInt(1, 10000));

        EntityCommandBuffer.ParallelWriter entityCommandBuffer = endSimulationEntityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();


        Entities.WithReadOnly(busStops).WithNone<PathfindingParams, ParkingTimerComponent>().WithAll<BusComponent>().ForEach((Entity entity, int entityInQueryIndex, in PathFollow pathFollow, in Translation translation) => {
            if (pathFollow.pathIndex == -1)
            {
                GetXY(translation.Value + new float3(1, 1, 0) * cellSize * +.5f, originPosition, cellSize, out int startX, out int startY);
                ValidateGridPosition(ref startX, ref startY, mapWidth, mapHeight);
                

                int busStop = random.NextInt(0, busStops.Length);
                int endX = (int)busStops[busStop].x;
                int endY = (int)busStops[busStop].y;
                //Debug.Log(busStop);
                entityCommandBuffer.AddComponent(entityInQueryIndex, entity, new PathfindingParams
                {
                    startPosition = new int2(startX, startY),
                    endPosition = new int2(endX, endY)
                });
            }
            

        }).ScheduleParallel();

        Entities.WithNone<PathfindingParams,BusComponent, ParkingTimerComponent>().ForEach((Entity entity, int entityInQueryIndex, in PathFollow pathFollow, in Translation translation) => { 
            if (pathFollow.pathIndex == -1) {
                
                GetXY(translation.Value + new float3(1, 1, 0) * cellSize * +.5f, originPosition, cellSize, out int startX, out int startY);

                ValidateGridPosition(ref startX, ref startY, mapWidth, mapHeight);
                int endX = random.NextInt(0, mapWidth);
                int endY = random.NextInt(0, mapHeight);

                entityCommandBuffer.AddComponent(entityInQueryIndex, entity, new PathfindingParams { 
                    startPosition = new int2(startX, startY), endPosition = new int2(endX, endY) 
                });
            }
        }).ScheduleParallel();

        endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(this.Dependency);
    }

    protected override void OnDestroy()
    {
       
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
