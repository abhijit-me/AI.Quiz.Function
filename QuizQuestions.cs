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

    [Function("GetAllUsers")]
    public async Task<IActionResult> GetAllUsers([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req)
    {
        _logger.LogInformation("GetAllUsers function processed a request.");

        try
        {
            var users = await _quizRepository.GetAllUsers();
            
            return new OkObjectResult(new { 
                users = users,
                count = users.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users");
            return new StatusCodeResult(500);
        }
    }

    [Function("ChangePassword")]
    public async Task<IActionResult> ChangePassword([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
    {
        _logger.LogInformation("ChangePassword function processed a request.");

        try
        {
            // Read request body
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            if (string.IsNullOrEmpty(requestBody))
            {
                return new BadRequestObjectResult(new { error = "Request body is required" });
            }

            // Parse JSON
            var data = JsonSerializer.Deserialize<ChangePasswordRequest>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (data == null ||
                string.IsNullOrWhiteSpace(data.Username) ||
                string.IsNullOrWhiteSpace(data.OldPassword) ||
                string.IsNullOrWhiteSpace(data.NewPassword))
            {
                return new BadRequestObjectResult(new { error = "Username, old password, and new password are required" });
            }

            // Change password
            bool? success = await _quizRepository.ChangePassword(data.Username, data.OldPassword, data.NewPassword);

            if (success == null)
            {
                return new StatusCodeResult(500);
            }

            if (!success.Value)
            {
                return new BadRequestObjectResult(new { error = "Invalid username or old password" });
            }

            return new OkObjectResult(new
            {
                success = true,
                message = "Password changed successfully"
            });
        }
        catch (JsonException)
        {
            return new BadRequestObjectResult(new { error = "Invalid JSON format" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password");
            return new StatusCodeResult(500);
        }
    }
    
    [Function("ResetPassword")]
    public async Task<IActionResult> ResetPassword([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
    {
        _logger.LogInformation("ResetPassword function processed a request.");

        try
        {
            // Read request body
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            
            if (string.IsNullOrEmpty(requestBody))
            {
                return new BadRequestObjectResult(new { error = "Request body is required" });
            }

            // Parse JSON
            var data = JsonSerializer.Deserialize<ChangePasswordRequest>(requestBody, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });

            if (data == null || 
                string.IsNullOrWhiteSpace(data.Username) || 
                string.IsNullOrWhiteSpace(data.NewPassword))
            {
                return new BadRequestObjectResult(new { error = "Username and new password are required" });
            }

            // Reset password
            bool? success = await _quizRepository.ResetPassword(data.Username, data.NewPassword);

            if (success == null)
            {
                return new StatusCodeResult(500);
            }

            if (!success.Value)
            {
                return new BadRequestObjectResult(new { error = "Invalid username" });
            }

            return new OkObjectResult(new {
                success = true,
                message = "Password reset successfully"
            });
        }
        catch (JsonException)
        {
            return new BadRequestObjectResult(new { error = "Invalid JSON format" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password");
            return new StatusCodeResult(500);
        }
    }

    [Function("DeleteUser")]
    public async Task<IActionResult> DeleteUser([HttpTrigger(AuthorizationLevel.Function, "delete")] HttpRequest req)
    {
        _logger.LogInformation("DeleteUser function processed a request.");

        try
        {
            // Get username from query parameter
            string? username = req.Query["username"];

            if (string.IsNullOrWhiteSpace(username))
            {
                return new BadRequestObjectResult(new { error = "Username parameter is required" });
            }

            // Delete user
            bool? success = await _quizRepository.DeleteUser(username);

            if (success == null)
            {
                return new StatusCodeResult(500);
            }

            if (!success.Value)
            {
                return new NotFoundObjectResult(new { error = $"User '{username}' not found" });
            }

            return new OkObjectResult(new {
                success = true,
                message = $"User '{username}' deleted successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user");
            return new StatusCodeResult(500);
        }
    }

    [Function("CreateUser")]
    public async Task<IActionResult> CreateUser([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
    {
        _logger.LogInformation("CreateUser function processed a request.");

        try
        {
            // Read request body
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            
            if (string.IsNullOrEmpty(requestBody))
            {
                return new BadRequestObjectResult(new { error = "Request body is required" });
            }

            // Parse JSON
            var data = JsonSerializer.Deserialize<CreateUserRequest>(requestBody, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });

            if (data == null || 
                string.IsNullOrWhiteSpace(data.Username) || 
                string.IsNullOrWhiteSpace(data.Password))
            {
                return new BadRequestObjectResult(new { error = "Username and password are required" });
            }

            // Create user object
            var newUser = new Models.Users
            {
                Username = data.Username,
                Password = data.Password
            };

            // Create user
            var createdUser = await _quizRepository.CreateUser(newUser);

            if (createdUser == null)
            {
                return new BadRequestObjectResult(new { error = "User creation failed. Username may already exist." });
            }

            return new OkObjectResult(new {
                success = true,
                message = "User created successfully",
                user = createdUser
            });
        }
        catch (JsonException)
        {
            return new BadRequestObjectResult(new { error = "Invalid JSON format" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
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

public class ChangePasswordRequest
{
    public string Username { get; set; } = string.Empty;
    public string OldPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public class CreateUserRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}