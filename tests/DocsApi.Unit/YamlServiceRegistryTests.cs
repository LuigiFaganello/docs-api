using DocsApi.Infrastructure.Registry;

namespace DocsApi.Unit;

public class YamlServiceRegistryTests : IDisposable
{
    private readonly string _tempFile = Path.GetTempFileName();

    private void WriteYaml(string content) => File.WriteAllText(_tempFile, content);

    [Fact]
    public void Load_ValidConfig_ReturnsServices()
    {
        WriteYaml("""
            services:
              - id: svc-a
                name: Service A
                specUrl: http://svc-a/spec.json
            """);

        var registry = new YamlServiceRegistry(_tempFile);
        var services = registry.GetAll();

        Assert.Single(services);
        Assert.Equal("svc-a", services[0].Id);
        Assert.Equal("Service A", services[0].Name);
        Assert.Equal(300, services[0].TtlSeconds);
    }

    [Fact]
    public void Load_WithBasicAuth_ParsesCredentials()
    {
        WriteYaml("""
            services:
              - id: secured
                name: Secured
                specUrl: http://secured/spec.json
                auth:
                  type: basic
                  username: user
                  password: pass
            """);

        var registry = new YamlServiceRegistry(_tempFile);
        var svc = registry.GetById("secured");

        Assert.NotNull(svc!.Auth);
        Assert.Equal("user", svc.Auth!.Username);
        Assert.Equal("pass", svc.Auth.Password);
    }

    [Fact]
    public void Load_DuplicateId_Throws()
    {
        WriteYaml("""
            services:
              - id: dup
                name: First
                specUrl: http://first/spec.json
              - id: dup
                name: Second
                specUrl: http://second/spec.json
            """);

        var ex = Assert.Throws<InvalidOperationException>(() => new YamlServiceRegistry(_tempFile));
        Assert.Contains("dup", ex.Message);
    }

    [Fact]
    public void Load_InvalidYaml_Throws()
    {
        WriteYaml("{{ not yaml :");

        Assert.Throws<InvalidOperationException>(() => new YamlServiceRegistry(_tempFile));
    }

    [Fact]
    public void Load_MissingFile_ThrowsFileNotFound()
    {
        Assert.Throws<FileNotFoundException>(() => new YamlServiceRegistry("/nonexistent/services.yml"));
    }

    [Fact]
    public void GetById_CaseInsensitive()
    {
        WriteYaml("""
            services:
              - id: MyService
                name: My Service
                specUrl: http://svc/spec.json
            """);

        var registry = new YamlServiceRegistry(_tempFile);
        Assert.NotNull(registry.GetById("myservice"));
        Assert.NotNull(registry.GetById("MYSERVICE"));
    }

    public void Dispose() => File.Delete(_tempFile);
}
