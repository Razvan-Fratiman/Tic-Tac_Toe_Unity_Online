using UnityEngine;

// DO NOT add 'public enum Mark { None, X, O }' here,
// as you've confirmed it's already defined elsewhere.

public class Box : MonoBehaviour
{
    public int index;
    public Mark mark; // Logical mark for this box (None, X, or O)
    public bool isMarked; // Whether the box currently has a mark

    private SpriteRenderer spriteRenderer;
    private CircleCollider2D circleCollider; // Reference to the collider

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("Box script requires a SpriteRenderer component.");
        }

        circleCollider = GetComponent<CircleCollider2D>(); // Get reference to the collider
        if (circleCollider == null)
        {
            Debug.LogError("Box script requires a CircleCollider2D component.");
        }

        index = transform.GetSiblingIndex(); // Sets the index based on the GameObject's position in the hierarchy

        // Initialize the box's state using ResetBox() for consistency
        ResetBox();
    }

    public void SetAsMarked(Sprite sprite, Mark newMark, Color color)
    {
        isMarked = true;
        this.mark = newMark; // Update the logical mark for this box

        spriteRenderer.color = color;
        spriteRenderer.sprite = sprite;

        // Disable the CircleCollider2D to prevent marking it twice
        if (circleCollider != null) // Safety check
        {
            circleCollider.enabled = false;
        }
    }

    // Method to reset the box's visual and logical state
    public void ResetBox()
    {
        spriteRenderer.sprite = null; // Clear the sprite (make it invisible)
        spriteRenderer.color = Color.white; // Reset color to default (no tint)
        isMarked = false;
        this.mark = Mark.None; // Reset the logical mark to None

        // Re-enable the CircleCollider2D when the box is reset, so it can be clicked again
        if (circleCollider != null) // Safety check
        {
            circleCollider.enabled = true;
        }
    }
}