using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using AI.Quiz.Function;

namespace AI.Quiz.Function;

public class QuizQuestions
{
    private readonly ILogger<QuizQuestions> _logger;
    private readonly QuizRepository _quizRepository;

    public QuizQuestions(ILogger<QuizQuestions> logger, QuizRepository quizRepository)
    {
        _logger = logger;
        _quizRepository = quizRepository;
    }

    [Function("ValidateUser")]
    public async Task<IActionResult> ValidateUser([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
    {
        _logger.LogInformation("ValidateUser function processed a request.");

        try
        {
            // Read request body
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            
            if (string.IsNullOrEmpty(requestBody))
            {
                return new BadRequestObjectResult(new { error = "Request body is required" });
            }

            // Parse JSON
            var data = JsonSerializer.Deserialize<ValidateUserRequest>(requestBody, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });

            if (data == null || string.IsNullOrWhiteSpace(data.Username) || string.IsNullOrWhiteSpace(data.Password))
            {
                return new BadRequestObjectResult(new { error = "Username and password are required" });
            }

            // Validate user
            bool? isValid = await _quizRepository.ValidateUser(data.Username, data.Password);

            return new OkObjectResult(new {
                isValid,
                message = isValid == true ? "User validation successful" : (isValid == false ? "Invalid credentials" : "User validation failed")
            });
        }
        catch (JsonException)
        {
            return new BadRequestObjectResult(new { error = "Invalid JSON format" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating user");
            return new StatusCodeResult(500);
        }
    }

    [Function("GetQuizCategories")]
    public async Task<IActionResult> GetQuizCategories([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req)
    {
        _logger.LogInformation("GetQuizCategories function processed a request.");

        try
        {
            var categories = await _quizRepository.GetAllQuizCategories();
            
            return new OkObjectResult(new { 
                categories = categories,
                count = categories.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving quiz categories");
            return new StatusCodeResult(500);
        }
    }

    [Function("GetQuizByCategory")]
    public async Task<IActionResult> GetQuizByCategory([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req)
    {
        _logger.LogInformation("GetQuizByCategory function processed a request.");

        try
        {
            // Get query parameters
            string? category = req.Query["category"];
            string? countStr = req.Query["count"];

            if (string.IsNullOrWhiteSpace(category))
            {
                return new BadRequestObjectResult(new { error = "Category parameter is required" });
            }

            if (!int.TryParse(countStr, out int count) || count <= 0)
            {
                return new BadRequestObjectResult(new { error = "Count parameter must be a positive integer" });
            }

            // Validate category exists
            bool categoryExists = await _quizRepository.CategoryExists(category);
            if (!categoryExists)
            {
                return new NotFoundObjectResult(new { error = $"Category '{category}' not found" });
            }

            // Get quiz questions
            var quizQuestions = await _quizRepository.GetQuizByCategory(category, count);
            
            // Get total available questions for this category
            int totalAvailable = await _quizRepository.GetQuestionCountByCategory(category);

            return new OkObjectResult(new { 
                category = category,
                requestedCount = count,
                actualCount = quizQuestions.Count,
                totalAvailable = totalAvailable,
                questions = quizQuestions
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving quiz questions by category");
            return new StatusCodeResult(500);
        }
    }

    [Function("QuizQuestions")]
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");
        return new OkObjectResult("Welcome to Azure Functions!");
    }
}

// Request models
public class ValidateUserRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}