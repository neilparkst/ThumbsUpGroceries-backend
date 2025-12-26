using System;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ThumbsUpGroceries_backend.Service
{
    public class CleanupTrolleySlotsService : BackgroundService
    {
        private readonly ILogger<CleanupTrolleySlotsService> _logger;
        private readonly string _connectionString;
        private readonly TimeSpan _interval = TimeSpan.FromMinutes(10);

        public CleanupTrolleySlotsService(IConfiguration configuration, ILogger<CleanupTrolleySlotsService> logger)
        {
            _logger = logger;
            _connectionString = configuration["ConnectionStrings:DefaultConnection"]
                ?? throw new ArgumentNullException("ConnectionStrings:DefaultConnection");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("CleanupTrolleySlotsService starting.");
            using var timer = new PeriodicTimer(_interval);

            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    await RunCleanupAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException) { /* shutting down */ }

            _logger.LogInformation("CleanupTrolleySlotsService stopping.");
        }

        private async Task RunCleanupAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Cleanup started at {UtcNow}", DateTime.UtcNow);

            // update using aggregated counts per slot (safe if multiple records target same slot)
            var sqlUpdate = @"
                UPDATE T
                SET T.SlotCount = T.SlotCount + COALESCE(s.RecordCount, 0)
                FROM TrolleyTimeSlot T
                INNER JOIN (
                    SELECT TimeSlotId, COUNT(*) AS RecordCount
                    FROM TrolleyTimeSlotRecord
                    WHERE CreatedAt < DATEADD(MINUTE, -10, GETUTCDATE())
                    GROUP BY TimeSlotId
                ) s
                ON s.TimeSlotId = T.SlotId;
            ";

            // delete expired records
            var sqlDelete = @"
                DELETE FROM TrolleyTimeSlotRecord
                WHERE CreatedAt < DATEADD(MINUTE, -10, GETUTCDATE());
            ";

            try
            {
                await using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);

                await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

                try
                {
                    var updateCount = await connection.ExecuteAsync(sqlUpdate, transaction: transaction);
                    var deleteCount = await connection.ExecuteAsync(sqlDelete, transaction: transaction);

                    var rowsAffected = updateCount + deleteCount;

                    await transaction.CommitAsync(cancellationToken);

                    _logger.LogInformation("Cleanup completed. Rows affected: {RowsAffected} (updated: {UpdateCount}, deleted: {DeleteCount})",
                        rowsAffected, updateCount, deleteCount);
                }
                catch (Exception)
                {
                    try
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        _logger.LogInformation("Cleanup transaction rolled back.");
                    }
                    catch (Exception rbEx)
                    {
                        _logger.LogError(rbEx, "Rollback failed during trolley slot cleanup");
                    }
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Trolley slot cleanup");
            }
        }
    }
}