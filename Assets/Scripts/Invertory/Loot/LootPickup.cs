using UnityEngine;

public class LootPickup : MonoBehaviour
{
    public Item item;
    private Transform player;
    private bool isAttracting = false;
    [SerializeField] private float attractSpeed = 8f;
    [SerializeField] private float attractDist = 3f;
    [SerializeField] private float pick = 0.3f;

    public void Init(Item item)
    {
        this.item = item;
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null && item != null)
            sr.sprite = item.icon;
    }

    private void Start()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null && item != null)
            sr.sprite = item.icon;

        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    private void Update()
    {
        if (player == null) return;

        float dist = Vector2.Distance(transform.position, player.position);

        if (dist < attractDist)
            isAttracting = true;

        if (isAttracting)
        {
            transform.position = Vector2.MoveTowards(transform.position, player.position, attractSpeed * Time.deltaTime);

            if (dist < pick)
            {
                var inventory = player.GetComponent<PlayerInventory>();
                if (inventory != null && item != null)
                {
                    inventory.AddItem(item);
                }
                Destroy(gameObject);
            }
        }
    }
}