using UnityEngine;

public class Box : MonoBehaviour
{
    public int index;
    public Mark mark;
    public bool isMarked;

    private SpriteRenderer spriteRenderer;
    private CircleCollider2D circleCollider;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        circleCollider = GetComponent<CircleCollider2D>();

        index = transform.GetSiblingIndex();
        mark = Mark.None;
        isMarked = false;
    }

    public void SetAsMarked(Sprite sprite, Mark mark, Color color)
    {
        isMarked = true;
        this.mark = mark;

        spriteRenderer.color = color;
        spriteRenderer.sprite = sprite;

        // Disable the CircleCollider2D (to avoid marking it twice)
        circleCollider.enabled = false;
    }

    public void ResetBox()
    {
        // Reset the box to its initial state
        isMarked = false;
        mark = Mark.None;

        // Clear the sprite and reset color
        spriteRenderer.sprite = null;
        spriteRenderer.color = Color.white;

        // Re-enable the collider
        circleCollider.enabled = true;
    }
}