using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

public class UI : MonoBehaviour
{
    private VisualElement root;
    static private TextElement particleCount;
    static private TextElement simsPerSecond;
    private Button resetButton;

    void Start()
    {
        root = GetComponent<UIDocument>().rootVisualElement;
        particleCount = root.Q<TextElement>("particleCount");
        simsPerSecond = root.Q<TextElement>("simsPerSecond");
        resetButton = root.Q<Button>("resetButton");

        resetButton.RegisterCallback((ClickEvent click) => ResetParticles());
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
        UiSystem.RespawnParticlesFlag = true;
    }
}