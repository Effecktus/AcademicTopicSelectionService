using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace AcademicTopicSelectionService.Infrastructure.Data;

public partial class ApplicationDbContext
{
    /// <summary>
    /// Части схемы, которые невозможно записать одинаково для PostgreSQL и SQL Server.
    /// Всё остальное (CHECK, индексы, длины, связи) в модели общее.
    /// </summary>
    /// <param name="CaseInsensitiveText">
    /// Тип регистронезависимой строки. В SQL Server <c>null</c>: <c>nvarchar(n)</c> по длине,
    /// регистронезависимость даёт collation базы (<see cref="SqlServerCollation"/>).
    /// </param>
    /// <param name="Timestamp">Тип даты и времени в UTC.</param>
    /// <param name="NewGuid">Генерация UUID по умолчанию (для вставок из SQL-скриптов).</param>
    /// <param name="UtcNow">Текущее время в UTC по умолчанию (для вставок из SQL-скриптов).</param>
    private sealed record SqlDialect(string? CaseInsensitiveText, string Timestamp, string NewGuid, string UtcNow)
    {
        /// <summary>
        /// Collation базы SQL Server: регистронезависимое сравнение и правильная сортировка кириллицы.
        /// </summary>
        public const string SqlServerCollation = "Cyrillic_General_CI_AS";

        public static readonly SqlDialect Postgres = new("citext", "timestamp with time zone", "gen_random_uuid()", "CURRENT_TIMESTAMP");

        // CURRENT_TIMESTAMP в SQL Server — локальное время сервера, поэтому SYSUTCDATETIME().
        public static readonly SqlDialect SqlServer = new(null, "datetime2", "NEWSEQUENTIALID()", "SYSUTCDATETIME()");

        public static SqlDialect For(DatabaseFacade database) => database.IsSqlServer() ? SqlServer : Postgres;
    }

    /// <summary>
    /// Настройки уровня базы, специфичные для провайдера.
    /// </summary>
    private void ConfigureDatabase(ModelBuilder modelBuilder)
    {
        if (Database.IsSqlServer())
        {
            modelBuilder.UseCollation(SqlDialect.SqlServerCollation);
            return;
        }

        modelBuilder
            .HasPostgresExtension("citext")
            .HasPostgresExtension("pgcrypto");
    }
}
