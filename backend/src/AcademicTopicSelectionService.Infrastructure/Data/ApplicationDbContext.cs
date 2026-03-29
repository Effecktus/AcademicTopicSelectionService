using AcademicTopicSelectionService.Domain.Common;
using AcademicTopicSelectionService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AcademicTopicSelectionService.Infrastructure.Data;

/// <summary>
/// Контекст базы данных приложения. Управляет подключением к PostgreSQL
/// и предоставляет доступ ко всем сущностям системы.
/// </summary>
/// <remarks>
/// Автоматически устанавливает <see cref="IAuditableEntity.CreatedAt"/> при добавлении
/// и <see cref="IAuditableEntity.UpdatedAt"/> при изменении сущностей.
/// </remarks>
public partial class ApplicationDbContext : DbContext
{
    /// <summary>
    /// Создаёт новый экземпляр контекста базы данных.
    /// </summary>
    /// <param name="options">Параметры конфигурации контекста.</param>
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Асинхронно сохраняет все изменения в базу данных.
    /// Автоматически устанавливает даты аудита для сущностей <see cref="IAuditableEntity"/>.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Количество записей, затронутых операцией.</returns>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditInfo();
        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Синхронно сохраняет все изменения в базу данных.
    /// Автоматически устанавливает даты аудита для сущностей <see cref="IAuditableEntity"/>.
    /// </summary>
    /// <returns>Количество записей, затронутых операцией.</returns>
    public override int SaveChanges()
    {
        ApplyAuditInfo();
        return base.SaveChanges();
    }

    /// <summary>
    /// Устанавливает даты аудита для отслеживаемых сущностей.
    /// </summary>
    private void ApplyAuditInfo()
    {
        var utcNow = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = utcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = utcNow;
                    break;
            }
        }
    }

    public virtual DbSet<AcademicDegree> AcademicDegrees { get; set; }

    public virtual DbSet<AcademicTitle> AcademicTitles { get; set; }

    public virtual DbSet<ApplicationStatus> ApplicationStatuses { get; set; }

    public virtual DbSet<ChatMessage> ChatMessages { get; set; }

    public virtual DbSet<Department> Departments { get; set; }

    public virtual DbSet<GraduateWork> GraduateWorks { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<NotificationType> NotificationTypes { get; set; }

    public virtual DbSet<Position> Positions { get; set; }

    public virtual DbSet<Student> Students { get; set; }

    public virtual DbSet<StudyGroup> StudyGroups { get; set; }

    public virtual DbSet<StudentApplication> StudentApplications { get; set; }

    public virtual DbSet<Teacher> Teachers { get; set; }

    public virtual DbSet<Topic> Topics { get; set; }

    public virtual DbSet<TopicCreatorType> TopicCreatorTypes { get; set; }

    public virtual DbSet<TopicStatus> TopicStatuses { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserRole> UserRoles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasPostgresExtension("citext")
            .HasPostgresExtension("pgcrypto");

        modelBuilder.Entity<AcademicDegree>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("AcademicDegrees_pkey");

            entity.ToTable(tb => tb.HasComment("Справочник ученых степеней. Содержит системные, отображаемые и сокращенные названия степеней."));

            entity.HasIndex(e => e.CodeName, "AcademicDegrees_CodeName_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasComment("Уникальный идентификатор ученой степени");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("Дата и время создания записи о степени")
                .HasColumnType("timestamp with time zone");
            entity.Property(e => e.DisplayName)
                .HasMaxLength(100)
                .HasComment("Отображаемое значение степени (для пользовательского интерфейса)");
            entity.Property(e => e.CodeName)
                .HasComment("Системное значение степени (для кода), регистронезависимо")
                .HasColumnType("citext");
            entity.Property(e => e.ShortName)
                .HasMaxLength(50)
                .HasComment("Сокращенное название степени (для отображения в кратких формах)");
            entity.Property(e => e.UpdatedAt)
                .HasComment("Дата и время последнего обновления записи о степени")
                .HasColumnType("timestamp with time zone");
        });

        modelBuilder.Entity<AcademicTitle>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("AcademicTitles_pkey");

            entity.ToTable(tb => tb.HasComment("Справочник ученых званий. Содержит системные и отображаемые названия званий."));

            entity.HasIndex(e => e.CodeName, "AcademicTitles_CodeName_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasComment("Уникальный идентификатор ученого звания");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("Дата и время создания записи о звании")
                .HasColumnType("timestamp with time zone");
            entity.Property(e => e.DisplayName)
                .HasMaxLength(100)
                .HasComment("Отображаемое значение звания (для пользовательского интерфейса)");
            entity.Property(e => e.CodeName)
                .HasComment("Системное значение звания (для кода), регистронезависимо")
                .HasColumnType("citext");
            entity.Property(e => e.UpdatedAt)
                .HasComment("Дата и время последнего обновления записи о звании")
                .HasColumnType("timestamp with time zone");
        });

        modelBuilder.Entity<ApplicationStatus>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("ApplicationStatuses_pkey");

            entity.ToTable(tb => tb.HasComment("Справочник статусов заявок на темы ВКР. Содержит системные и отображаемые названия статусов."));

            entity.HasIndex(e => e.CodeName, "ApplicationStatuses_CodeName_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasComment("Уникальный идентификатор статуса заявки");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("Дата и время создания записи о статусе")
                .HasColumnType("timestamp with time zone");
            entity.Property(e => e.DisplayName)
                .HasMaxLength(100)
                .HasComment("Отображаемое значение статуса (для пользовательского интерфейса)");
            entity.Property(e => e.CodeName)
                .HasComment("Системное значение статуса (для кода), регистронезависимо")
                .HasColumnType("citext");
            entity.Property(e => e.UpdatedAt)
                .HasComment("Дата и время последнего обновления записи о статусе")
                .HasColumnType("timestamp with time zone");
        });

        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("ChatMessages_pkey");

            entity.ToTable(tb => tb.HasComment("Таблица сообщений чата между студентами и преподавателями по заявкам на темы ВКР. Содержит историю переписки и информацию о прочтении сообщений."));

            entity.HasIndex(e => e.ApplicationId, "IX_ChatMessages_ApplicationId");

            entity.HasIndex(e => e.SentAt, "IX_ChatMessages_SentAt");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasComment("Уникальный идентификатор сообщения");
            entity.Property(e => e.ApplicationId).HasComment("Идентификатор заявки, к которой относится сообщение (внешний ключ к таблице Applications)");
            entity.Property(e => e.Content).HasComment("Текст сообщения");
            entity.Property(e => e.ReadAt)
                .HasComment("Дата и время прочтения сообщения получателем (NULL, если сообщение не прочитано)")
                .HasColumnType("timestamp with time zone");
            entity.Property(e => e.SenderId).HasComment("Идентификатор отправителя сообщения (внешний ключ к таблице Users)");
            entity.Property(e => e.SentAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("Дата и время отправки сообщения")
                .HasColumnType("timestamp with time zone");

            entity.HasOne(d => d.Application).WithMany(p => p.ChatMessages)
                .HasForeignKey(d => d.ApplicationId)
                .HasConstraintName("FK_ChatMessages_StudentApplications");

            entity.HasOne(d => d.Sender).WithMany(p => p.ChatMessages)
                .HasForeignKey(d => d.SenderId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_ChatMessages_Users");
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Departments_pkey");

            entity.ToTable(tb => tb.HasComment("Таблица кафедр. Содержит информацию о кафедрах и их заведующих."));

            entity.HasIndex(e => e.CodeName, "UQ_Departments_CodeName").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasComment("Уникальный идентификатор кафедры");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("Дата и время создания записи о кафедре")
                .HasColumnType("timestamp with time zone");
            entity.Property(e => e.HeadId).HasComment("Идентификатор заведующего кафедрой (внешний ключ к таблице Users)");
            entity.Property(e => e.CodeName)
                .HasComment("Системное значение кафедры (для кода), регистронезависимо")
                .HasColumnType("citext");

            entity.Property(e => e.DisplayName)
                .HasMaxLength(255)
                .HasComment("Отображаемое значение кафедры (для пользовательского интерфейса)");
            entity.Property(e => e.UpdatedAt)
                .HasComment("Дата и время последнего обновления записи о кафедре")
                .HasColumnType("timestamp with time zone");

            entity.HasOne(d => d.Head).WithMany(p => p.Departments)
                .HasForeignKey(d => d.HeadId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Departments_Users_Head");
        });

        modelBuilder.Entity<GraduateWork>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("GraduateWorks_pkey");

            entity.ToTable(tb => tb.HasComment("Таблица выпускных квалификационных работ (ВКР). Содержит информацию о завершенных работах студентов: название, оценки, файлы работ и презентаций, состав комиссии."));

            entity.HasIndex(e => e.StudentId, "IX_GraduateWorks_StudentId");

            entity.HasIndex(e => e.TeacherId, "IX_GraduateWorks_TeacherId");

            entity.HasIndex(e => e.Year, "IX_GraduateWorks_Year");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasComment("Уникальный идентификатор выпускной квалификационной работы");
            entity.Property(e => e.CommissionMembers).HasComment("Состав комиссии, оценивавшей работу (текстовое описание)");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("Дата и время создания записи о работе")
                .HasColumnType("timestamp with time zone");
            entity.Property(e => e.FilePath).HasComment("Путь к файлу выпускной квалификационной работы (не может быть пустым)");
            entity.Property(e => e.Grade).HasComment("Оценка за работу (от 0 до 100 баллов)");
            entity.Property(e => e.PresentationPath).HasComment("Путь к файлу презентации работы (опционально)");
            entity.Property(e => e.StudentId).HasComment("Идентификатор студента, выполнившего работу (внешний ключ к таблице Students)");
            entity.Property(e => e.TeacherId).HasComment("Идентификатор преподавателя-руководителя работы (внешний ключ к таблице Teachers)");
            entity.Property(e => e.Title)
                .HasComment("Название выпускной квалификационной работы (регистронезависимо)")
                .HasColumnType("citext");
            entity.Property(e => e.UpdatedAt)
                .HasComment("Дата и время последнего обновления записи о работе")
                .HasColumnType("timestamp with time zone");
            entity.Property(e => e.Year).HasComment("Учебный год, в котором была выполнена работа");

            entity.HasOne(d => d.Student).WithMany(p => p.GraduateWorks)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_GraduateWorks_Students");

            entity.HasOne(d => d.Teacher).WithMany(p => p.GraduateWorks)
                .HasForeignKey(d => d.TeacherId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_GraduateWorks_Teachers");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Notifications_pkey");

            entity.ToTable(tb => tb.HasComment("Таблица уведомлений пользователей системы. Содержит информацию о различных типах уведомлений и их статусе прочтения."));

            entity.HasIndex(e => e.CreatedAt, "IX_Notifications_CreatedAt");

            entity.HasIndex(e => e.UserId, "IX_Notifications_UserId");

            entity.HasIndex(e => new { e.UserId, e.IsRead }, "IX_Notifications_UserId_IsRead");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasComment("Уникальный идентификатор уведомления");
            entity.Property(e => e.Content).HasComment("Содержимое уведомления (полный текст)");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("Дата и время создания уведомления")
                .HasColumnType("timestamp with time zone");
            entity.Property(e => e.IsRead).HasComment("Флаг прочтения уведомления (true - прочитано, false - не прочитано)");
            entity.Property(e => e.Title).HasComment("Заголовок уведомления");
            entity.Property(e => e.TypeId).HasComment("Идентификатор типа уведомления (внешний ключ к таблице NotificationTypes)");
            entity.Property(e => e.UserId).HasComment("Идентификатор пользователя-получателя уведомления (внешний ключ к таблице Users)");

            entity.HasOne(d => d.Type).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.TypeId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Notifications_NotificationTypes");

            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Notifications_Users");
        });

        modelBuilder.Entity<NotificationType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("NotificationTypes_pkey");

            entity.ToTable(tb => tb.HasComment("Справочник типов уведомлений системы. Содержит системные и отображаемые названия типов."));

            entity.HasIndex(e => e.CodeName, "NotificationTypes_CodeName_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasComment("Уникальный идентификатор типа уведомления");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("Дата и время создания записи о типе уведомления")
                .HasColumnType("timestamp with time zone");
            entity.Property(e => e.DisplayName)
                .HasMaxLength(100)
                .HasComment("Отображаемое значение типа (для пользовательского интерфейса)");
            entity.Property(e => e.CodeName)
                .HasComment("Системное значение типа (для кода), регистронезависимо")
                .HasColumnType("citext");
            entity.Property(e => e.UpdatedAt)
                .HasComment("Дата и время последнего обновления записи о типе уведомления")
                .HasColumnType("timestamp with time zone");
        });

        modelBuilder.Entity<Position>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Positions_pkey");

            entity.ToTable(tb => tb.HasComment("Справочник должностей преподавателей. Содержит системные и отображаемые названия должностей."));

            entity.HasIndex(e => e.CodeName, "Positions_CodeName_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasComment("Уникальный идентификатор должности");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("Дата и время создания записи о должности")
                .HasColumnType("timestamp with time zone");
            entity.Property(e => e.DisplayName)
                .HasMaxLength(100)
                .HasComment("Отображаемое значение должности (для пользовательского интерфейса)");
            entity.Property(e => e.CodeName)
                .HasComment("Системное значение должности (для кода), регистронезависимо")
                .HasColumnType("citext");
            entity.Property(e => e.UpdatedAt)
                .HasComment("Дата и время последнего обновления записи о должности")
                .HasColumnType("timestamp with time zone");
        });

        modelBuilder.Entity<StudyGroup>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("StudyGroups_pkey");

            entity.ToTable(tb => tb.HasComment("Справочник учебных групп. Содержит номера групп в формате XXXX (факультет, курс, номер)."));

            entity.HasIndex(e => e.CodeName, "StudyGroups_CodeName_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasComment("Уникальный идентификатор группы");
            entity.Property(e => e.CodeName)
                .HasComment("Номер учебной группы (формат: XXXX, например 4411). Значение от 1000 до 9999.");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("Дата и время создания записи о группе")
                .HasColumnType("timestamp with time zone");
            entity.Property(e => e.UpdatedAt)
                .HasComment("Дата и время последнего обновления записи о группе")
                .HasColumnType("timestamp with time zone");
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Students_pkey");

            entity.ToTable(tb => tb.HasComment("Таблица студентов. Содержит дополнительную информацию о студентах: принадлежность к учебной группе."));

            entity.HasIndex(e => e.GroupId, "IX_Students_GroupId");

            entity.HasIndex(e => e.UserId, "Students_UserId_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasComment("Уникальный идентификатор студента");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("Дата и время создания записи о студенте")
                .HasColumnType("timestamp with time zone");
            entity.Property(e => e.GroupId).HasComment("Идентификатор учебной группы студента (внешний ключ к таблице StudyGroups)");
            entity.Property(e => e.UpdatedAt)
                .HasComment("Дата и время последнего обновления записи о студенте")
                .HasColumnType("timestamp with time zone");
            entity.Property(e => e.UserId).HasComment("Идентификатор пользователя-студента (внешний ключ к таблице Users)");

            entity.HasOne(d => d.User).WithOne(p => p.Student)
                .HasForeignKey<Student>(d => d.UserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Students_Users");

            entity.HasOne(d => d.Group).WithMany(p => p.Students)
                .HasForeignKey(d => d.GroupId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Students_StudyGroups");
        });

        modelBuilder.Entity<StudentApplication>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("StudentApplications_pkey");

            entity.ToTable(tb => tb.HasComment("Таблица заявок студентов на темы ВКР. Содержит информацию о заявках: выбранные темы, статусы обработки и временные метки действий преподавателей и заведующих кафедрой."));

            entity.HasIndex(e => e.CreatedAt, "IX_StudentApplications_CreatedAt");

            entity.HasIndex(e => e.StatusId, "IX_StudentApplications_StatusId");

            entity.HasIndex(e => e.StudentId, "IX_StudentApplications_StudentId");

            entity.HasIndex(e => e.TopicId, "IX_StudentApplications_TopicId");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasComment("Уникальный идентификатор заявки");
            entity.Property(e => e.CancelledAt)
                .HasComment("Дата и время отмены заявки студентом")
                .HasColumnType("timestamp with time zone");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("Дата и время создания заявки")
                .HasColumnType("timestamp with time zone");
            entity.Property(e => e.DepartmentHeadApprovedAt)
                .HasComment("Дата и время утверждения заявки заведующим кафедрой")
                .HasColumnType("timestamp with time zone");
            entity.Property(e => e.DepartmentHeadRejectedAt)
                .HasComment("Дата и время отклонения заявки заведующим кафедрой")
                .HasColumnType("timestamp with time zone");
            entity.Property(e => e.DepartmentHeadRejectionReason).HasComment("Причина отклонения заявки заведующим кафедрой");
            entity.Property(e => e.StatusId).HasComment("Идентификатор текущего статуса заявки (внешний ключ к таблице ApplicationStatuses)");
            entity.Property(e => e.StudentId).HasComment("Идентификатор студента, подавшего заявку (внешний ключ к таблице Students)");
            entity.Property(e => e.TeacherApprovedAt)
                .HasComment("Дата и время одобрения заявки преподавателем")
                .HasColumnType("timestamp with time zone");
            entity.Property(e => e.TeacherRejectedAt)
                .HasComment("Дата и время отклонения заявки преподавателем")
                .HasColumnType("timestamp with time zone");
            entity.Property(e => e.TeacherRejectionReason).HasComment("Причина отклонения заявки преподавателем");
            entity.Property(e => e.TopicId).HasComment("Идентификатор темы ВКР, на которую подана заявка (внешний ключ к таблице Topics)");
            entity.Property(e => e.UpdatedAt)
                .HasComment("Дата и время последнего обновления заявки")
                .HasColumnType("timestamp with time zone");

            entity.HasOne(d => d.Status).WithMany(p => p.StudentApplications)
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_StudentApplications_ApplicationStatuses");

            entity.HasOne(d => d.Student).WithMany(p => p.StudentApplications)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_StudentApplications_Students");

            entity.HasOne(d => d.Topic).WithMany(p => p.StudentApplications)
                .HasForeignKey(d => d.TopicId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_StudentApplications_Topics");
        });

        modelBuilder.Entity<Teacher>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Teachers_pkey");

            entity.ToTable(tb => tb.HasComment("Таблица преподавателей. Содержит дополнительную информацию о преподавателях: академические данные и лимит студентов."));

            entity.HasIndex(e => e.AcademicDegreeId, "IX_Teachers_AcademicDegreeId");

            entity.HasIndex(e => e.AcademicTitleId, "IX_Teachers_AcademicTitleId");

            entity.HasIndex(e => e.PositionId, "IX_Teachers_PositionId");

            entity.HasIndex(e => e.UserId, "Teachers_UserId_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasComment("Уникальный идентификатор преподавателя");
            entity.Property(e => e.AcademicDegreeId).HasComment("Идентификатор ученой степени преподавателя (внешний ключ к таблице AcademicDegrees)");
            entity.Property(e => e.AcademicTitleId).HasComment("Идентификатор ученого звания преподавателя (внешний ключ к таблице AcademicTitles)");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("Дата и время создания записи о преподавателе")
                .HasColumnType("timestamp with time zone");
            entity.Property(e => e.MaxStudentsLimit).HasComment("Максимальное количество студентов, которых может взять преподаватель для руководства ВКР");
            entity.Property(e => e.PositionId).HasComment("Идентификатор должности преподавателя (внешний ключ к таблице Positions)");
            entity.Property(e => e.UpdatedAt)
                .HasComment("Дата и время последнего обновления записи о преподавателе")
                .HasColumnType("timestamp with time zone");
            entity.Property(e => e.UserId).HasComment("Идентификатор пользователя-преподавателя (внешний ключ к таблице Users)");

            entity.HasOne(d => d.AcademicDegree).WithMany(p => p.Teachers)
                .HasForeignKey(d => d.AcademicDegreeId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Teachers_AcademicDegrees");

            entity.HasOne(d => d.AcademicTitle).WithMany(p => p.Teachers)
                .HasForeignKey(d => d.AcademicTitleId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Teachers_AcademicTitles");

            entity.HasOne(d => d.Position).WithMany(p => p.Teachers)
                .HasForeignKey(d => d.PositionId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Teachers_Positions");

            entity.HasOne(d => d.User).WithOne(p => p.Teacher)
                .HasForeignKey<Teacher>(d => d.UserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Teachers_Users");
        });

        modelBuilder.Entity<Topic>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Topics_pkey");

            entity.ToTable(tb => tb.HasComment("Таблица тем выпускных квалификационных работ (ВКР). Содержит темы, предложенные как научными руководителями, так и студентами."));

            entity.HasIndex(e => e.StatusId, "IX_Topics_StatusId");

            entity.HasIndex(e => e.CreatorTypeId, "IX_Topics_CreatorTypeId");

            entity.HasIndex(e => e.CreatedBy, "IX_Topics_CreatedBy");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasComment("Уникальный идентификатор темы ВКР");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("Дата и время создания записи о теме")
                .HasColumnType("timestamp with time zone");
            entity.Property(e => e.Description).HasComment("Подробное описание темы ВКР, требования и особенности");
            entity.Property(e => e.StatusId).HasComment("Идентификатор статуса темы (внешний ключ к таблице TopicStatuses)");
            entity.Property(e => e.CreatorTypeId).HasComment("Тип пользователя, создавшего тему (внешний ключ к таблице TopicCreatorTypes)");
            entity.Property(e => e.CreatedBy).HasComment("Пользователь, создавший тему (внешний ключ к таблице Users)");
            entity.Property(e => e.Title)
                .HasComment("Название темы выпускной квалификационной работы")
                .HasColumnType("citext");
            entity.Property(e => e.UpdatedAt)
                .HasComment("Дата и время последнего обновления записи о теме")
                .HasColumnType("timestamp with time zone");

            entity.HasOne(d => d.Status).WithMany(p => p.Topics)
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Topics_TopicStatuses");

            entity.HasOne(d => d.CreatorType).WithMany(p => p.Topics)
                .HasForeignKey(d => d.CreatorTypeId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Topics_TopicCreatorTypes");

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.CreatedTopics)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Topics_Users");
        });

        modelBuilder.Entity<TopicCreatorType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("TopicCreatorTypes_pkey");

            entity.ToTable(tb => tb.HasComment("Справочник типов пользователей, создающих темы ВКР. Определяет, кем была предложена тема: научным руководителем или студентом."));

            entity.HasIndex(e => e.CodeName, "TopicCreatorTypes_CodeName_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasComment("Уникальный идентификатор типа создателя темы");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("Дата и время создания записи")
                .HasColumnType("timestamp with time zone");
            entity.Property(e => e.DisplayName)
                .HasMaxLength(100)
                .HasComment("Отображаемое значение типа (для пользовательского интерфейса)");
            entity.Property(e => e.CodeName)
                .HasComment("Системное значение типа (для кода), регистронезависимо")
                .HasColumnType("citext");
            entity.Property(e => e.UpdatedAt)
                .HasComment("Дата и время последнего обновления записи")
                .HasColumnType("timestamp with time zone");
        });

        modelBuilder.Entity<TopicStatus>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("TopicStatuses_pkey");

            entity.ToTable(tb => tb.HasComment("Справочник статусов тем ВКР. Содержит системные и отображаемые названия статусов."));

            entity.HasIndex(e => e.CodeName, "TopicStatuses_CodeName_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasComment("Уникальный идентификатор статуса темы");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("Дата и время создания записи о статусе")
                .HasColumnType("timestamp with time zone");
            entity.Property(e => e.DisplayName)
                .HasMaxLength(100)
                .HasComment("Отображаемое значение статуса (для пользовательского интерфейса)");
            entity.Property(e => e.CodeName)
                .HasComment("Системное значение статуса (для кода), регистронезависимо")
                .HasColumnType("citext");
            entity.Property(e => e.UpdatedAt)
                .HasComment("Дата и время последнего обновления записи о статусе")
                .HasColumnType("timestamp with time zone");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Users_pkey");

            entity.ToTable(tb => tb.HasComment("Таблица пользователей системы. Содержит основную информацию о пользователях: учетные данные, персональные данные, роль и принадлежность к кафедре."));

            entity.HasIndex(e => e.DepartmentId, "IX_Users_DepartmentId");

            entity.HasIndex(e => e.IsActive, "IX_Users_IsActive");

            entity.HasIndex(e => e.RoleId, "IX_Users_RoleId");

            entity.HasIndex(e => e.Email, "Users_Email_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasComment("Уникальный идентификатор пользователя");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("Дата и время создания записи о пользователе")
                .HasColumnType("timestamp with time zone");
            entity.Property(e => e.DepartmentId).HasComment("Идентификатор кафедры пользователя (внешний ключ к таблице Departments)");
            entity.Property(e => e.Email)
                .HasComment("Email пользователя (уникальный, регистронезависимый)")
                .HasColumnType("citext");
            entity.Property(e => e.FirstName)
                .HasMaxLength(100)
                .HasComment("Имя пользователя");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasComment("Флаг активности пользователя (true - активен, false - деактивирован)");
            entity.Property(e => e.LastName)
                .HasMaxLength(100)
                .HasComment("Фамилия пользователя");
            entity.Property(e => e.MiddleName)
                .HasMaxLength(100)
                .HasComment("Отчество пользователя");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasComment("Хеш пароля пользователя");
            entity.Property(e => e.RoleId).HasComment("Идентификатор роли пользователя (внешний ключ к таблице UserRoles)");
            entity.Property(e => e.UpdatedAt)
                .HasComment("Дата и время последнего обновления записи о пользователе")
                .HasColumnType("timestamp with time zone");

            entity.HasOne(d => d.Department).WithMany(p => p.Users)
                .HasForeignKey(d => d.DepartmentId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Users_Departments");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Users_UserRoles");
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("UserRoles_pkey");

            entity.ToTable(tb => tb.HasComment("Справочник ролей пользователей системы. Содержит системные и отображаемые названия ролей."));

            entity.HasIndex(e => e.CodeName, "UserRoles_CodeName_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasComment("Уникальный идентификатор роли пользователя");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("Дата и время создания записи о роли")
                .HasColumnType("timestamp with time zone");
            entity.Property(e => e.DisplayName)
                .HasMaxLength(100)
                .HasComment("Отображаемое значение роли (для пользовательского интерфейса)");
            entity.Property(e => e.CodeName)
                .HasComment("Системное значение роли (для кода), регистронезависимо")
                .HasColumnType("citext");
            entity.Property(e => e.UpdatedAt)
                .HasComment("Дата и время последнего обновления записи о роли")
                .HasColumnType("timestamp with time zone");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
