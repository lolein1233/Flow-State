using UnityEngine;

public class SprayColorCan : MonoBehaviour
{
    public string colorName = "Negro";
    public Color sprayColor = Color.black;

    void Start()
    {
        ApplyColorToCan();
    }

    void ApplyColorToCan()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        foreach (Renderer r in renderers)
        {
            if (r.material.HasProperty("_BaseColor"))
            {
                r.material.SetColor("_BaseColor", sprayColor);
            }
            else if (r.material.HasProperty("_Color"))
            {
                r.material.SetColor("_Color", sprayColor);
            }
        }
    }
}
