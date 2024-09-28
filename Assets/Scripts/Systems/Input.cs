using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public partial struct MouseInputSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<ConfigSingleton>();
        state.RequireForUpdate<ClickData>();
    }

    public void OnUpdate(ref SystemState state)
    {
        if (Input.GetMouseButtonDown(0))
        {
            float3 mousePosition = Input.mousePosition;

            Ray ray = Camera.main.ScreenPointToRay(mousePosition);
            SystemAPI.SetSingleton(new ClickData() { clicked = true, pos = ray.origin + ray.direction * (-ray.origin.y) / ray.direction.y });
        }
        else
            SystemAPI.SetSingleton(new ClickData() { clicked = false });

    }
}
