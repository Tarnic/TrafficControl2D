using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

public class SpawnUnitsSystem : ComponentSystem {

    private Unity.Mathematics.Random random;
    private int gridWidth;
    private int gridHeight;
    Grid pathfindingGrid;

    private bool firstUpdate = true;

    protected override void OnUpdate() {
        if (firstUpdate) {
            firstUpdate = false;
            
            random = new Unity.Mathematics.Random(56);

            pathfindingGrid = PathfindingGridSetup.Instance.pathfindingGrid;
            gridWidth = pathfindingGrid.GetWidth();
            gridHeight = pathfindingGrid.GetHeight();

            SpawnUnits(0);
        }

        if (Input.GetKeyDown(KeyCode.Space)) {
            SpawnUnits(100);
        }
    }

    private void SpawnUnits(int spawnCount) {
        PrefabEntityComponent prefabEntityComponent = GetSingleton<PrefabEntityComponent>();
        float3 value = new float3(0, 0, 0);
        GridNode gridNode;

        for (int i = 0; i < spawnCount; i++) {
            Entity spawnedEntity = EntityManager.Instantiate(prefabEntityComponent.prefabEntity);
            
            do {
                value = new float3(random.NextInt(gridWidth), random.NextInt(gridHeight), 0f);
                gridNode = pathfindingGrid.GetGridObject((Vector3)value);

            } while (!gridNode.IsWalkable() || gridNode.IsOccupied());

            EntityManager.SetComponentData(spawnedEntity, new Translation { Value = value });
            gridNode.SetOccupied(true);
        }
    }

}
