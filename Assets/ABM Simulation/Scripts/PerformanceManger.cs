using System.Collections;
using System.Collections.Generic;
using System.Threading;

public class PerformanceManger
{
    private Thread performanceMonitorThread;
    /// Load Balancing
    private int TARGET_FPS = 60;
    private int[] targetsArray = new int[] { 15, 30, 45, 60 };
    private int[][] topicsArray;
    private long TIMEOUT_TARGET_UP = 3000;
    private long TIMEOUT_TARGET_DOWN = 1000;
    private long timestampLastUpdate = 0;

    /// Benchmarking
    private long start_time;
    private float fps;
    private float deltaTime = 0f;

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
        topicsArray = new int[][] { arrayTarget15, arrayTarget30, arrayTarget45, arrayTarget60 };
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

        System.Diagnostics.Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        while (true)
        {
            if (TARGET_FPS < 60 && fps > targetsArray[Array.IndexOf(targetsArray, TARGET_FPS) + 1])
            {
                stopwatch.Stop();
                timestampLastUpdate = stopwatch.ElapsedMilliseconds;
                stopwatch.Start();
                if (timestampLastUpdate > TIMEOUT_TARGET_UP)
                {
                    //controllare il target attuale
                    int index = Array.IndexOf(targetsArray, TARGET_FPS);
                    TARGET_FPS = targetsArray[++index];
                    //prendiamo l'array corretto in base al target aggiornato
                    CommController.SubscribeTopics(topicsArray[index]);
                    stopwatch.Restart();
                }
            }
            else if (TARGET_FPS > 15 && fps + 1 < TARGET_FPS)
            {
                stopwatch.Stop();
                timestampLastUpdate = stopwatch.ElapsedMilliseconds;
                stopwatch.Start();
                if (timestampLastUpdate > TIMEOUT_TARGET_DOWN)
                {
                    //controllare il target attuale
                    int index = Array.IndexOf(targetsArray, TARGET_FPS);
                    TARGET_FPS = targetsArray[index - 1];
                    //prendiamo l'array corretto in base al target aggiornato
                    CommController.UnsubscribeTopics(topicsArray[index]);
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
