using UnityEngine;

public class PrefabClickHandler : MonoBehaviour
{
    public PrefabManager manager;

    void OnMouseDown() // For editor testing
    {
        HandleClick();
    }

    public void OnPointerClick() // For VR interaction
    {
        HandleClick();
    }

    private void HandleClick()
    {
        if (manager != null && manager.targetObject == gameObject)
        {
            manager.RemoveObject(gameObject); // Remove only if correct
        }
    }
}
