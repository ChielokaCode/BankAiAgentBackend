public class TransferRecord
{
    public DateTime Timestamp { get; set; }
    public required string FromUser { get; set; }
    public required string FromUserName { get; set; } // Added for better reporting
    public required string ToUser { get; set; }
    public required string ToUserName { get; set; }    // Added for better reporting
    public required decimal Amount { get; set; }
    public required string Status { get; set; }       // "Completed", "Failed", etc.
}