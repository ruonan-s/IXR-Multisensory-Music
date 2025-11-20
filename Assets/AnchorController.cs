using UnityEngine;
using UnityEngine.Rendering;

public class AnchorController : MonoBehaviour
{

    RandomRouteEvaluator rre;
    FixedRouteEvaluator fre;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        rre = FindAnyObjectByType<RandomRouteEvaluator>();
        fre = FindAnyObjectByType<FixedRouteEvaluator>();
    }

    private void OnTriggerEnter(Collider col)
    {

        if (col.tag != "Player")
        {
            return;
        }

        if (rre)
        {
            rre.ProgressTrial();
        }
        else if (fre)
        {
            fre.ProgressTrial();
        }
        
    }
    


}
