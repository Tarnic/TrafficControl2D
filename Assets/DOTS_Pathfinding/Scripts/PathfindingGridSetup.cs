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
using System.IO;


public class PathfindingGridSetup : MonoBehaviour {
    [SerializeField] public int height = 50;
    [SerializeField] public int width = 50;
    [SerializeField] public bool collisions;
    [SerializeField] public int busUnits;
    [SerializeField] public int carUnits;



    public static bool collisionsFlag;
    public static int busToSpawn;
    public static int carsToSpawn;

    public static PathfindingGridSetup Instance { private set; get; }

    // [SerializeField] private PathfindingVisual pathfindingVisual;
    public Grid pathfindingGrid;

    private void Awake() {
        Instance = this;
        /////////////////////////////////////////////////////
        // LOADING CONFIGURATIONS FROM TXT FILE
        ///////////////////////////////////////////////////
        StreamReader reader = new StreamReader("Assets/configuration.TXT");
        string[] data = reader.ReadToEnd().Split('\n');
        width = int.Parse(data[0].Split('=')[1]);
        height = int.Parse(data[1].Split('=')[1]);
        //collisions = data[2].Split('=')[1] == "true";
        collisions = int.Parse(data[2].Split('=')[1]) == 1;
        busToSpawn = int.Parse(data[3].Split('=')[1]);
        carsToSpawn = int.Parse(data[4].Split('=')[1]);
        collisionsFlag = collisions;
        Debug.Log(width);
        Debug.Log(height);
        Debug.Log(collisions);
        Debug.Log(busToSpawn);
        Debug.Log(carsToSpawn);
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

    public static int GetBusToSpawn()
    {
        return busToSpawn;
    }

    public static int GetCarsToSpawn()
    {
        return carsToSpawn;
    }

}
