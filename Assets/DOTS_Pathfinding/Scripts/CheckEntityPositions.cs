using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Jobs;
using System.Collections.Generic;
using System;
using Unity.Collections;

public class CheckEntityPositions : SystemBase
{
    public static NativeMultiHashMap<int, float3> cellVsEntityPositions;

    protected override void OnCreate()
    {
        cellVsEntityPositions = new NativeMultiHashMap<int, float3>(0, Allocator.Persistent);
        base.OnCreate();
    }

    protected override void OnDestroy()
    {
        cellVsEntityPositions.Dispose();
        base.OnDestroy();
    }

    protected override void OnUpdate()
    {
        float cellSize = PathfindingGridSetup.Instance.pathfindingGrid.GetCellSize();
        EntityQuery eq = GetEntityQuery(typeof(PathFollow));
        cellVsEntityPositions.Clear();

        if (eq.CalculateEntityCount() > cellVsEntityPositions.Capacity)
        {
            cellVsEntityPositions.Capacity = eq.CalculateEntityCount();
        }

        NativeMultiHashMap<int, float3>.ParallelWriter cellEntityPositionParallel = cellVsEntityPositions.AsParallelWriter();

        Entities.ForEach((ref Translation translation, ref PathFollow pathFollow) => {
            cellEntityPositionParallel.Add(GetUniqueKeyForPosition(translation.Value, cellSize), translation.Value);
        }).ScheduleParallel();


    }

    public static int GetUniqueKeyForPosition(float3 position, float cellSize)
    {
        return (int)(19 * math.floor(position.x / cellSize) + (17 * math.floor(position.y / cellSize)));
    }

}
