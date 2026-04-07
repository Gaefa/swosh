using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    public Transform player;
    public Transform playerSpawn;

    private void Start()
    {
        if (player != null && playerSpawn != null)
        {
            player.position = playerSpawn.position;
        }
    }
}