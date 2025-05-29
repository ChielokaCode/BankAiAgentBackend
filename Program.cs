using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using DotNetEnv;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllForTesting", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});


// Load environment variables
Env.Load();

// Add services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Bank AI Agent API", Version = "v1" });
});

// Configure Semantic Kernel
builder.Services.AddSingleton<Kernel>(provider => 
{
    string? endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
    string? modelId = Environment.GetEnvironmentVariable("AZURE_OPENAI_MODEL_ID");
    string? apiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");

    if (endpoint is null || modelId is null || apiKey is null)
    {
        throw new InvalidOperationException("Azure OpenAI credentials not set.");
    }

    var kernelBuilder = Kernel.CreateBuilder()
        .AddAzureOpenAIChatCompletion(modelId, endpoint, apiKey);

    kernelBuilder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Information));
    kernelBuilder.Plugins.AddFromType<BankingPlugin>();
    kernelBuilder.Plugins.AddFromType<FraudDetectionPlugin>();
    
    return kernelBuilder.Build();
});

var app = builder.Build();

// Configure Swagger UI
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

}
app.UseCors("AllowAllForTesting");

// Improved endpoint with better error handling and Swagger documentation
app.MapPost("/chat", async ([FromBody] ChatRequest request, [FromServices] Kernel kernel) =>
{
    if (string.IsNullOrWhiteSpace(request.Message))
    {
        return Results.BadRequest("Message cannot be empty");
    }

    try
    {
        var chatCompletion = kernel.GetRequiredService<IChatCompletionService>();
        var history = new ChatHistory();
        history.AddUserMessage(request.Message);

        var settings = new OpenAIPromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        var stream = chatCompletion.GetStreamingChatMessageContentsAsync(
            history, executionSettings: settings, kernel: kernel);

        string fullMessage = "";
        await foreach (var content in stream)
        {
            fullMessage += content.Content;
        }

        return Results.Ok(new ChatResponse { Message = fullMessage });
    }
    catch (Exception ex)
    {
        return Results.Problem($"An error occurred: {ex.Message}");
    }
})
.WithName("ChatEndpoint");

app.Run();

// Simplified model names
public class ChatRequest
{
    public string? Message { get; set; }
}

public class ChatResponse
{
    public string? Message { get; set; }
}