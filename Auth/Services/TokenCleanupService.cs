using Auth.Data;
using Auth.Models;
using Microsoft.EntityFrameworkCore;
namespace Auth.Services;

public class TokenCleanupService : IHostedService, IDisposable
{

    private readonly ILogger<TokenCleanupService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private Timer _timer;

    public TokenCleanupService(IServiceScopeFactory scopeFactory, ILogger<TokenCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public void Dispose()
    {
        _timer?.Dispose();
        /*
        The issue is that the Dispose method in the TokenCleanupService class
        does not call GC.SuppressFinalize(this). This is a good practice when
        implementing the IDisposable interface.

        GC.SuppressFinalize(this) is used to prevent the garbage collector from
        calling the finalizer (also known as the destructor) of the object.
        When an object implements IDisposable, it's likely that it has some
        unmanaged resources that need to be cleaned up, and the finalizer is
        not necessary.

        By calling GC.SuppressFinalize(this), you're telling the garbage
        collector that the object has already been properly cleaned up and
        there's no need to call the finalizer. This can improve performance
        and prevent potential issues.
        */
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     Starts the hosted service.
    ///     This method is called when the hosted service is started. It creates a new timer that
    ///     runs the <see cref="CleanUpExpiredTokens" /> method every hour.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token that indicates when the service should stop.</param>
    /// <returns>A task that represents the start operation.</returns>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Create a new timer that runs the CleanUpExpiredTokens method every hour.
        // The first argument is the method to be executed.
        // The second argument is the state object that is passed to the method.
        // The third argument is the initial delay before the first execution.
        // The fourth argument is the period between subsequent executions.
        _timer = new Timer(
                           CleanUpExpiredTokens,
                           null,
                           TimeSpan.Zero,
                           TimeSpan.FromHours(1)
                          );

        // Return a completed task because starting the service is asynchronous and does not need to be awaited.
        _timer = new Timer(CleanUpExpiredTokens, null, TimeSpan.Zero, TimeSpan.FromHours(1));
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Cleans up expired refresh tokens from the database.
    ///     This method is executed periodically by the <see cref="TokenCleanupService" />.
    /// </summary>
    /// <param name="state">The state object passed to the method.</param>
    private void CleanUpExpiredTokens(object? state)
    {
        // Create a new scope for database operations.
        using IServiceScope scope = _scopeFactory.CreateScope();

        // Get the instance of the LocalDbContext.
        ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Get the list of expired refresh tokens from the database.
        List<RefreshToken> expiredTokens = context.RefreshTokens
                                                  .Where(rt => rt.ExpiryDate < DateTime.UtcNow)
                                                  .ToList();

        // Remove the expired tokens from the database.
        context.RefreshTokens.RemoveRange(expiredTokens);

        try
        {
            context.SaveChanges();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "A concurrency error occurred while trying to remove expired tokens !! Potential security breach with refresh tokens. Remove manually refresh token from database. ");
        }
    }
}