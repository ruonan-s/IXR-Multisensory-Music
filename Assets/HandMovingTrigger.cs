using UnityEngine;

public class OVRHandVelocityEnvironment : MonoBehaviour
{
    public Transform rightHandAnchor;      
    public Transform environmentObject;    
    public float movementScale = 0.3f;     

    void Update()
    {
        // --------------- 1. print right hand location ------------------
        if (rightHandAnchor != null)
        {
            Debug.Log("Right Hand Position: " + rightHandAnchor.position.ToString("F3"));
        }

        // --------------- 2. get -------------
        Vector3 localVel = OVRInput.GetLocalControllerVelocity(OVRInput.Controller.RTouch);

        // 转换成世界坐标速度
        Vector3 worldVel = rightHandAnchor.TransformDirection(localVel);

        // 打印速度（方便调试）
        Debug.Log("Right Hand Velocity: " + worldVel.ToString("F3"));

        // --------------- 3. 把手的速度映射到环境物体 -----------
        if (environmentObject != null)
        {
            // 让环境按照手的速度移动（可换成旋转、抖动、粒子等）
            environmentObject.position += worldVel * movementScale * Time.deltaTime;
        }
    }
}
