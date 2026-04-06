namespace oop_s2_1_mvc_79189.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next,
            ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unhandled exception on {Method} {Path} by user {User}",
                    context.Request.Method,
                    context.Request.Path,
                    context.User?.Identity?.Name ?? "Anonymous");

                context.Response.Redirect("/Home/Error");
            }
        }
    }
}
