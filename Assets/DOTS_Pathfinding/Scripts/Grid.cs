/* 
    ------------------- Code Monkey -------------------

    Thank you for downloading this package
    I hope you find it useful in your projects
    If you have any questions let me know
    Cheers!

               unitycodemonkey.com
    --------------------------------------------------
 */

using System;
using Unity.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using CodeMonkey.Utils;
using Unity.Mathematics;

public class Grid {

    public event EventHandler<OnGridObjectChangedEventArgs> OnGridObjectChanged;
    public class OnGridObjectChangedEventArgs : EventArgs {
        public int x;
        public int y;
    }

    private int width;
    private int height;
    private float cellSize;
    private Vector3 originPosition;
<<<<<<< Updated upstream
=======
    private NativeList<Vector3> busStops;
>>>>>>> Stashed changes
    private GridNode[,] gridArray;

    private enum GridNodeType {
        Crossroad,
        Tilemap_sx,
        Tilemap_dx,
        Colliders
    }

    public Grid(int width, int height, float cellSize, Vector3 originPosition, Func<Grid, int, int, GridNode> createGridObject) {
        this.width = width;
        this.height = height;
        this.cellSize = cellSize;
        this.originPosition = originPosition;
        this.busStops = new NativeList<Vector3>(0, Allocator.Persistent);

        gridArray = new GridNode[width, height];

        for (int x = 0; x < gridArray.GetLength(0); x++) {
            for (int y = 0; y < gridArray.GetLength(1); y++) {
                GridNode gridNode = createGridObject(this, x, y);

                int type = 3;
                Tilemap[] tilemapList = GameObject.FindObjectsOfType<Tilemap>();
                foreach(var tilemap in tilemapList) {
                    if (tilemap.HasTile(new Vector3Int(x - 18, y - 4, 0))) {
                        string name = tilemap.name;
                        
<<<<<<< Updated upstream
                        if (type != 3 && (name == "Tilemap_sx" || name == "Tilemap_dx")) {
=======

                        if (name == "Tilemap_sx") { 
                            type = 1;
                        } else if (name == "Tilemap_dx") {
                            type = 2;
                        }
                        else if (name == "BusStops")
                        {
                            type = 3;
                            Debug.Log("BusStop");
                            busStops.Add(new Vector3(x, y, 0));
                        }
                        else if (name == "BusEntrancesLeft")
                        {
                            type = 4;
                        }
                        else if (name == "BusEntrancesRight")
                        {
                            type = 5;
                        }
                        else if (name == "BusEntrancesUp")
                        {
                            type = 6;
                        }
                        else if (name == "BusEntrancesDown")
                        {
                            type = 7;
                        }
                        else if (name == "CrossLeftUp"){
                            type = 8;
                        } else if (name == "CrossLeftDown")
                        {
                            type = 9;
                        } else if (name == "CrossRightUp")
                        {
                            type = 10;
                        }
                        else if (name == "CrossRightDown")
                        {
                            type = 11;
                        }
                        else
                        {
>>>>>>> Stashed changes
                            type = 0;
                        } else { 
                            if (name == "Crossroads") { 
                                type = 0;
                            } else if (name == "Tilemap_sx") { 
                                type = 1;
                            } else if (name == "Tilemap_dx") {
                                type = 2;
                            } else {
                                type = 3;
                            }
                        }

                        
                    }
                }
                if (type == 3) {
                    gridNode.SetIsWalkable(false);
                }
                gridNode.SetType(type);
                gridArray[x, y] = gridNode;

            }
        }

        bool showDebug = false;
        if (showDebug) {
            TextMesh[,] debugTextArray = new TextMesh[width, height];

            for (int x = 0; x < gridArray.GetLength(0); x++) {
                for (int y = 0; y < gridArray.GetLength(1); y++) {
                    debugTextArray[x, y] = UtilsClass.CreateWorldText(gridArray[x, y]?.ToString(), null, GetWorldPosition(x, y) + new Vector3(cellSize, cellSize) * .5f, 30, Color.white, TextAnchor.MiddleCenter);
                    Debug.DrawLine(GetWorldPosition(x, y), GetWorldPosition(x, y + 1), Color.white, 100f);
                    Debug.DrawLine(GetWorldPosition(x, y), GetWorldPosition(x + 1, y), Color.white, 100f);
                }
            }
            Debug.DrawLine(GetWorldPosition(0, height), GetWorldPosition(width, height), Color.white, 100f);
            Debug.DrawLine(GetWorldPosition(width, 0), GetWorldPosition(width, height), Color.white, 100f);

            OnGridObjectChanged += (object sender, OnGridObjectChangedEventArgs eventArgs) => {
                debugTextArray[eventArgs.x, eventArgs.y].text = gridArray[eventArgs.x, eventArgs.y]?.ToString();
            };
        }
    }

    public int GetWidth() {
        return width;
    }

    public int GetHeight() {
        return height;
    }

    public float GetCellSize() {
        return cellSize;
    }

    public Vector3 GetWorldPosition(int x, int y) {
        return new Vector3(x, y) * cellSize + originPosition;
    }

    public void GetXY(Vector3 worldPosition, out int x, out int y) {
        x = Mathf.FloorToInt((worldPosition - originPosition).x / cellSize);
        y = Mathf.FloorToInt((worldPosition - originPosition).y / cellSize);
    }

    public void SetGridObject(int x, int y, GridNode value) {
        if (x >= 0 && y >= 0 && x < width && y < height) {
            gridArray[x, y] = value;
            if (OnGridObjectChanged != null) OnGridObjectChanged(this, new OnGridObjectChangedEventArgs { x = x, y = y });
        }
    }

    public void TriggerGridObjectChanged(int x, int y) {
        if (OnGridObjectChanged != null) OnGridObjectChanged(this, new OnGridObjectChangedEventArgs { x = x, y = y });
    }

    public void SetGridObject(Vector3 worldPosition, GridNode value) {
        int x, y;
        GetXY(worldPosition, out x, out y);
        SetGridObject(x, y, value);
    }

    public GridNode GetGridObject(int x, int y) {
        if (x >= 0 && y >= 0 && x < width && y < height) {
            return gridArray[x, y];
        } else {
            return default(GridNode);
        }
    }

    public GridNode GetGridObject(Vector3 worldPosition) {
        int x, y;
        GetXY(worldPosition, out x, out y);
        return GetGridObject(x, y);
    }

<<<<<<< Updated upstream
=======
    public NativeList<Vector3> GetBusStops()
    {
        return busStops;
    }
>>>>>>> Stashed changes
}
