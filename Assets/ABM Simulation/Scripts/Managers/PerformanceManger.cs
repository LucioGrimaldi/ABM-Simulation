using System;
using System.Linq;
using System.Threading;

public class PerformanceManger
{
    /// Load Balancing
    public static int SORTING_THRESHOLD = 15;
    public static int MAX_SPS = 60;
    public static int MIN_SPS = 8;
    public int produced_sps = 60;
    public int received_sps = 60;
    public int consumed_sps = 60;
    public int attempted_sps = 60;
    public double consume_rate = 1d;
    public int[] topics = new int[MAX_SPS];

    /// Time related vars
    public long timeout_target_up = 0;
    public long timeout_target_down = 0;
    public long timestamp_last_update = 0;

    public int[] TOPICS { get => topics; set => topics = value; }
    public int PRODUCED_SPS { get => produced_sps; set => produced_sps = value; }
    public int RECEIVED_SPS { get => received_sps; set => received_sps = value; }
    public int CONSUMED_SPS { get => consumed_sps; set => consumed_sps = value; }
    public int ATTEMPTED_SPS { get => attempted_sps; set => attempted_sps = value; }
    public double CONSUME_RATE { get => consume_rate; set => consume_rate = value; }
    public long TIMEOUT_TARGET_UP { get => timeout_target_up; set => timeout_target_up = value; }
    public long TIMEOUT_TARGET_DOWN { get => timeout_target_down; set => timeout_target_down = value; }
    public long TIMESTAMP_LAST_UPDATE { get => timestamp_last_update; set => timestamp_last_update = value; }

    public void CalculatePerformance(CommunicationController CommController, ref Simulation.StateEnum state, ref int stepsConsumed, ref int stepsReceived)
    {
        System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();

        while (true)
        {
            if (state == Simulation.StateEnum.PLAY)
            {
                CalculateConsumeRate(ref stepsConsumed, ref stepsReceived);
                stopwatch.Stop();
                timestamp_last_update = stopwatch.ElapsedMilliseconds;
                stopwatch.Start();
                if (CONSUME_RATE >= 1d && timestamp_last_update > TIMEOUT_TARGET_UP && !(CommController.SimMessageQueue.Count + CommController.SecondaryQueue.Count >= 2 * SORTING_THRESHOLD))
                {
                    ATTEMPTED_SPS = (CommController.SimMessageQueue.Count == 0) ? Math.Min(TOPICS.Length + 2, MAX_SPS) : Math.Min(TOPICS.Length + 1, MAX_SPS);
                    CalculateTargetTopics();
                    CommController.SubscribeOnly(TOPICS);
                }
                else if (CONSUME_RATE < 1d && timestamp_last_update > TIMEOUT_TARGET_DOWN)
                {
                    ATTEMPTED_SPS = (CommController.SimMessageQueue.Count + CommController.SecondaryQueue.Count >= 2*SORTING_THRESHOLD) ? Math.Max(TOPICS.Length - 4, MIN_SPS) : Math.Max(TOPICS.Length - 2, MIN_SPS);
                    CalculateTargetTopics();
                    CommController.SubscribeOnly(TOPICS);
                }
            }
        }
    }
    public void CalculateConsumeRate(ref int stepsConsumed, ref int stepsReceived)
    {
        stepsConsumed = 0;
        stepsReceived = 0;
        Thread.Sleep(500);
        RECEIVED_SPS = stepsReceived * 2;
        CONSUMED_SPS = stepsConsumed * 2;
        CONSUME_RATE = RECEIVED_SPS > 0d ? (CONSUMED_SPS / (double)RECEIVED_SPS) : 0d;
    }
    public void CalculateTargetTopics()
    {
        TOPICS = new int[ATTEMPTED_SPS];
        bool zero = false;

        for (int t = 1; t <= ATTEMPTED_SPS; t++)
        {
            int topic = t * MAX_SPS / (ATTEMPTED_SPS + 1);
            if (topic == 0) zero = true;
            topics[t - 1] = topic;
        }
        if (zero) TOPICS = TOPICS.Skip(1).ToArray();
    }
    public void Reset()
    {
        SORTING_THRESHOLD = 15;
        MAX_SPS = 60;
        MIN_SPS = 15;
        produced_sps = 60;
        received_sps = 60;
        consumed_sps = 60;
        attempted_sps = 60;
        consume_rate = 1d;
        timeout_target_up = 0;
        timeout_target_down = 0;
        timestamp_last_update = 0;
    }
}
