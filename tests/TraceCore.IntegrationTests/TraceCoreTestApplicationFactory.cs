using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TraceCore.Infrastructure.Persistence.InMemory;

namespace TraceCore.IntegrationTests;

public class TraceCoreTestApplicationFactory : WebApplicationFactory<Program>
{
    // Bloco 7.A.0: Program.cs (top-level statements) chama AddInfrastructure(builder.Configuration)
    // ANTES de builder.Build() — nesse ponto, um IWebHostBuilder.ConfigureAppConfiguration
    // registrado aqui pode chegar tarde demais para vencer appsettings.Development.json
    // (que agora tem Provider=MySql de propósito, ADR — Persistência). Definir a env var
    // do processo garante que WebApplication.CreateBuilder(args) já carregue
    // appsettings.Testing.json (Provider=InMemory) desde o início do cascateamento padrão
    // de configuração do ASP.NET Core, sem depender de timing interno do WebApplicationFactory.
    static TraceCoreTestApplicationFactory()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Mantido como reforço defensivo — não é mais a única linha de defesa.
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Persistence:Provider"] = "InMemory"
            });
        });

        builder.ConfigureServices(services =>
        {
            // InMemoryDataStore já provê seed inicial isolado
        });
    }

    public void ResetDatabase()
    {
        using var scope = Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<InMemoryDataStore>();
        store.Reset();
    }
}
