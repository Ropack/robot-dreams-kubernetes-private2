using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Polly;

namespace NorthwindStore.Tests.IntegrationTests;

public class EnsureDatabaseReadyFixture : IDisposable
{
    public IConfigurationRoot Configuration { get; }

    public EnsureDatabaseReadyFixture()
    {
        var builder = new ConfigurationBuilder()
            .AddJsonFile("testsettings.json")
            .AddEnvironmentVariables();
        Configuration = builder.Build();

        Policy.Handle<SqlException>().Or<DatabaseNotReadyException>()
            .WaitAndRetry(10, a => TimeSpan.FromSeconds(5))
            .Execute(() =>
            {
                using var con = new SqlConnection(Configuration.GetConnectionString("DB"));
                con.Open();

                using var cmd = new SqlCommand("SELECT COUNT(*) FROM Territories", con);
                if ((int)cmd.ExecuteScalar() == 0)
                {
                    throw new DatabaseNotReadyException();
                }

                // wait another 2 seconds to be really sure
                Thread.Sleep(2000);
            });
    }

    public void Dispose()
    {
        // ... clean up test data from the database ...
    }
        
}

[CollectionDefinition("Database collection")]
public class DatabaseCollection : ICollectionFixture<EnsureDatabaseReadyFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}

public class DatabaseNotReadyException : Exception
{
}