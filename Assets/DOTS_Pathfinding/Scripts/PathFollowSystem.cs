using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Jobs;
using System.Collections.Generic;
using System;
using Unity.Collections;

public class PathFollowSystem : SystemBase {
    private static NativeMultiHashMap<int, float3> cellVsEntityPositions;
    private Unity.Mathematics.Random random;

    protected override void OnCreate() {
        random = new Unity.Mathematics.Random(56);
        cellVsEntityPositions = new NativeMultiHashMap<int, float3>(0, Allocator.Persistent);
    }

    public static int GetUniqueKeyForPosition(float3 position, float cellSize)
    {
        return (int)(19 * math.floor(position.x / cellSize) + (17 * math.floor(position.y / cellSize)));
    }

    protected override void OnUpdate() {
        float deltaTime = Time.DeltaTime;
        float cellSize = PathfindingGridSetup.Instance.pathfindingGrid.GetCellSize();

        EntityQuery eq = GetEntityQuery(typeof(PathFollow));
        cellVsEntityPositions.Clear();
        if (eq.CalculateEntityCount() > cellVsEntityPositions.Capacity)
        {
            cellVsEntityPositions.Capacity = eq.CalculateEntityCount();
        }

        NativeMultiHashMap<int, float3>.ParallelWriter cellEntityPositionParallel = cellVsEntityPositions.AsParallelWriter();
        String seconds = DateTime.Now.ToString("ss");
        int timeElapsed = int.Parse(seconds[1].ToString());
        //int timeElapsed = int.Parse((DateTime.Now.ToString("ss"))[1]);

        Entities.ForEach((ref Translation translation, ref PathFollow pathFollow) => {
            cellEntityPositionParallel.Add(GetUniqueKeyForPosition(translation.Value, cellSize), translation.Value);
        }).ScheduleParallel();

        NativeMultiHashMap<int, float3> cellVsEntityPositionsForJob = cellVsEntityPositions;

        Entities.ForEach((Entity entity, DynamicBuffer<PathPosition> pathPositionBuffer, ref Translation translation, ref PathFollow pathFollow) => {
            if (pathFollow.pathIndex >= 0) {
                // Has path to follow
                PathPosition pathPosition = pathPositionBuffer[pathFollow.pathIndex];
                float3 targetPosition = new float3(pathPosition.position.x, pathPosition.position.y, 0);
                float3 moveDir = math.normalizesafe(targetPosition - translation.Value);
                float moveSpeed = 10f;

                float3 positionToCheck;
                float currentDistance = 1f;
                NativeMultiHashMapIterator<int> mapIterator;
                int indexTarget = GetUniqueKeyForPosition(targetPosition, cellSize);
                int countCollisions = 0;

                if (pathPosition.type > 7 && ((pathPosition.type == 8 && moveDir.y < -0.5f && timeElapsed >= 5)|| (pathPosition.type == 9 && moveDir.x > 0.5f && timeElapsed < 5)|| (pathPosition.type == 10 && moveDir.x < -0.5f && timeElapsed < 5)|| (pathPosition.type == 11 && moveDir.y > 0.5f && timeElapsed >= 5)))
                {}
                else
                {
                    /*if (cellVsEntityPositionsForJob.TryGetFirstValue(indexTarget, out positionToCheck, out mapIterator))
                    {
                        do
                        {

                            if (!translation.Value.Equals(positionToCheck))
                            {

                                if (!math.normalizesafe(positionToCheck - translation.Value).Equals(moveDir))
                                {
                                    continue;
                                }
                                if (math.length(translation.Value * moveDir - positionToCheck * moveDir) < 0)
                                {
                                    continue;
                                }
                                else if (currentDistance > math.sqrt(math.lengthsq(translation.Value - positionToCheck)))
                                {
                                    countCollisions++;
                                }
                                else
                                {
                                    continue;
                                }
                            }

                        } while (cellVsEntityPositionsForJob.TryGetNextValue(out positionToCheck, ref mapIterator));
                    }

                    if (countCollisions == 0)
                    {
                        translation.Value += moveDir * moveSpeed * deltaTime;
                    }*/
                    translation.Value += moveDir * moveSpeed * deltaTime;
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

        cellVsEntityPositions.Dispose();
    }

}



//[DisableAutoCreation]
[UpdateAfter(typeof(PathFollowSystem))]
public class PathFollowGetNewPathSystem : SystemBase {

    private Unity.Mathematics.Random random;
    //private EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBufferSystem;

    protected override void OnCreate() {
        random = new Unity.Mathematics.Random(56);

        //endSimulationEntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate() {
        int mapWidth = PathfindingGridSetup.Instance.pathfindingGrid.GetWidth();
        int mapHeight = PathfindingGridSetup.Instance.pathfindingGrid.GetHeight();
        List<Vector3> busStops = PathfindingGridSetup.Instance.pathfindingGrid.GetBusStops();
        Debug.Log("BUSSSSSS");
        if(busStops == null)
        {
            Debug.Log("NULL");
        }
        else
        {
            Debug.Log(busStops.Capacity);
        }
        float3 originPosition = float3.zero;
        float cellSize = PathfindingGridSetup.Instance.pathfindingGrid.GetCellSize();
        Unity.Mathematics.Random random = new Unity.Mathematics.Random(this.random.NextUInt(1, 10000));

        //EntityCommandBuffer.ParallelWriter entityCommandBuffer = endSimulationEntityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();

        Entities.WithStructuralChanges().WithNone<PathfindingParams>().WithAll<BusComponent>().ForEach((Entity entity, int entityInQueryIndex, in PathFollow pathFollow, in Translation translation) => {
            if (pathFollow.pathIndex == -1)
            {

                GetXY(translation.Value + new float3(1, 1, 0) * cellSize * +.5f, originPosition, cellSize, out int startX, out int startY);

                ValidateGridPosition(ref startX, ref startY, mapWidth, mapHeight);
                

                Vector3 busPos = busStops[random.NextInt(0, busStops.Capacity)];
                int busStop = random.NextInt(0, busStops.Capacity);
                int endX = (int)busStops[busStop].x;
                int endY = (int)busStops[busStop].y;

                EntityManager.AddComponentData(entity, new PathfindingParams
                {
                    startPosition = new int2(startX, startY),
                    endPosition = new int2(endX, endY)
                });
            }
        }).Run();

        Entities.WithStructuralChanges().WithNone<PathfindingParams,BusComponent>().ForEach((Entity entity, int entityInQueryIndex, in PathFollow pathFollow, in Translation translation) => { 
            if (pathFollow.pathIndex == -1) {
                
                GetXY(translation.Value + new float3(1, 1, 0) * cellSize * +.5f, originPosition, cellSize, out int startX, out int startY);

                ValidateGridPosition(ref startX, ref startY, mapWidth, mapHeight);
                int endX = random.NextInt(0, mapWidth);
                int endY = random.NextInt(0, mapHeight);

                EntityManager.AddComponentData(entity, new PathfindingParams { 
                    startPosition = new int2(startX, startY), endPosition = new int2(endX, endY) 
                });
            }
        }).Run();

        //endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(jobHandle);
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
