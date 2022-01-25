﻿using UnityEngine;
public class DisplayStats : MonoBehaviour
{
    private int batch_queue_length;
    private int secondary_queue_length;
    private long latestStepArrived;
    private long currentStep;
    private int topics;
    private double produced_sps;
    private int received_sps;
    private int consumed_sps;
    private double consume_rate;
    public Color text_color;

    void Update()
    {
        batch_queue_length = GameObject.FindGameObjectWithTag("SimulationController").GetComponent<SimulationController>().CommController.SimMessageQueue.Count;
        secondary_queue_length = GameObject.FindGameObjectWithTag("SimulationController").GetComponent<SimulationController>().CommController.SecondaryQueue.Count;
        latestStepArrived = GameObject.FindGameObjectWithTag("SimulationController").GetComponent<SimulationController>().LatestSimStepArrived;
        currentStep = GameObject.FindGameObjectWithTag("SimulationController").GetComponent<SimulationController>().GetSimulation().CurrentSimStep;
        topics = GameObject.FindGameObjectWithTag("SimulationController").GetComponent<SimulationController>().PerfManager.TOPICS.Length;
        produced_sps = GameObject.FindGameObjectWithTag("SimulationController").GetComponent<SimulationController>().PerfManager.PRODUCED_SPS;
        received_sps = GameObject.FindGameObjectWithTag("SimulationController").GetComponent<SimulationController>().PerfManager.RECEIVED_SPS;
        consumed_sps = GameObject.FindGameObjectWithTag("SimulationController").GetComponent<SimulationController>().PerfManager.CONSUMED_SPS;
        consume_rate = GameObject.FindGameObjectWithTag("SimulationController").GetComponent<SimulationController>().PerfManager.CONSUME_RATE;
    }

    void OnGUI()
    {
        int w = Screen.width, h = Screen.height;

        GUIStyle style = new GUIStyle();

        Rect rect = new Rect(40, h/3, w, h);
        style.alignment = TextAnchor.UpperLeft;
        style.fontSize = 24;
        style.normal.textColor = text_color;
        string debug_infos = string.Format(
            "Concurrent Queue: " + batch_queue_length + "\n" +
            "Sorted Queue: " + secondary_queue_length + "\n" +
            "\n" +
            "Latest Step Arrived: " + latestStepArrived + "\n" +
            "Current Step: " + currentStep + "\n" +
            "\n" +
            "TOPICS: " + topics + "\n" +
            "Produced Steps/s: " + produced_sps + "\n" +
            "Received Steps/s: " + received_sps + "\n" +
            "Consumed Steps/s: " + consumed_sps + "\n" +
            "Consume Rate: " + consume_rate + "\n");
        GUI.Label(rect, debug_infos, style);
    }
}