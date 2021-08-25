using UnityEngine;
public class DisplayStats : MonoBehaviour
{
    private float deltaTime = 0.0f;
    private int batch_queue_lenght;
    private int secondary_queue_lenght;
    private long current_step;
    private int target_fps;
    public float fps;
    public float msec;

    void Update()
    {
        //deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        //batch_queue_lenght = GameObject.FindGameObjectWithTag("SimulationController").GetComponent<SimulationController>().SimMessageQueue.Count;
        //secondary_queue_lenght = GameObject.FindGameObjectWithTag("SimulationController").GetComponent<SimulationController>().SecondaryQueue.Count;
        //current_step = GameObject.FindGameObjectWithTag("SimulationController").GetComponent<SimulationController>().LatestSimStepArrived;
        //target_fps = GameObject.FindGameObjectWithTag("SimulationController").GetComponent<SimulationController>().Target_steps;
    }

    void OnGUI()
    {
        int w = Screen.width, h = Screen.height;

        GUIStyle style = new GUIStyle();

        Rect rect = new Rect(h / 50, h * 2 / 5, w, h);
        style.alignment = TextAnchor.UpperLeft;
        style.fontSize = h * 2 / 100;
        style.normal.textColor = new Color(0.0f, 0.0f, 0.5f, 1.0f);
        msec = deltaTime * 1000.0f;
        fps = 1.0f / deltaTime;
        string fps_text = string.Format(
            "{0:0.0} ms ({1:0.} fps)\n" +
            "Concurrent Queue: " + batch_queue_lenght + "\n" +
            "Sorted Queue: " + secondary_queue_lenght + "\n" +
            "Current Step: " + current_step + "\n" +
            "Target: " + target_fps + "\n",
             msec, fps);
        GUI.Label(rect, fps_text, style);
    }
}