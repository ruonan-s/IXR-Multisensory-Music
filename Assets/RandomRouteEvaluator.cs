using System.Collections.Generic;
using UnityEngine;

using Text = TMPro.TMP_Text;
using Toggle = UnityEngine.UI.Toggle;


public class RandomRouteEvaluator : MonoBehaviour
{


    public Transform playerTracker;
    public Transform playerAnchor;
    public Transform anchorOrigin;
    public Transform arrow;
    public Text evalText;
    public Toggle startTrialButton;

    public int routeLength;
    public Vector3 playerStartPosition;

    Transform[] routeWaypoints;
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

        routeWaypoints = new Transform[routeLength];
        HashSet<int> seen = new();
        seen.Add(-1);

        int curr = -1;
        for (int i = 0; i < routeLength; i++)
        {

            while (seen.Contains(curr))
            {
                curr = Random.Range(0, anchorOrigin.childCount);
            }
            routeWaypoints[i] = anchorOrigin.GetChild(curr);
            seen.Add(curr);

        }

        evalText.transform.parent.parent.parent.parent.parent.gameObject.SetActive(false);
        arrow.gameObject.SetActive(true);
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
