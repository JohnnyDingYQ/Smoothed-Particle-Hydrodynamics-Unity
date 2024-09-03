using Unity.Entities;
using UnityEngine;

public class CamAuthoring : MonoBehaviour
{
    class CamBaker : Baker<CamAuthoring>
    {
        public override void Bake(CamAuthoring authoring)
        {
            var e = GetEntity(TransformUsageFlags.Dynamic);
            // AddComponentObject(e, Camera.main);
            AddComponentObject(e, new CameraSingleton
            {
                entity = e,
                camera = authoring.gameObject.GetComponent<Camera>()
            });
        }
    }
}

public class CameraSingleton : IComponentData
{
    public Entity entity;
    public Camera camera;
}

