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
using Random = Unity.Mathematics.Random;


public class PathfindingGridSetup : MonoBehaviour 
{
    [SerializeField] private GameObject nwAngle;
    [SerializeField] private GameObject neAngle;
    [SerializeField] private GameObject swAngle;
    [SerializeField] private GameObject seAngle;
    [SerializeField] private GameObject[] centralBlocks;
    [SerializeField] private GameObject borderDown;
    [SerializeField] private GameObject borderLeft;
    [SerializeField] private GameObject borderRight;
    [SerializeField] private GameObject borderUp;

    private Random random;

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

    private void Awake() 
    {
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
        random = new Random((uint)(numSectors * 13));
        height = width = sizeSector * numSectors;
    }

    private void Start() 
    {
        
        Vector3 offset = new Vector3(0.5f, 23.5f, 0f);
        float anglePosition = sizeSector * (numSectors - 1);

        // Instatiate angle blocks
        Instantiate(nwAngle, new Vector3(0, anglePosition, 0) - offset, Quaternion.identity);
        Instantiate(neAngle, new Vector3(anglePosition, anglePosition, 0) - offset, Quaternion.identity);
        Instantiate(swAngle, new Vector3(0, 0, 0) - offset, Quaternion.identity);
        Instantiate(seAngle, new Vector3(anglePosition, 0, 0) - offset, Quaternion.identity);

        // Instantiate border blocks

        // Number of border blocks is given by the following formula
        int numBorderBlocks = (int)(numSectors - 2);

        for (int i = 0; i < numBorderBlocks; i++)
        {
            Instantiate(borderDown, new Vector3(sizeSector * (i + 1), 0, 0) - offset, Quaternion.identity);
            Instantiate(borderUp, new Vector3(sizeSector * (i + 1), sizeSector * (numSectors - 1), 0) - offset, Quaternion.identity);
            Instantiate(borderLeft, new Vector3(0, sizeSector * (i + 1), 0) - offset, Quaternion.identity);
            Instantiate(borderRight, new Vector3(sizeSector * (numSectors - 1), sizeSector * (i + 1), 0) - offset, Quaternion.identity);
        }

        // Instantiate central blocks

        // Number of central blocks is given by the following formula
        int numCentralBlocks = (int) numSectors - 2;
        int indexCentralBlocks;
        
        for (int i = 0; i < numCentralBlocks; i++)
        {
            for (int j = 0; j < numCentralBlocks; j++)
            {
                indexCentralBlocks = random.NextInt(centralBlocks.Length);
                Instantiate(centralBlocks[indexCentralBlocks], new Vector3((i + 1) * sizeSector, (j + 1) * sizeSector, 0) - offset, Quaternion.identity);
            }
        }
        
        pathfindingGrid = new Grid(width, height, 1f, Vector3.zero, (Grid grid, int x, int y) => new GridNode(grid, x, y));
        pathfindingVisual.SetGrid(pathfindingGrid);
    }

    private void Update()
    {
        //if (Input.GetMouseButtonDown(0))
        //{
        //    Vector3 mousePosition = UtilsClass.GetMouseWorldPosition();// - new Vector3(0, 23.5f, 0);
        //    GridNode node = pathfindingGrid.GetGridObject(mousePosition);
        //    Debug.Log(node.GetType());
        //}
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
