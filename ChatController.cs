// using Microsoft.AspNetCore.Mvc;
// using Microsoft.SemanticKernel;
// using Microsoft.SemanticKernel.ChatCompletion;
// using Microsoft.SemanticKernel.Connectors.OpenAI;
// using Microsoft.Extensions.Logging;
// using System;
// using System.Threading.Tasks;
// using System;
// using System.ComponentModel;
// using System.IO;
// using System.Collections.Generic;

// using DotNetEnv;


// [ApiController]
//     [Route("api/[controller]")]
//     public class ChatController : ControllerBase
//     {
//         private readonly IChatCompletionService _chatCompletion;
//         private readonly Kernel _kernel; // Changed from IKernel to Kernel
//         private const string ChatHistorySessionKey = "ChatHistory";

//     public ChatController()
//     {
//         // Load environment variables
//         Env.Load();

//         string? endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
//         string? modelId = Environment.GetEnvironmentVariable("AZURE_OPENAI_MODEL_ID");
//         string? apiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");

//         if (endpoint is null || modelId is null || apiKey is null)
//         {
//             throw new Exception("Azure OpenAI credentials not set.");
//         }

//         var builder = Kernel.CreateBuilder()
//             .AddAzureOpenAIChatCompletion(modelId, endpoint, apiKey);

//         builder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Information));

//         _kernel = builder.Build();
//         _chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();

//         builder.Plugins.AddFromType<BankingPlugin>();
//         builder.Plugins.AddFromType<MathSolver>();
//         }

//         [HttpPost]
//         public async Task<IActionResult> Post([FromBody] ChatRequest request)
//         {

//             if (request == null || string.IsNullOrWhiteSpace(request.UserInput))
//             {
//                 return BadRequest("User input is required");
//             }

//             // Get or create chat history from session
//             var history = new ChatHistory();

//             // Add user message to history
//             history.AddUserMessage(request.UserInput);

//             // var settings = new OpenAIPromptExecutionSettings
//             // {
//             //     ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
//             // };

//             var settings = new OpenAIPromptExecutionSettings
//             {
//                 FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
//             };

//         // Get AI response
//         var result = await _chatCompletion.GetChatMessageContentAsync(history, executionSettings: settings, kernel: _kernel);

//             if (result.Content is null)
//             {
//                 return BadRequest(new { error = "AI service returned no response" });
//             }
//             // Add AI response to history
//             history.AddAssistantMessage(result.Content ?? "I didn't get a response from the AI service");

//             return Ok(new { reply = result?.Content });
//         }
//     }
//     // math Solving
//     public class MathSolver
//     {
//         [KernelFunction, Description("Solves a basic math expression like '5 + 3'.")]
//         public string Solve(string expression)
//         {
//             try
//             {
//                 var result = new System.Data.DataTable().Compute(expression, null);
//                 return $"{expression} = {result}";
//             }
//             catch
//             {
//                 return $"Could not solve: {expression}";
//             }
//         }
//     }


// //     public class BankingPlugin
// // {
// //     private static Dictionary<string, decimal> Accounts = new();

// //     [KernelFunction, Description("Creates a new bank account using the provided full name, address, phone, email and balance")]
// //     public string CreateAccount(
// //         [Description("Name of the user")]string fullName,
// //         [Description("Address or Location of the user")]string address,
// //         [Description("Phone number of the user")]string phone,
// //         [Description("Email of the user")]string email,
// //         [Description("Initial Balance of the account")]decimal balance)
// //     {
// //         var account = new Account
// //         {
// //             FullName = fullName,
// //             Address = address,
// //             Phone = phone,
// //             Email = email,
// //             Balance = balance
// //         };
// //         Accounts[email] = balance;
// //         return $"Account successfully created for {fullName}.";;
// //     }

// //     [KernelFunction, Description("View Account Details")]
// //     public string viewAccount(
// //     [Description("Email of the user to view account")] string email)
// //     {
// //         if (Accounts.TryGetValue(email, out decimal balance))
// //         {
// //             return $"Account found. Balance for account with email {email}: {balance:C}";
// //         }
// //         else
// //         {
// //             return $"No account found with email: {email}";
// //         }
// //     }

// //     [KernelFunction, Description("Transfers money between two user accounts.")]
// //     public string TransferMoney(string fromUser, string toUser, decimal amount)
// //     {
// //         if (!Accounts.ContainsKey(fromUser)) return $"Sender account {fromUser} not found.";
// //         if (!Accounts.ContainsKey(toUser)) return $"Recipient account {toUser} not found.";
// //         if (Accounts[fromUser] < amount) return $"Insufficient balance in {fromUser}'s account.";

// //         Accounts[fromUser] -= amount;
// //         Accounts[toUser] += amount;

// //         return $"Transfer of {amount:C} from {fromUser} to {toUser} completed.";
// //     }

// //     [KernelFunction, Description("Registers biometric data to user's account.")]
// //     public string RegisterBiometric(string userId)
// //     {
// //         // Stub logic
// //         return $"Biometric data registered successfully for {userId}.";
// //     }

// //     [KernelFunction, Description("Initiates real-time face scan for user KYC.")]
// //     public string RunFaceScan(string userId)
// //     {
// //         // Placeholder for live facial verification (connect to actual service in future)
// //         return $"Live face scan completed for {userId}. KYC verified.";
// //     }
// // }
