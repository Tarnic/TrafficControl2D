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
    private static NativeList<Vector3> busStops;
    private static NativeList<Vector3> validPositions;
    private GridNode[,] gridArray;

    public Grid(int width, int height, float cellSize, Vector3 originPosition, Func<Grid, int, int, GridNode> createGridObject) {
        this.width = width;
        this.height = height;
        this.cellSize = cellSize;
        this.originPosition = originPosition;
        busStops = new NativeList<Vector3>(0, Allocator.Persistent);
        validPositions = new NativeList<Vector3>(0, Allocator.Persistent);
        gridArray = new GridNode[width, height];

        for (int x = 0; x < gridArray.GetLength(0); x++) {
            for (int y = 0; y < gridArray.GetLength(1); y++) {
                GridNode gridNode = createGridObject(this, x, y);

                int type = 0;
                Tilemap[] tilemapList = GameObject.FindObjectsOfType<Tilemap>();
                foreach(var tilemap in tilemapList) {
                    if (tilemap.HasTile(new Vector3Int(x - 18, y - 4, 0))) {
                        string name = tilemap.name;
                        

                        if      (name == "MoveUp") { 
                            type = 1;
                            validPositions.Add(new Vector3(x, y, 0));
                        }
                        else if (name == "MoveDown")
                        {
                            type = 2;
                            validPositions.Add(new Vector3(x, y, 0));
                        }
                        else if (name == "MoveLeft")
                        {
                            type = 3;
                            validPositions.Add(new Vector3(x, y, 0));
                        }
                        else if (name == "MoveRight")
                        {
                            type = 4;
                            validPositions.Add(new Vector3(x, y, 0));
                        }
                        else if (name == "BusStops")
                        {
                            type = 5;
                            busStops.Add(new Vector3(x, y, 0));
                        }
                        else if (name == "BusEntrancesLeft")
                        {
                            type = 6;
                        }
                        else if (name == "BusEntrancesRight")
                        {
                            type = 7;
                        }
                        else if (name == "BusEntrancesUp")
                        {
                            type = 8;
                        }
                        else if (name == "BusEntrancesDown")
                        {
                            type = 9;
                        }
                        else if (name == "Parkings")
                        {
                            type = 10;
                            validPositions.Add(new Vector3(x, y, 0));
                        }
                        else if (name == "CrossLeftUp"){
                            type = 11;
                        }
                        else if (name == "CrossLeftDown")
                        {
                            type = 12;
                        } else if (name == "CrossRightUp")
                        {
                            type = 13;
                        }
                        else if (name == "CrossRightDown")
                        {
                            type = 14;
                        }
                        else
                        {
                            type = 0;
                        }   
                    }
                }
                if (type == 0) {
                    gridNode.SetIsWalkable(false);
                }
                gridNode.SetType(type);
                gridArray[x, y] = gridNode;

            }
        }

        /*bool showDebug = false;
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
        }*/
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

    public NativeList<Vector3> GetBusStops()
    {
        return busStops;
    }
    public NativeList<Vector3> GetValidPositions()
    {
        return validPositions;
    }
}
