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
using UnityEngine.Tilemaps;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Burst;
using CodeMonkey.Utils;

public class Pathfinding : ComponentSystem {

    private const int MOVE_STRAIGHT_COST = 10;
    private const int MOVE_DIAGONAL_COST = 14;
    float timeRemaining = 2;

    protected override void OnUpdate() {
        int gridWidth = PathfindingGridSetup.Instance.pathfindingGrid.GetWidth();
        int gridHeight = PathfindingGridSetup.Instance.pathfindingGrid.GetHeight();
        int2 gridSize = new int2(gridWidth, gridHeight);
        timeRemaining -= Time.DeltaTime;
        List<FindPathJob> findPathJobList = new List<FindPathJob>();
        NativeList<JobHandle> jobHandleList = new NativeList<JobHandle>(Allocator.Temp);
        
        NativeArray<PathNode> pathNodeArray = GetPathNodeArray();
        if(timeRemaining <= 0)
        {
            timeRemaining = 1f;
            Entities.ForEach((Entity entity, ref PathfindingParams pathfindingParams) => {

                NativeArray<PathNode> tmpPathNodeArray = new NativeArray<PathNode>(pathNodeArray, Allocator.TempJob);
                bool isBus = EntityManager.HasComponent<BusComponent>(entity);
                FindPathJob findPathJob = new FindPathJob {
                    gridSize = gridSize,
                    pathNodeArray = tmpPathNodeArray,
                    startPosition = pathfindingParams.startPosition,
                    endPosition = pathfindingParams.endPosition,
                    entity = entity,
                    isBus = isBus,
                };
                findPathJobList.Add(findPathJob);
                jobHandleList.Add(findPathJob.Schedule());

                PostUpdateCommands.RemoveComponent<PathfindingParams>(entity);

            });

            JobHandle.CompleteAll(jobHandleList);
            foreach (FindPathJob findPathJob in findPathJobList) {
                new SetBufferPathJob {
                    entity = findPathJob.entity,
                    gridSize = findPathJob.gridSize,
                    pathNodeArray = findPathJob.pathNodeArray,
                    pathfindingParamsComponentDataFromEntity = GetComponentDataFromEntity<PathfindingParams>(),
                    pathFollowComponentDataFromEntity = GetComponentDataFromEntity<PathFollow>(),
                    pathPositionBufferFromEntity = GetBufferFromEntity<PathPosition>(),
                }.Run();
            }

            pathNodeArray.Dispose();
        }
    }
    
    private NativeArray<PathNode> GetPathNodeArray() {
        Grid grid = PathfindingGridSetup.Instance.pathfindingGrid;

        int2 gridSize = new int2(grid.GetWidth(), grid.GetHeight());
        NativeArray<PathNode> pathNodeArray = new NativeArray<PathNode>(gridSize.x * gridSize.y, Allocator.TempJob);

        for (int x = 0; x < grid.GetWidth(); x++) {
            for (int y = 0; y < grid.GetHeight(); y++) {
                PathNode pathNode = new PathNode();
                pathNode.x = x;
                pathNode.y = y;
                pathNode.index = CalculateIndex(x, y, gridSize.x);

                pathNode.gCost = int.MaxValue;
                
                pathNode.isWalkable = grid.GetGridObject(x, y).IsWalkable();
                pathNode.type = grid.GetGridObject(x, y).GetType();
                pathNode.cameFromNodeIndex = -1;

                pathNodeArray[pathNode.index] = pathNode;
            }
        }

        return pathNodeArray;
    }
	

    [BurstCompile]
    private struct SetBufferPathJob : IJob {
        
        public int2 gridSize;

        [DeallocateOnJobCompletion]
        public NativeArray<PathNode> pathNodeArray;

        public Entity entity;

        public ComponentDataFromEntity<PathfindingParams> pathfindingParamsComponentDataFromEntity;
        public ComponentDataFromEntity<PathFollow> pathFollowComponentDataFromEntity;
        public BufferFromEntity<PathPosition> pathPositionBufferFromEntity;

        public void Execute() {
            DynamicBuffer<PathPosition> pathPositionBuffer = pathPositionBufferFromEntity[entity];
            pathPositionBuffer.Clear();

            PathfindingParams pathfindingParams = pathfindingParamsComponentDataFromEntity[entity];
            int endNodeIndex = CalculateIndex(pathfindingParams.endPosition.x, pathfindingParams.endPosition.y, gridSize.x);
            PathNode endNode = pathNodeArray[endNodeIndex];
            if (endNode.cameFromNodeIndex == -1) {
                // Didn't find a path!
                //Debug.Log("Didn't find a path!");
                pathFollowComponentDataFromEntity[entity] = new PathFollow { pathIndex = -1 };
            } else {
                // Found a path
                CalculatePath(pathNodeArray, endNode, pathPositionBuffer);
                
                pathFollowComponentDataFromEntity[entity] = new PathFollow { pathIndex = pathPositionBuffer.Length - 1 };
            }

        }
    }


    [BurstCompile]
    private struct FindPathJob : IJob {

        public int2 gridSize;
        public NativeArray<PathNode> pathNodeArray;

        public int2 startPosition;
        public int2 endPosition;

        public Entity entity;
        public bool isBus;


        public void Execute() {
            // Define hCost for each node of the grid for A* pathfinding.
            for (int i = 0; i < pathNodeArray.Length; i++) {
                PathNode pathNode = pathNodeArray[i];
                pathNode.hCost = CalculateDistanceCost(new int2(pathNode.x, pathNode.y), endPosition, pathNode.type);
                pathNode.cameFromNodeIndex = -1;

                pathNodeArray[i] = pathNode;
            }

            // Define which are the visitable neighbours -> since we move only in 4 directions there are only 4 visitable neighbours
            NativeArray<int2> neighbourOffsetArray = new NativeArray<int2>(4, Allocator.Temp);
            neighbourOffsetArray[0] = new int2(-1, 0); // Left
            neighbourOffsetArray[1] = new int2(+1, 0); // Right
            neighbourOffsetArray[2] = new int2(0, +1); // Up
            neighbourOffsetArray[3] = new int2(0, -1); // Down

            int endNodeIndex = CalculateIndex(endPosition.x, endPosition.y, gridSize.x);

            PathNode startNode = pathNodeArray[CalculateIndex(startPosition.x, startPosition.y, gridSize.x)];
            startNode.gCost = 0;
            startNode.CalculateFCost();
            pathNodeArray[startNode.index] = startNode;

            NativeList<int> openList = new NativeList<int>(Allocator.Temp);
            NativeList<int> closedList = new NativeList<int>(Allocator.Temp);

            openList.Add(startNode.index);

            int testIndex = 0;

            while (openList.Length > 0) {
                int currentNodeIndex = GetLowestCostFNodeIndex(openList, pathNodeArray);
                PathNode currentNode = pathNodeArray[currentNodeIndex];

                if (currentNodeIndex == endNodeIndex) {
                    // Reached our destination!
                    break;
                }

                // Remove current node from Open List
                for (int i = 0; i < openList.Length; i++) {
                    if (openList[i] == currentNodeIndex) {
                        openList.RemoveAtSwapBack(i);
                        break;
                    }
                }

                closedList.Add(currentNodeIndex);
                
                for (int i = 0; i < neighbourOffsetArray.Length; i++) {
                    int2 neighbourOffset = neighbourOffsetArray[i];
                    int2 neighbourPosition = new int2(currentNode.x + neighbourOffset.x, currentNode.y + neighbourOffset.y);

                    if (!IsPositionInsideGrid(neighbourPosition, gridSize)) {
                        // Neighbour not valid position
                        continue;
                    }

                    int neighbourNodeIndex = CalculateIndex(neighbourPosition.x, neighbourPosition.y, gridSize.x);
                    if (closedList.Contains(neighbourNodeIndex)) {
                        // Already searched this node
                        continue;
                    }

                    PathNode neighbourNode = pathNodeArray[neighbourNodeIndex];
                    if (!neighbourNode.isWalkable) {
                        continue;
                    }
                    // 0 -> wall, 1 -> left, 2 -> right, 3 -> busStop, 4 -> busentranceLeft, 
                    // 5 -> buseEtranceRight, 6 -> busEntranceUp, 7 -> busEntranceDown, 8 -> Parking, 9 -> LeftUpCross,
                    // 10 -> LeftDownCross, 11 -> RightUpCross , 12 -> RightDownCross
                    int currentType = currentNode.type;
                    int neighbourType = neighbourNode.type;

                    if (!isBus)
                    {
                        if (neighbourType > 4 && neighbourType < 10)
                        {
                            continue;
                        }
                    }
                    else
                    {
                        // Check Bus Entrances
                        if (neighbourType == 6 && neighbourOffset.x == 1) // supposed to go left
                        {
                            continue;
                        }
                        if (neighbourType == 7 && neighbourOffset.x == -1) // supposed to go right
                        {
                            continue;
                        }
                        if (neighbourType == 8 && neighbourOffset.y == -1) // supposed to go up
                        {
                            continue;
                        }
                        if (neighbourType == 9 && neighbourOffset.y == 1) // supposed to go down
                        {
                            continue;
                        }
                    }

                    if (currentType == 10 && neighbourType == 10)
                    {
                        continue;
                    }
                    

                    if (!(neighbourType > 4 && neighbourType < 11))
                    {
                        // Normal Streets
                        
                        if (currentType == 1 && neighbourOffset.y != 1)
                        {
                            continue;
                        }
                        if (currentType == 2 && neighbourOffset.y != -1)
                        {
                            continue;
                        }
                        if (currentType == 3 && neighbourOffset.x != -1)
                        {
                            continue;
                        }
                        if (currentType == 4 && neighbourOffset.x != 1)
                        {
                            continue;
                        }

                        // Cross Roads
                        if (currentType == 11 && (neighbourOffset.y != -1 && neighbourOffset.x != -1))
                        {
                            continue;
                        }
                        if (currentType == 12 && (neighbourOffset.y != -1 && neighbourOffset.x != 1))
                        {
                            continue;
                        }
                        if (currentType == 13 && (neighbourOffset.y != 1 && neighbourOffset.x != -1))
                        {
                            continue;
                        }
                        if (currentType == 14 && (neighbourOffset.y != 1 && neighbourOffset.x != 1))
                        {
                            continue;
                        }

                        
                    }

                    // add the node to the list and keep checkin
                    int2 currentNodePosition = new int2(currentNode.x, currentNode.y);
	                int tentativeGCost = currentNode.gCost + CalculateDistanceCost(currentNodePosition, neighbourPosition, neighbourType);
	                if (tentativeGCost < neighbourNode.gCost) {
		                neighbourNode.cameFromNodeIndex = currentNodeIndex;
		                neighbourNode.gCost = tentativeGCost;
		                neighbourNode.CalculateFCost();
		                pathNodeArray[neighbourNodeIndex] = neighbourNode;
                        

                        if (!openList.Contains(neighbourNode.index)) {
                            openList.Add(neighbourNode.index);
		                }
	                }

                }
            }

            neighbourOffsetArray.Dispose();
            openList.Dispose();
            closedList.Dispose();
        }
        

    }
    
    private static void CalculatePath(NativeArray<PathNode> pathNodeArray, PathNode endNode, DynamicBuffer<PathPosition> pathPositionBuffer) {
        if (endNode.cameFromNodeIndex == -1) {
            // Couldn't find a path!
        } else {
            // Found a path
            pathPositionBuffer.Add(new PathPosition { position = new int2(endNode.x, endNode.y) });

            PathNode currentNode = endNode;
            while (currentNode.cameFromNodeIndex != -1) {
                PathNode cameFromNode = pathNodeArray[currentNode.cameFromNodeIndex];
                pathPositionBuffer.Add(new PathPosition { position = new int2(cameFromNode.x, cameFromNode.y), type = cameFromNode.type });
                currentNode = cameFromNode;
            }
        }
    }

    private static NativeList<int2> CalculatePath(NativeArray<PathNode> pathNodeArray, PathNode endNode) {
        if (endNode.cameFromNodeIndex == -1) {
            // Couldn't find a path!
            return new NativeList<int2>(Allocator.Temp);
        } else {
            // Found a path
            NativeList<int2> path = new NativeList<int2>(Allocator.Temp);
            path.Add(new int2(endNode.x, endNode.y));

            PathNode currentNode = endNode;
            while (currentNode.cameFromNodeIndex != -1) {
                PathNode cameFromNode = pathNodeArray[currentNode.cameFromNodeIndex];
                path.Add(new int2(cameFromNode.x, cameFromNode.y));
                currentNode = cameFromNode;
            }

            return path;
        }
    }

    private static bool IsPositionInsideGrid(int2 gridPosition, int2 gridSize) {
        return
            gridPosition.x >= 0 && 
            gridPosition.y >= 0 &&
            gridPosition.x < gridSize.x &&
            gridPosition.y < gridSize.y;
    }

    private static int CalculateIndex(int x, int y, int gridWidth) {
        return x + y * gridWidth;
    }

    private static int CalculateDistanceCost(int2 aPosition, int2 bPosition, int type) {
        int xDistance = math.abs(aPosition.x - bPosition.x);
        int yDistance = math.abs(aPosition.y - bPosition.y);
        int remaining = math.abs(xDistance - yDistance);
        int crossCost = 0;
        
        return MOVE_DIAGONAL_COST * math.min(xDistance, yDistance) + MOVE_STRAIGHT_COST * remaining + crossCost;
    }

    
    private static int GetLowestCostFNodeIndex(NativeList<int> openList, NativeArray<PathNode> pathNodeArray) {
        PathNode lowestCostPathNode = pathNodeArray[openList[0]];
        for (int i = 1; i < openList.Length; i++) {
            PathNode testPathNode = pathNodeArray[openList[i]];
            if (testPathNode.fCost < lowestCostPathNode.fCost) {
                lowestCostPathNode = testPathNode;
            }
        }
        return lowestCostPathNode.index;
    }

    private struct PathNode {
        public int x;
        public int y;
        public int crosses;
        public int index;

        public int gCost;
        public int hCost;
        public int fCost;

        public bool isWalkable;
        public int type;

        public int cameFromNodeIndex;

        public void CalculateFCost() {
            fCost = gCost + hCost;
        }

        public void SetIsWalkable(bool isWalkable) {
            this.isWalkable = isWalkable;
        }

        public void SetType(int type) {
            this.type = type;
        }

    }
}
