namespace PlumbBuddy.Data;

public sealed class SQLiteBusyTimeoutConnectionInterceptor(TimeSpan busyTimeout) :
    IDbConnectionInterceptor
{
    readonly string busyTimeoutPragmaCommandText = $"PRAGMA busy_timeout = {(int)busyTimeout.TotalMilliseconds};";

    DbCommand CreateBusyTimeoutPragmaCommand(DbConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);
        var pragmaCommand = connection.CreateCommand();
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
        pragmaCommand.CommandText = busyTimeoutPragmaCommandText;
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities
        return pragmaCommand;
    }

    void IDbConnectionInterceptor.ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        using var pragmaCommand = CreateBusyTimeoutPragmaCommand(connection);
        pragmaCommand.ExecuteNonQuery();
    }

    async Task IDbConnectionInterceptor.ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken)
    {
        using var pragmaCommand = CreateBusyTimeoutPragmaCommand(connection);
        await pragmaCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
