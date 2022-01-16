using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;
using Random = Unity.Mathematics.Random;

public class SpawnUnitsSystem : ComponentSystem {

    private Random random;
    private int gridWidth;
    private int gridHeight;
    Grid pathfindingGrid;

    //private bool firstUpdate = true;

    protected override void OnUpdate() {
        if (Input.GetKeyDown(KeyCode.Space)) {
            //firstUpdate = false;
            
            //random = new Unity.Mathematics.Random(56);

            pathfindingGrid = PathfindingGridSetup.Instance.pathfindingGrid;
            gridWidth = pathfindingGrid.GetWidth();
            gridHeight = pathfindingGrid.GetHeight();

            SpawnUnits(500);
        }

        //if (Input.GetKeyDown(KeyCode.Space)) {
        //    SpawnUnits(200);
        //}
    }

    private void SpawnUnits(int spawnCount) {
        PrefabEntityComponent prefabEntityComponent = GetSingleton<PrefabEntityComponent>();
        NativeList<Vector3> validPositions = PathfindingGridSetup.Instance.pathfindingGrid.GetValidPositions();
        float3 value = new float3(0, 0, 0);
        GridNode gridNode;
        Entity spawnedEntity;

        for (int i = 0; i < spawnCount; i++) {

            if (i % 10 == 0)
            {
                spawnedEntity = EntityManager.Instantiate(prefabEntityComponent.busPrefab);
            }
            else
            {
                spawnedEntity = EntityManager.Instantiate(prefabEntityComponent.carPrefab);
            }
             
            int cont = 0;
            do
            {
                random = Random.CreateFromIndex((uint)i);
                value = validPositions[random.NextInt(0, validPositions.Length)];
                //value = new float3(random.NextInt(gridWidth), random.NextInt(gridHeight), 0f);
                gridNode = pathfindingGrid.GetGridObject((Vector3)value);
                cont++;
                //} while (cont<500 && (!gridNode.IsWalkable() || (gridNode.GetType() > 4 && gridNode.GetType() != 10) || gridNode.IsOccupied()));
            } while (cont<500 && gridNode.IsOccupied());

            if (cont < 500) { 
                EntityManager.SetComponentData(spawnedEntity, new Translation { Value = value });
                gridNode.SetOccupied(true);
            }
        }
    }

}
