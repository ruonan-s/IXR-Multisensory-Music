using UnityEngine;
using UnityEngine.InputSystem;

public class SampleNavigationController : MonoBehaviour
{

    public Transform rigAnchor;
    public Transform rigTracker;

    public InputActionReference primaryLeft;
    public InputActionReference primaryRight;
    public InputActionReference secondaryLeft;
    public InputActionReference secondaryRight;
    public InputActionReference triggerLeft;
    public InputActionReference triggerRight;

    public LayerMask moveLayerMask;

    void TryMove(Vector3 vec)
    {
        RaycastHit hit;
        if (Physics.Raycast(rigAnchor.position, vec.normalized, out hit, vec.magnitude, moveLayerMask))
        {
            rigAnchor.position = rigAnchor.position + vec.normalized * Mathf.Max(0f,hit.distance - 0.2f);
        }
        else
        {
            rigAnchor.position = rigAnchor.position + vec;
        }


    }


    void Start()
    {

        primaryLeft.action.performed += (context) =>
        {
            TryMove(Vector3.down);
        };

        primaryRight.action.performed += (context) =>
        {
            TryMove(new Vector3(rigTracker.forward.x, 0f, rigTracker.forward.z).normalized * -1f);
        };

        secondaryLeft.action.performed += (context) =>
        {
            TryMove(Vector3.up);
        };

        secondaryRight.action.performed += (context) =>
        {
            TryMove(new Vector3(rigTracker.forward.x, 0f, rigTracker.forward.z).normalized);

        };

        triggerLeft.action.performed += (context) =>
        {
            rigAnchor.Rotate(Vector3.up * -90f);

        };
        
        triggerRight.action.performed += (context) =>
        {
            rigAnchor.Rotate(Vector3.up * 90f);
            
        };

    }
    
    


}
