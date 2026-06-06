using DbUp;

// DbUp migration runner (ADR-0010). Packaged as the one-shot `migrate` container.
// It applies any .sql scripts in /migrations that haven't run yet, tracking them
// in a SchemaVersions table. Re-running is safe: applied scripts are skipped.

var connectionString =
    Environment.GetEnvironmentVariable("POSTGRES_CONN")
    ?? "Host=postgres;Username=orders;Password=orders;Database=orders";

EnsureDatabase.For.PostgresqlDatabase(connectionString);

var upgrader = DeployChanges.To
    .PostgresqlDatabase(connectionString)
    .WithScriptsFromFileSystem("/migrations")
    .LogToConsole()
    .Build();

var result = upgrader.PerformUpgrade();
if (!result.Successful)
{
    Console.Error.WriteLine(result.Error);
    return 1;
}

Console.WriteLine("Migrations complete.");
return 0;
