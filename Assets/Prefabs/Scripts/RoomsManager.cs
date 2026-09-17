using UnityEngine;

public class RoomManager : MonoBehaviour
{
    private Transform player;
    private string currentRoom;

    void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            player = playerObject.transform;
        }
        else
        {
            Debug.LogError("No GameObject with the 'Player' tag found.");
        }
    }

    void Update()
    {
        if (player == null)
            return;

        foreach (Transform room in transform)
        {
            BoxCollider2D roomCollider = room.GetComponent<BoxCollider2D>();

            if (roomCollider != null && roomCollider.OverlapPoint(player.position))
            {
                if (currentRoom != room.name)
                {
                    currentRoom = room.name;
                    SetActiveRoom(room);
                }

                break;
            }
        }
    }

    void SetActiveRoom(Transform activeRoom)
    {
        foreach (Transform room in transform)
        {
            SpriteRenderer overlay = room.GetComponent<SpriteRenderer>();

            if (overlay != null)
            {
                overlay.enabled = room != activeRoom;
            }
        }
    }
}