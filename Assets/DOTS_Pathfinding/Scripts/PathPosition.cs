using Unity.Entities;
using Unity.Mathematics;

[InternalBufferCapacity(200)]
public struct PathPosition : IBufferElementData {

    public int2 position;
    public int type;

}
