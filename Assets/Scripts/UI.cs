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
    private VisualElement links;

    void Start()
    {
        root = GetComponent<UIDocument>().rootVisualElement;
        particleCount = root.Q<TextElement>("particleCount");
        simsPerSecond = root.Q<TextElement>("simsPerSecond");
        resetButton = root.Q<Button>("resetButton");
        testButton = root.Q<Button>("testButton");
        links = root.Q<VisualElement>("links");

        resetButton.RegisterCallback((ClickEvent click) => ResetParticles());
        testButton.RegisterCallback((ClickEvent click) => Test());
        links.RegisterCallback((ClickEvent click) => JumpToLink());
    }

    public static void SetParticleCount(int count)
    {
        particleCount.text = count.ToString();
    }

    public static void SetSimsPerSecond(int count)
    {
        simsPerSecond.text = count.ToString();
    }

    void ResetParticles()
    {
        EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        EntityQuery query = entityManager.CreateEntityQuery(typeof(ActionFlags));
        var flagEntity = query.GetSingletonEntity();
        ActionFlags actionFlags = entityManager.GetComponentData<ActionFlags>(flagEntity);
        actionFlags.RespawnParticles = true;
        entityManager.SetComponentData(flagEntity, actionFlags);
    }

    void Test()
    {
        EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        EntityQuery query = entityManager.CreateEntityQuery(typeof(ActionFlags));
        var flagEntity = query.GetSingletonEntity();
        ActionFlags actionFlags = entityManager.GetComponentData<ActionFlags>(flagEntity);
        actionFlags.ApplyForce = true;
        entityManager.SetComponentData(flagEntity, actionFlags);
    }

    void JumpToLink()
    {
        Application.OpenURL("https://github.com/JohnnyDingYQ/Smoothed-Particle-Hydrodynamics-Unity");
    }
}