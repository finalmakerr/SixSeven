using UnityEngine;

public class Board : MonoBehaviour
{
    public int width = 7;
    public int height = 7;
    public float spacing = 1.05f;

    public Tile tilePrefab;

    private Sprite[] sprites;

    private void Awake()
    {
        sprites = Resources.LoadAll<Sprite>("Tiles");
        if (sprites == null || sprites.Length == 0)
            Debug.LogError("No sprites found in Assets/Resources/Tiles");
    }

    private void Start()
    {
        Generate();
    }

    private void Generate()
    {
        float startX = -(width - 1) * spacing * 0.5f;
        float startY = -(height - 1) * spacing * 0.5f;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector3 pos = new Vector3(startX + x * spacing, startY + y * spacing, 0f);
                Tile t = Instantiate(tilePrefab, pos, Quaternion.identity, transform);

                Sprite s = sprites[Random.Range(0, sprites.Length)];
                t.SetSprite(s);
            }
        }
    }
}
