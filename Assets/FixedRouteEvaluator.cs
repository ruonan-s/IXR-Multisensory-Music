using UnityEngine;
using System.Collections.Generic;
using System;

using Text = TMPro.TMP_Text;
using Toggle = UnityEngine.UI.Toggle;

public class FixedRouteEvaluator : MonoBehaviour
{


    public Transform playerTracker;
    public Transform playerAnchor;
    public Transform anchorOrigin;
    public Transform arrow;
    public Text evalText;
    public Toggle startTrialButton;
    public Vector3 playerStartPosition;


    public int[] route;

    Transform[] routeWaypoints;

    int routeLength;
    int trialProgress;
    float startTime;
    bool inProgress;
    

    void Start()
    {
        startTrialButton.onValueChanged.AddListener((b) => { StartTrial(); });
    }

    void Update()
    {

        if (inProgress)
        {
            arrow.position = playerTracker.position + playerTracker.forward * 0.4f;
            if (trialProgress < routeWaypoints.Length)
            {
                arrow.LookAt(routeWaypoints[trialProgress]);
            }
        }

    }

    void StartTrial()
    {

        routeWaypoints = new Transform[route.Length];
        HashSet<int> seen = new();
        seen.Add(-1);

        AnchorController[] waypoints = FindObjectsByType<AnchorController>(FindObjectsSortMode.InstanceID);

        for (int i = 0; i < route.Length; i++)
        {
            routeWaypoints[i] = waypoints[Math.Clamp(route[i], 0, waypoints.Length)].transform;
        }

        evalText.transform.parent.parent.parent.parent.parent.gameObject.SetActive(false);
        arrow.gameObject.SetActive(true);
        routeLength = routeWaypoints.Length;
        trialProgress = 0;
        startTime = Time.time;
        routeWaypoints[trialProgress].gameObject.SetActive(true);
        inProgress = true;
    }

    public void ProgressTrial()
    {

        if (!inProgress)
        {
            return;
        }

        routeWaypoints[trialProgress].gameObject.SetActive(false);
        trialProgress++;

        if (trialProgress >= routeLength)
        {
            EndTrial();
        }
        else
        {
            routeWaypoints[trialProgress].gameObject.SetActive(true);
        }


    }


    void EndTrial()
    {
        if (!inProgress)
        {
            return;
        }

        playerAnchor.position = playerStartPosition;
        evalText.transform.parent.parent.parent.parent.parent.gameObject.SetActive(true);
        arrow.gameObject.SetActive(false);

        float trialTime = Time.time - startTime;

        evalText.text = $"Total time: {trialTime}\n\n Press button below to restart trial.";


        inProgress = false;
    }
}
