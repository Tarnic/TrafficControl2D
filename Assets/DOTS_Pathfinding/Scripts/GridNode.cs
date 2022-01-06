using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridNode {

    private Grid grid;
    private int x;
    private int y;

    private bool isWalkable;
    private int type;
    private bool isOccupied;

    public GridNode(Grid grid, int x, int y) {
        this.grid = grid;
        this.x = x;
        this.y = y;
        isWalkable = true;
    }

    public bool IsWalkable() {
        return isWalkable;
    }

    public void SetIsWalkable(bool isWalkable) {
        this.isWalkable = isWalkable;
        grid.TriggerGridObjectChanged(x, y);
    }

    public void SetType(int type) {
        this.type = type;
        grid.TriggerGridObjectChanged(x, y);
    }

    public new int GetType() {
        return type;
    }

    public void SetOccupied(bool occupied) { 
        this.isOccupied = occupied;
        grid.TriggerGridObjectChanged(x, y);
    }

    public bool IsOccupied() {
        return isOccupied;
    }

}
