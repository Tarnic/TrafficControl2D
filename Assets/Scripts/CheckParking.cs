using Unity.Entities;
using System;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;
using UnityEngine;

public class CheckParking : SystemBase 
{
    private Unity.Mathematics.Random random;
    private float timeRemaining = 2;
    private BeginInitializationEntityCommandBufferSystem commandBufferSystem;

    protected override void OnCreate()
    {
        commandBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
        random = new Unity.Mathematics.Random(56);
    }

    protected override void OnUpdate() {

        EntityCommandBuffer.ParallelWriter entityCommandBuffer = commandBufferSystem.CreateCommandBuffer().AsParallelWriter();
        NativeList<Vector3> busStops = PathfindingGridSetup.Instance.pathfindingGrid.GetBusStops();
        NativeList<Vector3> validPositions = PathfindingGridSetup.Instance.pathfindingGrid.GetValidPositions();
        Unity.Mathematics.Random random = new Unity.Mathematics.Random(this.random.NextUInt(1, 10000));

        float cellSize = PathfindingGridSetup.Instance.pathfindingGrid.GetCellSize();
        int mapWidth = PathfindingGridSetup.Instance.pathfindingGrid.GetWidth();
        int mapHeight = PathfindingGridSetup.Instance.pathfindingGrid.GetHeight();

        if (timeRemaining > 0)
        {
            timeRemaining -= Time.DeltaTime;
        }
        else
        {
            timeRemaining = 2;
            Entities
                .WithoutBurst()
                .WithReadOnly(busStops)
                .WithAll<ParkingTimerComponent, BusComponent>()
                .ForEach((Entity entity, int entityInQueryIndex, ref ParkingTimerComponent parkingTimer, ref Translation translation) =>
                {
                    if (parkingTimer.timeOfDeparture.Subtract(DateTime.Now).TotalSeconds <= 0)
                    {
                        entityCommandBuffer.RemoveComponent<ParkingTimerComponent>(entityInQueryIndex, entity);

                        PathFollowGetNewPathSystem.AssignNewParams(entity, translation, busStops, cellSize, mapWidth, mapHeight, true, random, out int startX, out int startY, out int endX, out int endY);

                        entityCommandBuffer.AddComponent(entityInQueryIndex, entity, new PathfindingParams
                        {
                            startPosition = new int2(startX, startY),
                            endPosition = new int2(endX, endY)
                        });
                    }
                    
                }).ScheduleParallel();

            Entities
                .WithReadOnly(validPositions)
                .WithoutBurst()
                .WithNone<BusComponent>()
                .WithAll<ParkingTimerComponent>()
                .ForEach((Entity entity, int entityInQueryIndex, ref ParkingTimerComponent parkingTimer, ref Translation translation) =>
                {
                    if (parkingTimer.timeOfDeparture.Subtract(DateTime.Now).TotalSeconds <= 0)
                    {
                        entityCommandBuffer.RemoveComponent<ParkingTimerComponent>(entityInQueryIndex, entity);

                        PathFollowGetNewPathSystem.AssignNewParams(entity, translation, validPositions, cellSize, mapWidth, mapHeight, false, random, out int startX, out int startY, out int endX, out int endY);

                        entityCommandBuffer.AddComponent(entityInQueryIndex, entity, new PathfindingParams
                        {
                            startPosition = new int2(startX, startY),
                            endPosition = new int2(endX, endY)
                        });
                    }

                }).ScheduleParallel();
        }
    }
}
