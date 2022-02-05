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
    [SerializeField] private GameObject nwAngle;
    [SerializeField] private GameObject neAngle;
    [SerializeField] private GameObject swAngle;
    [SerializeField] private GameObject seAngle;

    private int numSectors;
    private bool collisions;
    private int busUnits;
    private int carUnits;
    private int sizeSector = 30;
    private int height;
    private int width;

    public static bool collisionsFlag;
    public static int busToSpawn;
    public static int carsToSpawn;

    public static PathfindingGridSetup Instance { private set; get; }

    [SerializeField] private PathfindingVisual pathfindingVisual;
    public Grid pathfindingGrid;

    private void Awake() {
        Instance = this;

        ///////////////////////////////////////////////////
        //     LOADING CONFIGURATIONS FROM TXT FILE      //
        ///////////////////////////////////////////////////
        
        StreamReader reader = new StreamReader("Assets/configuration.TXT");
        string[] data = reader.ReadToEnd().Split('\n');
        numSectors = int.Parse(data[0].Split('=')[1]);
        collisions = int.Parse(data[1].Split('=')[1]) == 1;
        busToSpawn = int.Parse(data[2].Split('=')[1]);
        carsToSpawn = int.Parse(data[3].Split('=')[1]);
        collisionsFlag = collisions;

        height = width = sizeSector * numSectors;
    }

    private void Start() {

        Instantiate(nwAngle, new Vector3(0, sizeSector, 0), Quaternion.identity);
        Instantiate(neAngle, new Vector3(sizeSector, sizeSector, 0), Quaternion.identity);
        Instantiate(swAngle, new Vector3(0, 0, 0), Quaternion.identity);
        Instantiate(seAngle, new Vector3(sizeSector, 0, 0), Quaternion.identity);

        pathfindingGrid = new Grid(width, height, 1f, Vector3.zero, (Grid grid, int x, int y) => new GridNode(grid, x, y));
        pathfindingVisual.SetGrid(pathfindingGrid);
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mousePosition = UtilsClass.GetMouseWorldPosition() - new Vector3(0, 23.5f, 0);
            GridNode node = pathfindingGrid.GetGridObject(mousePosition);
            Debug.Log(node.GetType());
        }
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
