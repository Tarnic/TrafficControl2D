using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using CodeMonkey.Utils;

public class UnitMoveOrderSystem : ComponentSystem
{
    private bool running = false;
    float3 value;
    private Unity.Mathematics.Random random = new Unity.Mathematics.Random(56);
    protected override void OnUpdate()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mouseposition = UtilsClass.GetMouseWorldPosition();

            float cellsize = PathfindingGridSetup.Instance.pathfindingGrid.GetCellSize();

            PathfindingGridSetup.Instance.pathfindingGrid.GetXY(mouseposition + new Vector3(1, 1) * cellsize * +.5f, out int endx, out int endy);

            ValidateGridPosition(ref endx, ref endy);
            //cmdebug.textpopupmouse(x + ", " + y);

            Entities.ForEach((Entity entity, DynamicBuffer<PathPosition> pathpositionbuffer, ref Translation translation) =>
             {
                //debug.log("add component!");
                PathfindingGridSetup.Instance.pathfindingGrid.GetXY(translation.Value + new float3(1, 1, 0) * cellsize * +.5f, out int startx, out int starty);

                 ValidateGridPosition(ref startx, ref starty);

                 EntityManager.AddComponentData(entity, new PathfindingParams
                 {
                     startPosition = new int2(startx, starty),
                     endPosition = new int2(endx, endy)
                 });
             }) ;
        }

    }

    private void ValidateGridPosition(ref int x, ref int y)
    {
        x = math.clamp(x, 0, PathfindingGridSetup.Instance.pathfindingGrid.GetWidth() - 1);
        y = math.clamp(y, 0, PathfindingGridSetup.Instance.pathfindingGrid.GetHeight() - 1);
    }

}
