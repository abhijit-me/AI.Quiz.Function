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
        public async Task<List<QuizCategories>> GetAllQuizCategories()
        {
            try
            {
                return await _context.QuizCategories
                    .OrderBy(qc => qc.Category)
                    .ToListAsync();
            }
            catch (Exception)
            {
                // Log the exception in a real application
                return new List<QuizCategories>();
            }
        }

        /// <summary>
        /// Gets a random set of quiz questions for a specific category
        /// </summary>
        /// <param name="category">The category to filter by</param>
        /// <param name="count">The number of random questions to return</param>
        /// <returns>A list of random quiz questions for the specified category</returns>
        public async Task<List<Models.Quiz>> GetQuizByCategory(string category, int count)
        {
            if (string.IsNullOrWhiteSpace(category) || count <= 0)
            {
                return new List<Models.Quiz>();
            }

            try
            {
                // Use raw SQL with NEWID() for proper randomization
                var sql = @"
                    SELECT TOP ({0}) [id], [category], [question], [option_a], [option_b], [option_c], [option_d], [option_e], [answer]
                    FROM [Quiz] 
                    WHERE [category] = {1}
                    ORDER BY NEWID()";

                var quiz = await _context.Quiz
                    .FromSqlRaw(sql, count, category)
                    .ToListAsync();
                quiz.ForEach(q => q.Answer = EncodeAnswer(q.Id, q.Answer));
                return quiz;
            }
            catch (Exception)
            {
                // Log the exception in a real application
                return new List<Models.Quiz>();
            }
        }

        /// <summary>
        /// Gets a user by username (useful for additional operations)
        /// </summary>
        /// <param name="username">The username to search for</param>
        /// <returns>The user if found, null otherwise</returns>
        public async Task<Users?> GetUserByUsername(string username)
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
                return await _context.QuizCategories
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
                return await _context.Quiz
                    .CountAsync(q => q.Category == category);
            }
            catch (Exception)
            {
                // Log the exception in a real application
                return 0;
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