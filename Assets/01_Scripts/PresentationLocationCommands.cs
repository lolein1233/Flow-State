using UnityEngine;

/// <summary>Atajos de la presentacion: F7 para Lugar 1 y F8 para Lugar 2.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(FPSController))]
public sealed class PresentationLocationCommands : MonoBehaviour
{
    public FPSController player;
    public Transform lugar1;
    public Transform lugar2;
    public bool startAtLugar1 = true;

    void Awake()
    {
        if (player == null)
            player = GetComponent<FPSController>();
    }

    void Start()
    {
        if (startAtLugar1)
            GoToLugar(1);
    }

    void Update()
    {
        if (player == null || !player.isActiveAndEnabled || Time.timeScale <= 0f)
            return;

        if (Input.GetKeyDown(KeyCode.F7))
            GoToLugar(1);
        else if (Input.GetKeyDown(KeyCode.F8))
            GoToLugar(2);
    }

    public bool GoToLugar(int number)
    {
        Transform destination = number == 1 ? lugar1 : number == 2 ? lugar2 : null;
        if (player != null && destination != null && player.TryTeleportToGround(destination))
            return true;

        Debug.LogWarning($"No se pudo ir a Lugar {number}: revisa el marcador, el suelo y el espacio para el player.", this);
        return false;
    }
}
