using Unity.Entities;
using Unity.Mathematics;

[InternalBufferCapacity(500)]
public struct PathPosition : IBufferElementData {

    public int2 position;
    public int type;

}
