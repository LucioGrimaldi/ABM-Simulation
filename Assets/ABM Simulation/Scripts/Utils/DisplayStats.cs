using UnityEngine;
public class DisplayStats : MonoBehaviour
{
    private int batch_queue_lenght;
    private int secondary_queue_lenght;
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
        batch_queue_lenght = GameObject.FindGameObjectWithTag("SimulationController").GetComponent<SimulationController>().CommController.SimMessageQueue.Count;
        secondary_queue_lenght = GameObject.FindGameObjectWithTag("SimulationController").GetComponent<SimulationController>().CommController.SecondaryQueue.Count;
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

        Rect rect = new Rect(w / 5f, 20, w, h);
        style.alignment = TextAnchor.UpperLeft;
        style.fontSize = 30;
        style.normal.textColor = text_color;
        string debug_infos = string.Format(
            "Concurrent Queue: " + batch_queue_lenght + "\n" +
            "Sorted Queue: " + secondary_queue_lenght + "\n" +
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