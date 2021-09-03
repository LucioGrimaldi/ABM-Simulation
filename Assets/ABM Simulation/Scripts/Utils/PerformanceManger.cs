using System;
using System.Threading;
using UnityEngine;

public class PerformanceManger
{
    private Thread performanceMonitorThread;

    /// Load Balancing
    public int target_fps = 60;
    public int[] targets_array = new int[] { 15, 30, 45, 60 };
    public int[][] topics_array;
    public long timeout_target_up = 3000;
    public long timeout_target_down = 1000;
    public long timestamp_last_update = 0;

    /// Benchmarking
    public long start_time;
    public float fps;
    public float deltaTime = 0f;

    public int TARGET_FPS { get => target_fps; set => target_fps = value; }
    public int[] Targets_array { get => targets_array; set => targets_array = value; }
    public int[][] Topics_array { get => topics_array; set => topics_array = value; }
    public long TIMEOUT_TARGET_UP { get => timeout_target_up; set => timeout_target_up = value; }
    public long TIMEOUT_TARGET_DOWN { get => timeout_target_down; set => timeout_target_down = value; }
    public long Timestamp_last_update { get => timestamp_last_update; set => timestamp_last_update = value; }

    public void PerformanceMonitor()
    {
        performanceMonitorThread = new Thread(this.CalculatePerformance);
        performanceMonitorThread.Start();
    }

    public void CalculatePerformance()
    {
        int[] arrayTarget60 = new int[60];
        int[] arrayTarget45 = new int[45];
        int[] arrayTarget30 = new int[30];
        int[] arrayTarget15 = new int[15];
        topics_array = new int[][] { arrayTarget15, arrayTarget30, arrayTarget45, arrayTarget60 };
        //Fill the array incrementally without repeating common values from others target arrays 
        for (int i = 0, y = 0, z = 0, k = 0; i < TARGET_FPS; i++)
        {
            if ((i % 4) == 3)
            {
                arrayTarget60[i] = i;
            }
            if ((i % 4) == 1)
            {
                arrayTarget45[y] = i;
                y++;
            }

            if ((i % 4) == 2)
            {
                arrayTarget30[z] = i;
                z++;
            }
            if ((i % 4) == 0)
            {
                arrayTarget15[k] = i;
                k++;
            }
        }

        System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();
        while (true)
        {
            if (TARGET_FPS < 60 && fps > targets_array[Array.IndexOf(targets_array, TARGET_FPS) + 1])
            {
                stopwatch.Stop();
                timestamp_last_update = stopwatch.ElapsedMilliseconds;
                stopwatch.Start();
                if (timestamp_last_update > TIMEOUT_TARGET_UP)
                {
                    //controllare il target attuale
                    int index = Array.IndexOf(targets_array, TARGET_FPS);
                    TARGET_FPS = targets_array[++index];
                    //prendiamo l'array corretto in base al target aggiornato
                    //CommController.SubscribeTopics(topics_array[index]);
                    stopwatch.Restart();
                }
            }
            else if (TARGET_FPS > 15 && fps + 1 < TARGET_FPS)
            {
                stopwatch.Stop();
                timestamp_last_update = stopwatch.ElapsedMilliseconds;
                stopwatch.Start();
                if (timestamp_last_update > TIMEOUT_TARGET_DOWN)
                {
                    //controllare il target attuale
                    int index = Array.IndexOf(targets_array, TARGET_FPS);
                    TARGET_FPS = targets_array[index - 1];
                    //prendiamo l'array corretto in base al target aggiornato
                    //CommController.UnsubscribeTopics(topics_array[index]);
                    stopwatch.Restart();
                }
            }
            Thread.Sleep(500);
        }
    }

    public void CalculateFps()
    {
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        fps = 1.0f / deltaTime;//da prendere da display stats
    }
}
