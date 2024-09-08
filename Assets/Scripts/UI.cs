using System.Text;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;

public class UI : MonoBehaviour
{
    private VisualElement root;
    static private TextElement particleCount;
    static private TextElement simsPerSecond;
    private Button resetButton;
    private Button testButton;

    void Start()
    {
        root = GetComponent<UIDocument>().rootVisualElement;
        particleCount = root.Q<TextElement>("particleCount");
        simsPerSecond = root.Q<TextElement>("simsPerSecond");
        resetButton = root.Q<Button>("resetButton");
        testButton = root.Q<Button>("testButton");

        resetButton.RegisterCallback((ClickEvent click) => ResetParticles());
        testButton.RegisterCallback((ClickEvent click) => Test());
    }

    public static void SetParticleCount(int count)
    {
        particleCount.text = $"Particle Count: {count}";
    }

    public static void SetSimsPerSecond(int count)
    {
        simsPerSecond.text = $"Simulations Per Second: {count}";
    }

    void ResetParticles()
    {
        EntityManager entityManager =  World.DefaultGameObjectInjectionWorld.EntityManager;
        EntityQuery query = entityManager.CreateEntityQuery(typeof(ActionFlags));
        var flagEntity = query.GetSingletonEntity();
        ActionFlags actionFlags = entityManager.GetComponentData<ActionFlags>(flagEntity);
        actionFlags.RespawnParticles = true;
        entityManager.SetComponentData(flagEntity, actionFlags);
    }

    void Test()
    {
        EntityManager entityManager =  World.DefaultGameObjectInjectionWorld.EntityManager;
        EntityQuery query = entityManager.CreateEntityQuery(typeof(ActionFlags));
        var flagEntity = query.GetSingletonEntity();
        ActionFlags actionFlags = entityManager.GetComponentData<ActionFlags>(flagEntity);
        actionFlags.ApplyForce = true;
        entityManager.SetComponentData(flagEntity, actionFlags);
    }
}