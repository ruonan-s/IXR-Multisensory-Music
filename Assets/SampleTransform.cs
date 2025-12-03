using UnityEngine;
using UnityEngine.InputSystem;
using Text = TMPro.TMP_Text;


public class SampleTransform : MonoBehaviour
{

    public TransformationEvaluator evaluator;

    public InputActionReference triggerPress;
    public InputActionReference primaryButtonPress;
    public InputActionReference secondaryButtonPress;
    public InputActionReference joyStickMove;
    public Text guideText;

    public float moveSpeed;
    public float rotateSpeed;
    public float scaleSpeed;

    Transform sourceTransform;


    int transformType = 0; //Scale, Rotate, Translate
    string transformString = "Scale";

    int axis = 0; //X, Y, Z
    string axisString = "X";

    void ConfirmSelection(InputAction.CallbackContext context)
    {
        evaluator.ConfirmSelection();
    }

    void UpdateGuide()
    {
        guideText.text = transformString + " " + axisString;
    }

    void RotateTransformType(InputAction.CallbackContext context)
    {
        transformType = (transformType + 1) % 3;

        transformString = (transformType == 0) ? "Scale" :
                          (transformType == 1) ? "Rotate" : "Translate";

        UpdateGuide();
    }

    void RotateAxis(InputAction.CallbackContext context)
    {
        axis = (axis + 1) % 3;

        axisString = (axis == 0) ? "X" :
                     (axis == 1) ? "Y" : "Z";


        UpdateGuide();
    }


    void Update()
    {
        float val = joyStickMove.action.ReadValue<Vector2>().y;

        Vector3 axisChannel = (axis == 0) ? Vector3.right :
                              (axis == 1) ? Vector3.up : Vector3.forward;

        if (transformType == 0)
        {
            axisChannel *= scaleSpeed * val * Time.deltaTime;
            sourceTransform.localScale += axisChannel;
        }
        else if (transformType == 1)
        {
            axisChannel *= rotateSpeed * val * Time.deltaTime;
            sourceTransform.Rotate(axisChannel);
        }
        else
        {
            axisChannel *= moveSpeed * val * Time.deltaTime;
            sourceTransform.localPosition += axisChannel;
        }

    }



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        triggerPress.action.performed += ConfirmSelection;
        primaryButtonPress.action.performed += RotateTransformType;
        secondaryButtonPress.action.performed += RotateAxis;

        sourceTransform = evaluator.GetSourceTransform();
        UpdateGuide();

    }


}
