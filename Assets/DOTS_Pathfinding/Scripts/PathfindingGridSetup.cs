/* 
    ------------------- Code Monkey -------------------

    Thank you for downloading this package
    I hope you find it useful in your projects
    If you have any questions let me know
    Cheers!

               unitycodemonkey.com
    --------------------------------------------------
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeMonkey.Utils;
using CodeMonkey;
using Unity.Mathematics;
using Unity.Collections;


public class PathfindingGridSetup : MonoBehaviour {
    [SerializeField] public int height = 50;
    [SerializeField] public int width = 50;
    [SerializeField] public bool collisions;
    [SerializeField] public int units;
    public static bool collisionsFlag;
    public static int unitsToSpawn;

    public static PathfindingGridSetup Instance { private set; get; }

    // [SerializeField] private PathfindingVisual pathfindingVisual;
    public Grid pathfindingGrid;

    private void Awake() {
        Instance = this;
        collisionsFlag = collisions;
        unitsToSpawn = units;
    }

    private void Start() {
        pathfindingGrid = new Grid(width, height, 1f, Vector3.zero, (Grid grid, int x, int y) => new GridNode(grid, x, y));
       
    }

    private void Update() {
        /*if (Input.GetMouseButtonDown(1)) {
            Vector3 mousePosition = UtilsClass.GetMouseWorldPosition() + (new Vector3(+1, +1) * pathfindingGrid.GetCellSize() * .5f);
            GridNode gridNode = pathfindingGrid.GetGridObject(mousePosition);
            if (gridNode != null) {
                gridNode.SetIsWalkable(!gridNode.IsWalkable());
            }
        }*/
    }

    private void OnDestroy()
    {
        pathfindingGrid.GetBusStops().Dispose();
        pathfindingGrid.GetValidPositions().Dispose();
    }

    public static bool GetCollisions()
    {
        return collisionsFlag;
    }

    public static int GetUnitsToSpawn()
    {
        return unitsToSpawn;
    }

}
