﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Note to self: (0, 0) is bottom-left.
public class PatternPanel : MonoBehaviour
{
    public ScrollRect scrollRect;
    public Transform patternContainer;
    public GameObject markerTemplate;
    public GameObject lineTemplate;
    public GameObject dottedLineTemplate;
    public GameObject laneDividers;
    public GameObject cursor;

    public const float scanMarkerY = 0f;
    public const float beatMarkerY = -20f;
    public const float timeMarkerY = -40f;
    public const float bpmMarkerY = -60f;
    public const float containerHeight = 480f;

    public static float ScanWidth
    {
        get
        {
            return 1500f * zoom * 0.01f;
        }
    }
    
    private float containerWidth
    {
        get
        {
            return ScanWidth * numScans;
        }
    }

    private int numScans;
    private static int zoom;
    private int divisionsPerBeat;

    public Text divisionsPerBeatDisplay;

    public static event UnityAction RepositionNeeded;

    private void SpawnLine(int scan, GameObject template)
    {
        GameObject line = Instantiate(template, patternContainer);
        line.SetActive(true);

        EditorElement element = line.GetComponent<EditorElement>();
        element.type = EditorElement.Type.Line;
        element.scan = scan;
    }

    private void SpawnDottedLine(int beat, GameObject template)
    {
        GameObject line = Instantiate(template, patternContainer);
        line.SetActive(true);

        EditorElement element = line.GetComponent<EditorElement>();
        element.type = EditorElement.Type.DottedLine;
        element.beat = beat;
    }
    
    private void SpawnScanBasedMarker(EditorElement.Type type, int scan, string text)
    {
        GameObject marker = Instantiate(markerTemplate, patternContainer);
        marker.SetActive(true);
        marker.GetComponentInChildren<Text>().text = text;
        marker.GetComponent<EditorElement>().type = type;

        EditorElement element = marker.GetComponent<EditorElement>();
        element.type = type;
        element.scan = scan;
    }

    private void SpawnBeatBasedMarker(EditorElement.Type type, int beat, string text)
    {
        GameObject marker = Instantiate(markerTemplate, patternContainer);
        marker.SetActive(true);
        marker.GetComponentInChildren<Text>().text = text;
        marker.GetComponent<EditorElement>().type = type;

        EditorElement element = marker.GetComponent<EditorElement>();
        element.type = type;
        element.beat = beat;
    }

    private void ResizeContainer()
    {
        RectTransform containerRect = patternContainer.GetComponent<RectTransform>();
        containerRect.sizeDelta = new Vector3(
            containerWidth,
            containerRect.sizeDelta.y);
    }

    private void SpawnMarkersAndLines()
    {
        for (int i = 0; i < patternContainer.childCount; i++)
        {
            Transform t = patternContainer.GetChild(i);
            if (t == markerTemplate.transform) continue;
            if (t == lineTemplate.transform) continue;
            if (t == dottedLineTemplate.transform) continue;
            if (t == laneDividers.transform) continue;
            if (t == cursor.transform) continue;
            Destroy(t.gameObject);
        }

        Pattern pattern = Navigation.GetCurrentPattern();

        // Scan based stuff
        for (int scan = 0; scan <= numScans; scan++)
        {
            SpawnLine(scan, lineTemplate);
            SpawnScanBasedMarker(EditorElement.Type.ScanMarker,
                scan, $"Scan {scan}");
        }

        // Beat based stuff
        int bps = pattern.patternMetadata.bps;
        double secondsPerBeat = 60.0 / pattern.patternMetadata.initBpm;
        for (int beat = 0; beat < numScans * bps ; beat++)
        {
            float time = (float)(secondsPerBeat * beat +
                pattern.patternMetadata.firstBeatOffset);
            int minute = Mathf.FloorToInt(time / 60f);
            time -= minute * 60f;
            int second = Mathf.FloorToInt(time);
            time -= second;
            int milliSecond = Mathf.FloorToInt(time * 1000f);

            SpawnDottedLine(beat, dottedLineTemplate);
            SpawnBeatBasedMarker(EditorElement.Type.BeatMarker,
                beat, $"Beat {beat}");
            SpawnBeatBasedMarker(EditorElement.Type.TimeMarker,
                beat, $"{minute}:{second:D2}.{milliSecond:D3}");
        }
    }

    private void OnEnable()
    {
        numScans = 4;
        zoom = 100;
        divisionsPerBeat = 2;
        ResizeContainer();
        SpawnMarkersAndLines();
        RepositionNeeded?.Invoke();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.mouseScrollDelta.y != 0)
        {
            if (Input.GetKey(KeyCode.LeftControl) ||
            Input.GetKey(KeyCode.RightControl))
            {
                // Adjust zoom
                zoom += Mathf.FloorToInt(Input.mouseScrollDelta.y * 10f);
                zoom = Mathf.Clamp(zoom, 10, 500);
                // Debug.Log($"zoom={zoom} ScanLength={ScanLength}");
                float horizontal = scrollRect.horizontalNormalizedPosition;

                ResizeContainer();
                RepositionNeeded?.Invoke();

                scrollRect.horizontalNormalizedPosition = horizontal;
            }
            else
            {
                // Scroll
                scrollRect.horizontalNormalizedPosition +=
                    Input.mouseScrollDelta.y * 0.05f;
            }
        }

        // Snap cursor
        Vector2 pointInContainer;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            patternContainer.GetComponent<RectTransform>(),
            Input.mousePosition,
            cam: null,
            out pointInContainer);

        float cursorScan = pointInContainer.x / ScanWidth;
        float cursorBeat = cursorScan *
            Navigation.GetCurrentPattern().patternMetadata.bps;
        float cursorPulse = cursorBeat * Pattern.pulsesPerBeat;
        int pulsesPerDivision = Pattern.pulsesPerBeat / divisionsPerBeat;
        int snappedPulse = Mathf.RoundToInt(cursorPulse / pulsesPerDivision)
            * pulsesPerDivision;
        float snappedScan = (float)snappedPulse / Pattern.pulsesPerBeat
            / Navigation.GetCurrentPattern().patternMetadata.bps;
        float snappedX = snappedScan * ScanWidth;

        int cursorLane = Mathf.FloorToInt((pointInContainer.y + 80f)
            / -100f);
        float snappedY = -130f - 100f * cursorLane;

        if (snappedX >= 0f &&
            snappedX <= containerWidth &&
            cursorLane >= 0 &&
            cursorLane <= 3)
        {
            cursor.SetActive(true);
            cursor.GetComponent<RectTransform>().anchoredPosition =
                new Vector2(snappedX, snappedY);
        }
        else
        {
            cursor.SetActive(false);
        }
    }

    public void ModifyDevisionsPerBeat(int direction)
    {
        do
        {
            divisionsPerBeat += direction;
            if (divisionsPerBeat <= 0 && direction < 0)
            {
                divisionsPerBeat = Pattern.pulsesPerBeat;
            }
            if (divisionsPerBeat > Pattern.pulsesPerBeat && direction > 0)
            {
                divisionsPerBeat = 1;
            }
        }
        while (Pattern.pulsesPerBeat % divisionsPerBeat != 0);
        divisionsPerBeatDisplay.text = divisionsPerBeat.ToString();
    }
}