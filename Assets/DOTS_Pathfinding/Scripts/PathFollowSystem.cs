using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Jobs;

<<<<<<< Updated upstream
public class PathFollowSystem : JobComponentSystem {
=======
[UpdateAfter(typeof(CheckEntityPositions))]
public class PathFollowSystem : SystemBase {
>>>>>>> Stashed changes

    private Unity.Mathematics.Random random;

    protected override void OnCreate() {
        random = new Unity.Mathematics.Random(56);
<<<<<<< Updated upstream
=======
    }

    public static int GetUniqueKeyForPosition(float3 position, float cellSize) {
        return (int)(19 * math.floor(position.x / cellSize) + (17 * math.floor(position.y / cellSize)));
>>>>>>> Stashed changes
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps) {
        float deltaTime = Time.DeltaTime;

<<<<<<< Updated upstream
        return Entities.ForEach((Entity entity, DynamicBuffer<PathPosition> pathPositionBuffer, ref Translation translation, ref PathFollow pathFollow) => {
            if (pathFollow.pathIndex >= 0) {
                // Has path to follow
                PathPosition pathPosition = pathPositionBuffer[pathFollow.pathIndex];

                float3 targetPosition = new float3(pathPosition.position.x, pathPosition.position.y, 0);
                float3 moveDir = math.normalizesafe(targetPosition - translation.Value);
                float moveSpeed = 20f;

                translation.Value += moveDir * moveSpeed * deltaTime;
=======
        String seconds = DateTime.Now.ToString("ss");
        int timeElapsed = int.Parse(seconds[1].ToString());

        NativeMultiHashMap<int, float3> cellVsEntityPositionsForJob = CheckEntityPositions.cellVsEntityPositions;

        Entities
            .WithReadOnly(cellVsEntityPositionsForJob)
            .ForEach((Entity entity, DynamicBuffer<PathPosition> pathPositionBuffer, ref Translation translation, ref PathFollow pathFollow) => {
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

                    if (pathPosition.type > 7 && ((pathPosition.type == 8 && moveDir.y < -0.5f && timeElapsed >= 5) || (pathPosition.type == 9 && moveDir.x > 0.5f && timeElapsed < 5)|| (pathPosition.type == 10 && moveDir.x < -0.5f && timeElapsed < 5)|| (pathPosition.type == 11 && moveDir.y > 0.5f && timeElapsed >= 5)))
                    {}
                    else {
                        if (cellVsEntityPositionsForJob.TryGetFirstValue(indexTarget, out positionToCheck, out mapIterator)) {
                            do {

                                if (!translation.Value.Equals(positionToCheck)) {

                                    float3 checkDir = math.normalizesafe(positionToCheck - translation.Value);

                                    if (math.length(moveDir - checkDir) > 0.5) {
                                        continue;
                                    }
                                    /*if (math.length(translation.Value * moveDir - positionToCheck * moveDir) < 0) {
                                        continue;
                                    }*/
                                    if (currentDistance > math.sqrt(math.lengthsq(translation.Value - positionToCheck))) {
                                        countCollisions++;
                                    }
                                    
                                }

                            } while (cellVsEntityPositionsForJob.TryGetNextValue(out positionToCheck, ref mapIterator));
                        }

                        if (countCollisions == 0) {
                            translation.Value += moveDir * moveSpeed * deltaTime;
                        }
                    }
                
>>>>>>> Stashed changes
                
                if (math.distance(translation.Value, targetPosition) < .1f) {
                    // Next waypoint
                    pathFollow.pathIndex--;
                }
            }
        }).Schedule(inputDeps);
    }

    private void ValidateGridPosition(ref int x, ref int y) {
        x = math.clamp(x, 0, PathfindingGridSetup.Instance.pathfindingGrid.GetWidth() - 1);
        y = math.clamp(y, 0, PathfindingGridSetup.Instance.pathfindingGrid.GetHeight() - 1);
    }

}

[UpdateAfter(typeof(PathFollowSystem))]
[DisableAutoCreation]
public class PathFollowGetNewPathSystem : JobComponentSystem {
    
    private Unity.Mathematics.Random random;
<<<<<<< Updated upstream

=======
>>>>>>> Stashed changes
    private EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBufferSystem;

    protected override void OnCreate() {
        random = new Unity.Mathematics.Random(56);

        endSimulationEntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
<<<<<<< Updated upstream
=======
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
>>>>>>> Stashed changes
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps) {
        int mapWidth = PathfindingGridSetup.Instance.pathfindingGrid.GetWidth();
        int mapHeight = PathfindingGridSetup.Instance.pathfindingGrid.GetHeight();
<<<<<<< Updated upstream
=======
        NativeList<Vector3> busStops = PathfindingGridSetup.Instance.pathfindingGrid.GetBusStops();

>>>>>>> Stashed changes
        float3 originPosition = float3.zero;
        float cellSize = PathfindingGridSetup.Instance.pathfindingGrid.GetCellSize();
        Unity.Mathematics.Random random = new Unity.Mathematics.Random(this.random.NextUInt(1, 10000));

        EntityCommandBuffer.ParallelWriter entityCommandBuffer = endSimulationEntityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();
<<<<<<< Updated upstream

        JobHandle jobHandle = Entities.WithNone<PathfindingParams>().ForEach((Entity entity, int entityInQueryIndex, in PathFollow pathFollow, in Translation translation) => { 
            if (pathFollow.pathIndex == -1) {
=======

        Entities
            .WithReadOnly(busStops)
            .WithNone<PathfindingParams>()
            .WithAll<BusComponent>()
            .ForEach((Entity entity, int entityInQueryIndex, in PathFollow pathFollow, in Translation translation) => {
                if (pathFollow.pathIndex == -1)
                {

                    GetXY(translation.Value + new float3(1, 1, 0) * cellSize * +.5f, originPosition, cellSize, out int startX, out int startY);

                    ValidateGridPosition(ref startX, ref startY, mapWidth, mapHeight);
               
                    int busStop = random.NextInt(0, busStops.Length - 1);
                    int endX = (int)busStops[busStop].x;
                    int endY = (int)busStops[busStop].y;

                    entityCommandBuffer.AddComponent(entityInQueryIndex, entity, new PathfindingParams
                    {
                        startPosition = new int2(startX, startY),
                        endPosition = new int2(endX, endY)
                    });
                }
            }).ScheduleParallel();

        Entities
            .WithNone<PathfindingParams,BusComponent>()
            .ForEach((Entity entity, int entityInQueryIndex, in PathFollow pathFollow, in Translation translation) => { 
                if (pathFollow.pathIndex == -1) {
>>>>>>> Stashed changes
                
                    GetXY(translation.Value + new float3(1, 1, 0) * cellSize * +.5f, originPosition, cellSize, out int startX, out int startY);

<<<<<<< Updated upstream
                ValidateGridPosition(ref startX, ref startY, mapWidth, mapHeight);

                int endX = random.NextInt(0, mapWidth);
                int endY = random.NextInt(0, mapHeight);

                entityCommandBuffer.AddComponent(entityInQueryIndex, entity, new PathfindingParams { 
                    startPosition = new int2(startX, startY), endPosition = new int2(endX, endY) 
                });
            }
        }).Schedule(inputDeps);

        endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(jobHandle);

        return jobHandle;
=======
                    ValidateGridPosition(ref startX, ref startY, mapWidth, mapHeight);
                    int endX = random.NextInt(0, mapWidth);
                    int endY = random.NextInt(0, mapHeight);

                    entityCommandBuffer.AddComponent(entityInQueryIndex, entity, new PathfindingParams { 
                        startPosition = new int2(startX, startY), endPosition = new int2(endX, endY) 
                    });
                }
            }).ScheduleParallel();

        
        endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(this.Dependency);
>>>>>>> Stashed changes
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
