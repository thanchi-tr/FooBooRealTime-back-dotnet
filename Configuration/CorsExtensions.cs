namespace QuizApp.Configurations
{
    public static class CorsExtensions
    {
        public static IServiceCollection ConfigureCors(this IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("AllowSpecificOrigin", policy =>
                {
                    policy.WithOrigins("https://localhost:3000") // Allow your frontend origin
                          .AllowAnyMethod() // Allow any HTTP method (GET, POST, etc.)
                          .AllowAnyHeader() // Allow any headers (e.g., Content-Type, Authorization)
                          .AllowCredentials(); // If you need cookies or authorization headers
                });
            });

            return services;
        }
    }
}
