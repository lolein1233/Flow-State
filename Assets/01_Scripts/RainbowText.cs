using UnityEngine;
using TMPro;
public class RainbowText : MonoBehaviour
{
    public TMP_Text text;
    public float speed = 1.5f;
    public float saturation = 1f;
    public float brightness = 1f;

    void Reset()
    {
        text = GetComponent<TMP_Text>();
    }

    void Update()
    {
        if (text == null) return;

        float currentAlpha = text.color.a; // mantener alpha del fade

        float hue = Mathf.Repeat(Time.time * speed, 1f);
        Color rainbow = Color.HSVToRGB(hue, saturation, brightness);

        rainbow.a = currentAlpha; // clave

        text.color = rainbow;
    }
}
