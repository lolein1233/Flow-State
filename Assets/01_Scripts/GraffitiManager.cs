using UnityEngine;

public class GraffitiManager : MonoBehaviour
{
    public Texture2D[] graffitiTextures;
    public int currentIndex = 0;

    public Texture2D GetCurrentGraffiti()
    {
        return graffitiTextures[currentIndex];
    }

    public void NextGraffiti()
    {
        currentIndex = (currentIndex + 1) % graffitiTextures.Length;
    }
}
