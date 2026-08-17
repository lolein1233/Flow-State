using UnityEngine;

public class PivotFollowRotation : MonoBehaviour
{
    public Transform player;

    void LateUpdate()
    {
        transform.rotation = Quaternion.Euler(0, player.eulerAngles.y, 0);
    }
}
