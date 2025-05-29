using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;

public class BankingPlugin
{
    private static Dictionary<string, Account> Accounts = new Dictionary<string, Account>();
    private List<TransferRecord> TransferHistory = new List<TransferRecord>();


    [KernelFunction, Description("Creates a new bank account using the provided full name, address, phone, email and balance")]
public string CreateAccount(
    [Description("Name of the user")] string fullName,
    [Description("Address or Location of the user")] string address,
    [Description("Phone number of the user")] string phone,
    [Description("Email of the user")] string email,
    [Description("Initial Balance of the account")] decimal balance)
{
    // 1. First check for fraudulent behavior
    if (CheckFraudulentAccountCreation(fullName, address, phone, email, balance))
    {
        return "Account creation denied: This account has been flagged as potentially fraudulent!";
    }

    // 2. Check if account already exists
    if (Accounts.ContainsKey(email))
    {
        return $"Account creation failed: An account already exists for {email}";
    }

    // 3. Create new account
    var account = new Account
    {
        FullName = fullName,
        Address = address,
        Phone = phone,
        Email = email,
        Balance = balance
    };
    
    // 4. Store the full account object
    Accounts[email] = account;
    
    return $"Account successfully created for {fullName} with starting balance ₦{balance:N2}";
}

    [KernelFunction, Description("Views all account information for a given email")]
    public string ViewAccount(
    [Description("Email of the user to view account")] string email)
    {
        // 1. Try to get the full account object
        if (Accounts.TryGetValue(email, out Account account))
        {
            return $"Account found:\n" +
                   $"• Name: {account.FullName}\n" +
                   $"• Email: {account.Email}\n" +
                   $"• Phone: {account.Phone}\n" +
                   $"• Address: {account.Address}\n" +
                   $"• Balance: {account.Balance:C}";
        }
        else
        {
            return $"No account found with email: {email}";
        }
    }

    //Check fraudulent transfer
 [KernelFunction, Description(
    "Detects potentially fraudulent transfers by analyzing transaction patterns. " +
    "Checks for: (1) Amounts significantly higher than the sender's historical average (3x threshold), " +
    "Returns true if any fraud indicators are triggered."
)]
public bool checkFraudulentTransfers(
        [Description("Email of the sender")] string fromUser,
        [Description("Email of the recipient")] string toUser,
        [Description("Amount to transfer")] decimal amount)
    {
        // 1. Basic Validation Check
        if (amount <= 0)
        {
            return true; // Fraudulent - invalid amount
        }

        // Get all transfers from this sender, ordered by most recent
        var senderTransfers = TransferHistory
            .Where(t => t.FromUser.Equals(fromUser, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(t => t.Timestamp)
            .ToList();

        // 2. Unusually Large Amount Check
        if (senderTransfers.Count >= 3)
        {
            decimal averageAmount = senderTransfers.Take(3).Average(t => t.Amount);

            if (amount > averageAmount * 3)
            {
                return true; // Fraudulent - unusually large amount
            }
        }

        // If all checks pass
        return false;
    }

    ///Transfer
    [KernelFunction, Description("Transfers money between accounts")]
    public string TransferMoney(
    [Description("Email of the sender")] string fromUser,
    [Description("Email of the recipient")] string toUser,
    [Description("Amount to transfer")] decimal amount)
    {
        // 1. First check account existence
        if (!Accounts.TryGetValue(fromUser, out Account fromAccount))
        {
            return $"Sender account {fromUser} not found.";
        }

        if (!Accounts.TryGetValue(toUser, out Account toAccount))
        {
            return $"Recipient account {toUser} not found.";
        }

        // 2. Now check for fraudulent behavior (after we have account objects)
        if (checkFraudulentTransfers(fromUser, toUser, amount))
        {
            TransferHistory.Add(new TransferRecord
            {
                Timestamp = DateTime.UtcNow,
                FromUser = fromUser,
                FromUserName = fromAccount.FullName, 
                ToUser = toUser,
                ToUserName = toAccount.FullName,
                Amount = amount,
                Status = "Blocked (Fraud)"
            });
            return "Transfer denied: Transfer has been flagged as potentially fraudulent! Forwarding to Human analyst";
        }

        // 3. Check balance
        if (fromAccount.Balance < amount)
        {
            return $"Insufficient balance in {fromUser}'s account.";
        }

        // 4. Execute transfer
        fromAccount.Balance -= amount;
        toAccount.Balance += amount;

        TransferHistory.Add(new TransferRecord
        {
            Timestamp = DateTime.UtcNow,
            FromUser = fromUser,
            FromUserName = fromAccount.FullName,
            ToUser = toUser,
            ToUserName = toAccount.FullName,
            Amount = amount,
            Status = "Completed"
        });

        return $"Transferred ₦{amount:N2} from {fromAccount.FullName} to {toAccount.FullName}";
    }


    [KernelFunction, Description("Check for fraudulent account creation based on email format, name validity, and other risk factors")]
    public bool CheckFraudulentAccountCreation(
    [Description("Name of the user")] string fullName,
    [Description("Address or Location of the user")] string address,
    [Description("Phone number of the user")] string phone,
    [Description("Email of the user")] string email,
    [Description("Initial Balance of the account")] decimal balance)
{
    // 1. Email Validation
    bool isSuspiciousEmail = !email.EndsWith("@ext.com", StringComparison.OrdinalIgnoreCase);
    
    // 2. Name Validation
    bool hasNumbersInName = fullName.Any(char.IsDigit);
    bool hasConsecutiveSpecialChars = fullName.Count(c => !char.IsLetterOrDigit(c)) > 2;
    
    // 3. Phone Validation
    bool isInvalidPhone = phone.Length < 10 || !phone.All(char.IsDigit);
    
    // 4. Balance Check
    bool isUnusuallyHighInitialBalance = balance > 100000m; // $100,000 threshold
    
    // 5. Address Validation (basic check)
    bool isSuspiciousAddress = string.IsNullOrWhiteSpace(address) || address.Length < 10;
    
    // 6. Email Username Part Check
    string emailUser = email.Split('@')[0];
    bool hasRandomStringPattern = 
        emailUser.Length > 20 || 
        emailUser.Count(char.IsDigit) > 5 ||
        emailUser.Count(c => !char.IsLetterOrDigit(c)) > 3;
    
    // 7. Name-Email Mismatch
    bool nameEmailMismatch = !fullName.Split(' ').Any(name => 
        emailUser.Contains(name, StringComparison.OrdinalIgnoreCase));
    
    // Composite fraud score (adjust thresholds as needed)
    int fraudScore = 0;
    fraudScore += isSuspiciousEmail ? 2 : 0;
    fraudScore += hasNumbersInName ? 3 : 0;
    fraudScore += hasConsecutiveSpecialChars ? 2 : 0;
    fraudScore += isInvalidPhone ? 2 : 0;
    fraudScore += isUnusuallyHighInitialBalance ? 3 : 0;
    fraudScore += isSuspiciousAddress ? 1 : 0;
    fraudScore += hasRandomStringPattern ? 2 : 0;
    fraudScore += nameEmailMismatch ? 1 : 0;
    
    // Mark as fraudulent if score exceeds threshold
    return fraudScore >= 5; // Adjust this threshold based on your risk tolerance
}

}
