using System;
using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace TraceCore.Infrastructure.Migrations;

public class DatabaseMigrationRunner
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseMigrationRunner> _logger;

    public DatabaseMigrationRunner(IServiceProvider serviceProvider, ILogger<DatabaseMigrationRunner> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Bloco 7.A.0 §4.4: a aplicação precisa conseguir subir do zero usando somente
    /// migrations, inclusive criando o banco caso ele ainda não exista (bootstrap em
    /// banco MySQL limpo). Conecta sem "Database=" no builder, cria o schema com
    /// CREATE DATABASE IF NOT EXISTS e utf8mb4, e só então as migrations (que assumem
    /// o banco já existente) podem rodar.
    /// </summary>
    public void EnsureDatabaseCreated(string connectionString)
    {
        var builder = new MySqlConnectionStringBuilder(connectionString);
        var databaseName = builder.Database;
        if (string.IsNullOrWhiteSpace(databaseName))
        {
            throw new InvalidOperationException("Connection string de MySQL não informa um banco de dados (Database=).");
        }

        builder.Database = string.Empty;

        using var connection = new MySqlConnection(builder.ConnectionString);
        connection.Open();

        // ADR-0002 (revisado, Fase 7.A): o ambiente real disponível é MariaDB 10.5, não
        // MySQL 8.4 LTS. utf8mb4_unicode_ci é compatível com ambos (MariaDB não tem
        // utf8mb4_0900_ai_ci, exclusiva do MySQL 8.0+).
        using var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE IF NOT EXISTS `{databaseName}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;";
        command.ExecuteNonQuery();

        _logger.LogInformation("Banco de dados '{Database}' verificado/criado com sucesso.", databaseName);
    }

    public void MigrateUp()
    {
        using var scope = _serviceProvider.CreateScope();
        var runner = scope.ServiceProvider.GetService<IMigrationRunner>();
        if (runner == null)
        {
            _logger.LogWarning("Migration runner not registered or skipped.");
            return;
        }

        try
        {
            _logger.LogInformation("Iniciando execução de migrations do banco de dados...");
            runner.MigrateUp();
            _logger.LogInformation("Migrations executadas com sucesso.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao executar migrations no banco de dados.");
            throw;
        }
    }
}
