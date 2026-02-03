using UnityEngine;

public class ShadowSpriteSync : MonoBehaviour
{
    SpriteRenderer parent;
    SpriteRenderer self;

    void Awake()
    {
        parent = transform.parent.GetComponent<SpriteRenderer>();
        self = GetComponent<SpriteRenderer>();

        self.sprite = parent.sprite;
    }
}
