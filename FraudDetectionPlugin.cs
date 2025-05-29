using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.SemanticKernel;

public class FraudDetectionPlugin
{
    private static Dictionary<string, List<decimal>> transferLogs = new();

    [KernelFunction, Description("Checks if current transfer fits user’s past patterns.")]
    public string CheckTransferPattern(string userId, decimal currentAmount)
    {
        if (!transferLogs.ContainsKey(userId) || transferLogs[userId].Count < 3)
        {
            return "⚠️ Not enough history to evaluate pattern. Proceed with caution.";
        }

        var history = transferLogs[userId];
        var average = history.Average();
        var stdDev = Math.Sqrt(history.Select(x => Math.Pow((double)(x - average), 2)).Average());
        decimal threshold = (decimal)(2 * stdDev);


        if (Math.Abs(currentAmount - average) > threshold)
        {
            return $"🚨 Pattern anomaly: Current amount {currentAmount:C} deviates from normal range.";
        }

        return $"✅ Transfer {currentAmount:C} matches user {userId}'s historical pattern.";
    }

    [KernelFunction, Description("Detects spikes and prevents suspicious transfers.")]
    public string DetectAmountSpike(string userId, decimal currentAmount)
    {
        if (!transferLogs.ContainsKey(userId)) transferLogs[userId] = new List<decimal>();

        var history = transferLogs[userId];
        history.Add(currentAmount);

        if (history.Count < 3)
            return $"⚠️ New user, added {currentAmount:C} to history.";

        var avg = history.Take(history.Count - 1).Average();

        decimal spikeThreshold = avg * 3m; // 3m means "3 as decimal"
        if (currentAmount > spikeThreshold)
        {
            history.RemoveAt(history.Count - 1); // rollback spike
            return $"🚫 Transfer blocked! {currentAmount:C} is a 3x spike over average ({avg:C}).\n🧑‍💼 Action Required: {HumanInLoopReview(userId, currentAmount)}";
        }

        return $"✅ No spike detected. {currentAmount:C} added to history.";
    }

    [KernelFunction, Description("Analyzes account data for fraud risk and prevents creation.")]
    public string PreventAccountCreation(string userId, string email, string ipAddress)
    {
        if (email.Contains("tempmail") || ipAddress.StartsWith("192.168.0."))
        {
            return $"🚫 Account flagged: suspicious email/IP. {userId} denied.";
        }

        return $"✅ Account creation for {userId} allowed.";
    }

    [KernelFunction, Description("Sends fraud alert to monitoring system.")]
    public string SendFraudAlert(string userId)
    {
        return $"📣 Fraud alert triggered for user {userId}. Incident logged.";
    }

    [KernelFunction, Description("Sends biometric or facial challenge to verify user identity.")]
    public string ChallengeUser(string userId)
    {
        return $"⚠️ Verification challenge issued to user {userId}.";
    }

    [KernelFunction, Description("Escalates incident to human reviewer.")]
    public string HumanInLoopReview(string userId, decimal amount)
    {
        return $"🧠 Human analyst has been notified to review suspicious transfer of {amount:C} by {userId}. Awaiting decision.";
    }
}
