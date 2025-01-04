namespace PlumbBuddy.Data;

public sealed class SQLiteWalConnectionInterceptor :
    IDbConnectionInterceptor
{
    static DbCommand CreateJournalModePragmaCommand(DbConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);
        var pragmaCommand = connection.CreateCommand();
        pragmaCommand.CommandText = "PRAGMA journal_mode=WAL;";
        return pragmaCommand;
    }

    void IDbConnectionInterceptor.ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        using var pragmaCommand = CreateJournalModePragmaCommand(connection);
        pragmaCommand.ExecuteNonQuery();
    }

    async Task IDbConnectionInterceptor.ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken)
    {
        using var pragmaCommand = CreateJournalModePragmaCommand(connection);
        await pragmaCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
