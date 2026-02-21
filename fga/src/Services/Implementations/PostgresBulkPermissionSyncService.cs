namespace ODataFga.Services.Implementations;

using Microsoft.EntityFrameworkCore;
using Npgsql;
using ODataFga.Database;
using ODataFga.Models;
using ODataFga.Services;
using System.Data;
using System.Transactions;

public class PostgresBulkPermissionSyncService : IPermissionSyncService
{
    public async Task SyncForUserAsync(AppDbContext db, string userId, IEnumerable<PermissionIndex> permissions, CancellationToken ct = default)
    {
        using TransactionScope scope = new TransactionScope(
            TransactionScopeOption.Required,
            new TransactionOptions { IsolationLevel = System.Transactions.IsolationLevel.ReadCommitted },
            TransactionScopeAsyncFlowOption.Enabled);

        NpgsqlConnection conn = (NpgsqlConnection)db.Database.GetDbConnection();

        if (conn.State != ConnectionState.Open)
        {
            await conn.OpenAsync(ct);
        }

        using (NpgsqlCommand cmd = new NpgsqlCommand("DELETE FROM \"Permissions\" WHERE \"UserId\" = @u", conn))
        {
            cmd.Parameters.AddWithValue("u", userId);
            await cmd.ExecuteNonQueryAsync(ct);
        }

        if (permissions.Any())
        {
            const string copySql = "COPY \"Permissions\" (\"ObjectType\", \"ObjectId\", \"UserId\", \"PermissionMask\") FROM STDIN (FORMAT BINARY)";

            using var writer = await conn.BeginBinaryImportAsync(copySql, ct);

            foreach (var p in permissions)
            {
                await writer.StartRowAsync(ct);
                await writer.WriteAsync(p.ObjectType, ct);
                await writer.WriteAsync(p.ObjectId, ct);
                await writer.WriteAsync(p.UserId, ct);
                await writer.WriteAsync(p.PermissionMask, ct);
            }
            await writer.CompleteAsync(ct);
        }

        scope.Complete();
    }

    public async Task SyncForObjectAsync(AppDbContext db, string objectType, string objectId, IEnumerable<PermissionIndex> permissions, CancellationToken ct = default)
    {
        using TransactionScope scope = new TransactionScope(
            TransactionScopeOption.Required,
            new TransactionOptions { IsolationLevel = System.Transactions.IsolationLevel.ReadCommitted },
            TransactionScopeAsyncFlowOption.Enabled);

        NpgsqlConnection conn = (NpgsqlConnection)db.Database.GetDbConnection();

        if (conn.State != ConnectionState.Open)
        {
            await conn.OpenAsync(ct);
        }

        using (NpgsqlCommand cmd = new NpgsqlCommand("DELETE FROM \"Permissions\" WHERE \"ObjectType\" = @t AND \"ObjectId\" = @i", conn))
        {
            cmd.Parameters.AddWithValue("t", objectType);
            cmd.Parameters.AddWithValue("i", objectId);
            await cmd.ExecuteNonQueryAsync(ct);
        }

        if (permissions.Any())
        {
            using var writer = await conn.BeginBinaryImportAsync("COPY \"Permissions\" (\"ObjectType\", \"ObjectId\", \"UserId\", \"PermissionMask\") FROM STDIN (FORMAT BINARY)", ct);
            foreach (var p in permissions)
            {
                await writer.StartRowAsync(ct);
                await writer.WriteAsync(p.ObjectType, ct);
                await writer.WriteAsync(p.ObjectId, ct);
                await writer.WriteAsync(p.UserId, ct);
                await writer.WriteAsync(p.PermissionMask, ct);
            }
            await writer.CompleteAsync(ct);
        }

        scope.Complete();
    }
}