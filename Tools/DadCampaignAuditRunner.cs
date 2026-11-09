using System;
class DadCampaignAuditRunner
{
    static void Main()
    {
        // Default six fixed seeds per stage: local pacing signal, never a human win-rate estimate.
        Console.Write(SlingshotChecks.RunMainBot(6, 1));
    }
}
