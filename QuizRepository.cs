using Microsoft.EntityFrameworkCore;
using AI.Quiz.Function.Data;
using AI.Quiz.Function.Models;
using System.Security.Principal;

namespace AI.Quiz.Function
{
    public class QuizRepository
    {
        private readonly QuizDbContext _context;

        public QuizRepository(QuizDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Validates a user's username and password combination
        /// </summary>
        /// <param name="username">The username to validate</param>
        /// <param name="password">The password to validate</param>
        /// <returns>True if the user credentials are valid, false otherwise</returns>
        public async Task<bool?> ValidateUser(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return false;
            }

            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == username && u.Password == password);

                return user != null;
            }
            catch (Exception)
            {
                // Log the exception in a real application
                return null;
            }
        }

        /// <summary>
        /// Gets all quiz categories from the database
        /// </summary>
        /// <returns>A list of all quiz categories</returns>
        public async Task<List<QuizCategory>> GetAllQuizCategories()
        {
            try
            {
                return await _context.Categories
                    .OrderBy(qc => qc.Category)
                    .ToListAsync();
            }
            catch (Exception)
            {
                // Log the exception in a real application
                return new List<QuizCategory>();
            }
        }

        /// <summary>
        /// Gets a random set of quiz questions for a specific category
        /// </summary>
        /// <param name="category">The category to filter by</param>
        /// <param name="count">The number of random questions to return</param>
        /// <returns>A list of random quiz questions for the specified category</returns>
        public async Task<List<QuizQuestion>> GetQuizByCategory(string category, int count)
        {
            if (string.IsNullOrWhiteSpace(category) || count <= 0)
            {
                return new List<QuizQuestion>();
            }

            try
            {
                // Use raw SQL with NEWID() for proper randomization
                var sql = @"
                    SELECT TOP ({0}) [id], [category], [question], [option_a], [option_b], [option_c], [option_d], [option_e], [answer]
                    FROM [Quiz].[Questions] 
                    WHERE [category] = {1}
                    ORDER BY NEWID()";

                var quiz = await _context.Questions
                    .FromSqlRaw(sql, count, category)
                    .ToListAsync();
                quiz.ForEach(q => q.Answer = EncodeAnswer(q.Id, q.Answer));
                return quiz;
            }
            catch (Exception)
            {
                // Log the exception in a real application
                return new List<QuizQuestion>();
            }
        }

        /// <summary>
        /// Gets a user by username (useful for additional operations)
        /// </summary>
        /// <param name="username">The username to search for</param>
        /// <returns>The user if found, null otherwise</returns>
        public async Task<User?> GetUserByUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return null;
            }

            try
            {
                return await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == username);
            }
            catch (Exception)
            {
                // Log the exception in a real application
                return null;
            }
        }

        /// <summary>
        /// Checks if a category exists in the database
        /// </summary>
        /// <param name="category">The category to check</param>
        /// <returns>True if the category exists, false otherwise</returns>
        public async Task<bool> CategoryExists(string category)
        {
            if (string.IsNullOrWhiteSpace(category))
            {
                return false;
            }

            try
            {
                return await _context.Categories
                    .AnyAsync(qc => qc.Category == category);
            }
            catch (Exception)
            {
                // Log the exception in a real application
                return false;
            }
        }

        /// <summary>
        /// Gets the total count of questions for a specific category
        /// </summary>
        /// <param name="category">The category to count questions for</param>
        /// <returns>The number of questions in the category</returns>
        public async Task<int> GetQuestionCountByCategory(string category)
        {
            if (string.IsNullOrWhiteSpace(category))
            {
                return 0;
            }

            try
            {
                return await _context.Questions
                    .CountAsync(q => q.Category == category);
            }
            catch (Exception)
            {
                // Log the exception in a real application
                return 0;
            }
        }

        /// <summary>
        /// Gets all users from the database with passwords removed for security
        /// </summary>
        /// <returns>A list of all users with empty password fields</returns>
        public async Task<List<User>> GetAllUsers()
        {
            try
            {
                var users = await _context.Users.ToListAsync();
                
                // Clear passwords before returning
                users.ForEach(u => u.Password = string.Empty);
                
                return users;
            }
            catch (Exception)
            {
                // Log the exception in a real application
                return new List<User>();
            }
        }

        /// <summary>
        /// Changes a user's password after validating the old password
        /// </summary>
        /// <param name="username">The username of the user</param>
        /// <param name="oldPassword">The current password for validation</param>
        /// <param name="newPassword">The new password to set</param>
        /// <returns>True if password was changed successfully, false if validation failed, null if an error occurred</returns>
        public async Task<bool?> ChangePassword(string username, string oldPassword, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(oldPassword) ||
                string.IsNullOrWhiteSpace(newPassword))
            {
                return false;
            }

            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == username && u.Password == oldPassword);

                if (user == null)
                {
                    return false; // User not found or old password incorrect
                }

                user.Password = newPassword;
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception)
            {
                // Log the exception in a real application
                return null;
            }
        }
        
                /// <summary>
        /// Changes a user's password after validating the old password
        /// </summary>
        /// <param name="username">The username of the user</param>
        /// <param name="newPassword">The new password to set</param>
        /// <returns>True if password was changed successfully, false if validation failed, null if an error occurred</returns>
        public async Task<bool?> ResetPassword(string username, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(newPassword))
            {
                return false;
            }

            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == username);

                if (user == null)
                {
                    return false; // User not found
                }

                user.Password = newPassword;
                await _context.SaveChangesAsync();
                
                return true;
            }
            catch (Exception)
            {
                // Log the exception in a real application
                return null;
            }
        }

        /// <summary>
        /// Deletes a user from the database
        /// </summary>
        /// <param name="username">The username of the user to delete</param>
        /// <returns>True if user was deleted, false if user not found, null if an error occurred</returns>
        public async Task<bool?> DeleteUser(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return false;
            }

            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == username);

                if (user == null)
                {
                    return false; // User not found
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                
                return true;
            }
            catch (Exception)
            {
                // Log the exception in a real application
                return null;
            }
        }

        /// <summary>
        /// Creates a new user in the database
        /// </summary>
        /// <param name="user">The user object to create</param>
        /// <returns>The created user with password cleared, null if creation failed</returns>
        public async Task<User?> CreateUser(User user)
        {
            if (user == null || 
                string.IsNullOrWhiteSpace(user.Username) || 
                string.IsNullOrWhiteSpace(user.Password))
            {
                return null;
            }

            try
            {
                // Check if username already exists
                var existingUser = await _context.Users
                    .AnyAsync(u => u.Username == user.Username);

                if (existingUser)
                {
                    return null; // Username already exists
                }

                // Set creation timestamp
                user.CreatedAt = DateTime.UtcNow;

                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                
                // Clear password before returning
                user.Password = string.Empty;
                
                return user;
            }
            catch (Exception)
            {
                // Log the exception in a real application
                return null;
            }
        }

        private string EncodeAnswer(int id, string answer)
        {
            if (string.IsNullOrEmpty(answer))
            {
                return string.Empty;
            }

            // Simple encoding logic (for demonstration purposes)
            var bytes = System.Text.Encoding.UTF8.GetBytes(id.ToString() + answer);
            return Convert.ToBase64String(bytes);
        }
    }
}