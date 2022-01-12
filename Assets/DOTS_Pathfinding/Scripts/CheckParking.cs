using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

public class CheckParking : SystemBase 
{

    private float timeRemaining = 10;
    private BeginInitializationEntityCommandBufferSystem commandBufferSystem;

    protected override void OnCreate()
    {
        commandBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate() {

        EntityCommandBuffer.ParallelWriter entityCommandBuffer = commandBufferSystem.CreateCommandBuffer().AsParallelWriter();

        if (timeRemaining > 0)
        {
            timeRemaining -= Time.DeltaTime;
        }
        else
        {
            // Debug.Log("Time has run out");
            Entities
                .WithAll<ParkingTimerComponent>()
                .ForEach((Entity entity, int entityInQueryIndex) =>
                {
                    entityCommandBuffer.RemoveComponent<ParkingTimerComponent>(entityInQueryIndex, entity);
                }).ScheduleParallel();

            timeRemaining = 5;
        }
    }
}
