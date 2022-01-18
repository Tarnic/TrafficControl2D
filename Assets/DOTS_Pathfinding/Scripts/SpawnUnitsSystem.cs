using UnityEngine;
using System.Collections;
using UnityEngine.UI;
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
    public int spawnedCars = 0;
    public int spawnedBusses = 0;
    //private bool firstUpdate = true;

    protected override void OnUpdate() {
        if (Input.GetKeyDown(KeyCode.Space)) {
            //firstUpdate = false;

            //random = new Unity.Mathematics.Random(56);
            Text textUI = GameObject.Find("CarNumber").GetComponent<Text>();
            pathfindingGrid = PathfindingGridSetup.Instance.pathfindingGrid;
            gridWidth = pathfindingGrid.GetWidth();
            gridHeight = pathfindingGrid.GetHeight();
            
            SpawnUnits(1000);
            textUI.text = "Current Cars: " + spawnedCars.ToString() + "\nCurrent Busses: " + spawnedBusses.ToString();
        }

    }

    private void SpawnUnits(int spawnCount) {
        PrefabEntityComponent prefabEntityComponent = GetSingleton<PrefabEntityComponent>();
        NativeList<Vector3> validPositions = PathfindingGridSetup.Instance.pathfindingGrid.GetValidPositions();
        float3 value = new float3(0, 0, 0);
        GridNode gridNode;
        Entity spawnedEntity;

        // spawning a certain amount of entities, every 10 cars a bus is spawned
        for (int i = 0; i < spawnCount; i++) {

            if (i % 10 == 0)
            {
                spawnedEntity = EntityManager.Instantiate(prefabEntityComponent.busPrefab);
                spawnedBusses++;
            }
            else
            {
                spawnedEntity = EntityManager.Instantiate(prefabEntityComponent.carPrefab);
                spawnedCars++;
            }
             
            int cont = 0;
            // keep looking for a position till an empty cell is found
            do
            {
                random = Random.CreateFromIndex((uint)i);
                value = validPositions[random.NextInt(0, validPositions.Length)];
                //value = new float3(random.NextInt(gridWidth), random.NextInt(gridHeight), 0f);
                gridNode = pathfindingGrid.GetGridObject((Vector3)value);
                cont++;
            } while (cont<500 && gridNode.IsOccupied());

            if (cont < 500) { 
                EntityManager.SetComponentData(spawnedEntity, new Translation { Value = value });
                gridNode.SetOccupied(true);
            }
        }
    }

}
