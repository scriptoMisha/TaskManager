namespace TaskManager.Tests.Infrastructure;

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "Api";
}
