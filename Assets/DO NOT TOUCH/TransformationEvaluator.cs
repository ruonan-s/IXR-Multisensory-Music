using System;
using System.Data;
using UnityEngine;
using Text = TMPro.TMP_Text;


public class TransformationEvaluator : MonoBehaviour
{

    public Vector3 spawnBoxCenter;
    public Vector3 spawnBoxDimensions;

    public Vector2 scaleBounds;

    private float rotationMin = 0f;
    private float rotationMax = 360f;

    private float thresholdDistance = 0.2f;
    private float thresholdScale = 0.2f;
    private float thresholdRotation = 20f;
    
    public GameObject targetGameObject;
    public GameObject sourceGameObject;

    public GameObject evaluationUI;

    public Text evaluationText;

    public LineRenderer target_x;
    public LineRenderer target_y;
    public LineRenderer target_z;

    public LineRenderer source_x;
    public LineRenderer source_y;
    public LineRenderer source_z;


    public int trialAmount;
    private int trialCount = 0;
    private double[] resultsTime;
    private int[] resultsAccuracy;
    public double penalty;
    private DateTime timeStart;

    bool inProgress;

    public Transform GetSourceTransform()
    {
        return sourceGameObject.transform;
    }


    private Vector3 GetRandomPosition()
    {
        Vector3 randomPosition = new Vector3(
            UnityEngine.Random.Range(-spawnBoxDimensions.x / 2f, spawnBoxDimensions.x / 2f) + spawnBoxCenter.x,
            UnityEngine.Random.Range(-spawnBoxDimensions.y / 2f, spawnBoxDimensions.y / 2f) + spawnBoxCenter.y,
            UnityEngine.Random.Range(-spawnBoxDimensions.z / 2f, spawnBoxDimensions.z / 2f) + spawnBoxCenter.z
        );
        return randomPosition;
    }

    private float GetRandomScale()
    {
        return UnityEngine.Random.Range(scaleBounds.x, scaleBounds.y);
    }

    private float GetRandomRotation()
    {
        return UnityEngine.Random.Range(rotationMin, rotationMax);
    }



    private void InitiateTarget()
    {
        targetGameObject.transform.position = GetRandomPosition();
        targetGameObject.transform.localScale = new Vector3(GetRandomScale(), GetRandomScale(), GetRandomScale());
        targetGameObject.transform.rotation = Quaternion.Euler(new Vector3(GetRandomRotation(), GetRandomRotation(), GetRandomRotation()));
    }

    private void InitiateSource()
    {
        sourceGameObject.transform.localScale = Vector3.one;
        sourceGameObject.transform.rotation = Quaternion.Euler(new Vector3(0f,-180f,0f));
        sourceGameObject.transform.position = spawnBoxCenter;
    }


    private void UpdateDebugLog()
    {
        float distance = (targetGameObject.transform.position - sourceGameObject.transform.position).magnitude;
        float diffRotation = (targetGameObject.transform.rotation.eulerAngles - sourceGameObject.transform.rotation.eulerAngles).magnitude;
        float diffScale = (targetGameObject.transform.localScale - sourceGameObject.transform.localScale).magnitude;
    }


    public void StartTrial()
    {
        resultsTime = new double[trialAmount];
        resultsAccuracy = new int[trialAmount];
        evaluationText.text = "";
        evaluationUI.SetActive(false);

        InitiateSource();
        InitiateTarget();
        trialCount = 0;
        timeStart = DateTime.Now;
        inProgress = true;
    }

    public void ConfirmSelection()
    {

        if (!inProgress) {
            return;
        }

        Debug.Log("TIALCOUNT: "+ trialCount);

        float distance = (targetGameObject.transform.position - sourceGameObject.transform.position).magnitude;
        float diffRotation = (targetGameObject.transform.rotation.eulerAngles - sourceGameObject.transform.rotation.eulerAngles).magnitude;
        diffRotation = Mathf.Min(diffRotation, Mathf.Abs(360 - diffRotation));
        float diffScale = (targetGameObject.transform.localScale - sourceGameObject.transform.localScale).magnitude;

        Debug.Log($"Diffs: {distance}, {diffRotation}, {diffScale}");

        if (distance < thresholdDistance && diffRotation < thresholdRotation && diffScale < thresholdScale)
        {
            resultsAccuracy[trialCount] = 0;
        }
        else
        {
            resultsAccuracy[trialCount] = 1;
        }
        DateTime timeNow = DateTime.Now;
        TimeSpan timeDifference = timeNow - timeStart;
        resultsTime[trialCount] = timeDifference.TotalSeconds;
        trialCount++;
        if (trialCount == trialAmount)
        {
            EndTest();
        }
        else
        {
            InitiateSource();
            InitiateTarget();
            timeStart = DateTime.Now;
        }
    }

    private void EndTest()
    {
        inProgress = false;
        string summary = "Average Time: ";
        double sumTime = 0;
        int sumAccuracy = 0;
        foreach (double t in resultsTime)
        {
            sumTime += t;
        }
        summary += (sumTime / trialAmount).ToString() + "\nError: ";
        foreach (int a in resultsAccuracy)
        {
            sumAccuracy += a;
        }
        summary += sumAccuracy.ToString() + "\nTime with Penalty: " + (sumTime / trialAmount + sumAccuracy * penalty).ToString();
        evaluationText.text = summary;
        evaluationUI.SetActive(true);
    }

    private void UpdateTarget()
    {

        target_x.SetPosition(0, targetGameObject.transform.position);
        target_x.SetPosition(1, targetGameObject.transform.right*2f + targetGameObject.transform.position);

        target_y.SetPosition(0, targetGameObject.transform.position);
        target_y.SetPosition(1, targetGameObject.transform.up*2f + targetGameObject.transform.position);

        target_z.SetPosition(0, targetGameObject.transform.position);
        target_z.SetPosition(1, targetGameObject.transform.forward*2f + targetGameObject.transform.position);


        float distance = (targetGameObject.transform.position - sourceGameObject.transform.position).magnitude;
        float diffRotation = (targetGameObject.transform.rotation.eulerAngles - sourceGameObject.transform.rotation.eulerAngles).magnitude;
        float diffScale = (targetGameObject.transform.localScale - sourceGameObject.transform.localScale).magnitude;
        if (distance < thresholdDistance && diffRotation < thresholdRotation && diffScale < thresholdScale)
        {
            targetGameObject.GetComponent<Renderer>().material.color = Color.green;
        }
        else
        {
            targetGameObject.GetComponent<Renderer>().material.color = Color.red;
        }
    }

    private void UpdateSource()
    {
        source_x.SetPosition(0, sourceGameObject.transform.position);
        source_x.SetPosition(1, sourceGameObject.transform.right*2f + sourceGameObject.transform.position);

        source_y.SetPosition(0, sourceGameObject.transform.position);
        source_y.SetPosition(1, sourceGameObject.transform.up*2f + sourceGameObject.transform.position);

        source_z.SetPosition(0, sourceGameObject.transform.position);
        source_z.SetPosition(1, sourceGameObject.transform.forward*2f + sourceGameObject.transform.position);

    }


    void Update()
    {
        UpdateTarget();
        UpdateSource();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0, 0, 0.3f);
        Gizmos.DrawCube(spawnBoxCenter, spawnBoxDimensions);

        Gizmos.color = new Color(0, 0, 1, 0.6f);
        Gizmos.DrawCube(spawnBoxCenter, Vector3.one);
    }

}
