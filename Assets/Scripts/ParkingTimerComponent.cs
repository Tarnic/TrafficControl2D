using UnityEngine;
using Unity;
using Unity.Entities;
using System;

public struct ParkingTimerComponent : IComponentData {

    public DateTime timeOfDeparture;
}