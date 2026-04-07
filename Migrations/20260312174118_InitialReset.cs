using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KodvianSuperMarket.Migrations
{
    /// <inheritdoc />
    public partial class InitialReset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContainerTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DepositAmount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContainerTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DNI = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Address = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    PhoneBackup = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    BirthDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsFixedCustomer = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    AllowsCredit = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreditLimit = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    IsAnonymous = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImportJobs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ImportType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Upsert = table.Column<bool>(type: "boolean", nullable: false),
                    Mode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TotalRows = table.Column<int>(type: "integer", nullable: false),
                    ValidRows = table.Column<int>(type: "integer", nullable: false),
                    InvalidRows = table.Column<int>(type: "integer", nullable: false),
                    CreatedCount = table.Column<int>(type: "integer", nullable: false),
                    UpdatedCount = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KanbanBoards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KanbanBoards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentProviderEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Provider = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    EventId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    EventType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ExternalReference = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    PayloadJson = table.Column<string>(type: "text", nullable: false, defaultValue: "{}"),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentProviderEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PriceLists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceLists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Promotions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PromotionType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    NxM_BuyQuantity = table.Column<int>(type: "integer", nullable: true),
                    NxM_FreeQuantity = table.Column<int>(type: "integer", nullable: true),
                    PercentDiscount = table.Column<decimal>(type: "numeric", nullable: true),
                    PackPrice = table.Column<decimal>(type: "numeric", nullable: true),
                    MinPurchaseAmount = table.Column<decimal>(type: "numeric", nullable: true),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Promotions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Settings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Suppliers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CUIT = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Address = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suppliers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsTrainingTenant = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerMonthlyStatements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    InitialBalance = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    Purchases = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    Payments = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    LateFees = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    FinalBalance = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    LateFeeAccrued = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    PaidAmount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    RemainingBalance = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsPaid = table.Column<bool>(type: "boolean", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LateFeeAppliedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LateFeeAppliedAmount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerMonthlyStatements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerMonthlyStatements_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ImportJobErrors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ImportJobId = table.Column<int>(type: "integer", nullable: false),
                    RowNumber = table.Column<int>(type: "integer", nullable: false),
                    Field = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportJobErrors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportJobErrors_ImportJobs_ImportJobId",
                        column: x => x.ImportJobId,
                        principalTable: "ImportJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KanbanTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    KanbanBoardId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Shift = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    IsRequiredForShiftClose = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KanbanTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KanbanTemplates_KanbanBoards_KanbanBoardId",
                        column: x => x.KanbanBoardId,
                        principalTable: "KanbanBoards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Barcode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    QuickCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    SaleType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsCigarette = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    AllowsManualPrice = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    TracksExpiry = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    StockControl = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    MinStock = table.Column<decimal>(type: "numeric(12,3)", nullable: false),
                    ReorderQtySuggestion = table.Column<decimal>(type: "numeric(12,3)", nullable: false),
                    PreferredSupplierId = table.Column<int>(type: "integer", nullable: true),
                    PurchaseUnit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsReplenishable = table.Column<bool>(type: "boolean", nullable: false),
                    ContainerTypeId = table.Column<int>(type: "integer", nullable: true),
                    ContainerDepositOverride = table.Column<decimal>(type: "numeric(12,2)", nullable: true),
                    CatalogStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    UnitName = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    DefaultPrice = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    DefaultPricePerKg = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    LastCost = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    LastCostUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Products_ContainerTypes_ContainerTypeId",
                        column: x => x.ContainerTypeId,
                        principalTable: "ContainerTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Products_Suppliers_PreferredSupplierId",
                        column: x => x.PreferredSupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Stores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Address = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SettingsJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Stores_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenantBrandingSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    LogoUrl = table.Column<string>(type: "character varying(800)", maxLength: 800, nullable: true),
                    PrimaryColor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SecondaryColor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TicketHeaderText = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    TicketFooterText = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ReturnPolicyText = table.Column<string>(type: "character varying(800)", maxLength: 800, nullable: false),
                    SupportPhone = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    SupportEmail = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantBrandingSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantBrandingSettings_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrainingChecklists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: true),
                    Role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingChecklists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingChecklists_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TenantId = table.Column<int>(type: "integer", nullable: true),
                    PasswordHash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PinHash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Operator"),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Usuarios_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CustomerAccountMovements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    MovementType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ReferenceType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ReferenceId = table.Column<int>(type: "integer", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AllocatedStatementId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerAccountMovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerAccountMovements_CustomerMonthlyStatements_Allocate~",
                        column: x => x.AllocatedStatementId,
                        principalTable: "CustomerMonthlyStatements",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CustomerAccountMovements_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecurrenceRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    KanbanTemplateId = table.Column<int>(type: "integer", nullable: false),
                    Frequency = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DayOfWeek = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecurrenceRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecurrenceRules_KanbanTemplates_KanbanTemplateId",
                        column: x => x.KanbanTemplateId,
                        principalTable: "KanbanTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TemplateChecklistItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    KanbanTemplateId = table.Column<int>(type: "integer", nullable: false),
                    Text = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateChecklistItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemplateChecklistItems_KanbanTemplates_KanbanTemplateId",
                        column: x => x.KanbanTemplateId,
                        principalTable: "KanbanTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductPrices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    PriceListId = table.Column<int>(type: "integer", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    PricePerKg = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductPrices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductPrices_PriceLists_PriceListId",
                        column: x => x.PriceListId,
                        principalTable: "PriceLists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductPrices_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PromotionProducts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PromotionId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromotionProducts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PromotionProducts_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PromotionProducts_Promotions_PromotionId",
                        column: x => x.PromotionId,
                        principalTable: "Promotions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OnboardingSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: true),
                    StoreId = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CurrentStepKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedByUsuarioId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnboardingSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OnboardingSessions_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_OnboardingSessions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ProductStocks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    StoreId = table.Column<int>(type: "integer", nullable: true),
                    Bucket = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(12,3)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductStocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductStocks_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductStocks_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StockCountSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StoreId = table.Column<int>(type: "integer", nullable: true),
                    SessionType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ExplicitConfirmation = table.Column<bool>(type: "boolean", nullable: false),
                    WarningMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CommittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockCountSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockCountSessions_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TrainingChecklistItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TrainingChecklistId = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingChecklistItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingChecklistItems_TrainingChecklists_TrainingChecklist~",
                        column: x => x.TrainingChecklistId,
                        principalTable: "TrainingChecklists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AuditEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    StoreId = table.Column<int>(type: "integer", nullable: true),
                    EventType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DeviceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AdditionalData = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Success = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditEvents_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AuditEvents_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Devices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    StoreId = table.Column<int>(type: "integer", nullable: true),
                    TokenHash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    DeviceName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DeviceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsRevoked = table.Column<bool>(type: "boolean", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ParentCashRegisterDeviceId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Devices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Devices_Devices_ParentCashRegisterDeviceId",
                        column: x => x.ParentCashRegisterDeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Devices_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Devices_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeExtras",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    CreatedById = table.Column<int>(type: "integer", nullable: false),
                    ExtraDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    Hours = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsApproved = table.Column<bool>(type: "boolean", nullable: false),
                    ApprovedById = table.Column<int>(type: "integer", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeExtras", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeExtras_Usuarios_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_EmployeeExtras_Usuarios_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeExtras_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OperatorSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    SessionTokenHash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsRevoked = table.Column<bool>(type: "boolean", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokeReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperatorSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OperatorSessions_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PayrollReceipts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    FileName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FileContent = table.Column<byte[]>(type: "bytea", nullable: false),
                    ContentType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollReceipts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollReceipts_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseSuggestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: true),
                    StoreId = table.Column<int>(type: "integer", nullable: true),
                    GeneratedByUsuarioId = table.Column<int>(type: "integer", nullable: true),
                    DaysWindow = table.Column<int>(type: "integer", nullable: false),
                    TargetCoverageDays = table.Column<decimal>(type: "numeric(12,3)", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseSuggestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseSuggestions_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PurchaseSuggestions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PurchaseSuggestions_Usuarios_GeneratedByUsuarioId",
                        column: x => x.GeneratedByUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "StoreUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StoreId = table.Column<int>(type: "integer", nullable: false),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    Role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoreUsers_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StoreUsers_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrainingChecklistRuns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: true),
                    TrainingChecklistId = table.Column<int>(type: "integer", nullable: false),
                    Role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    StartedByUsuarioId = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingChecklistRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingChecklistRuns_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TrainingChecklistRuns_TrainingChecklists_TrainingChecklistId",
                        column: x => x.TrainingChecklistId,
                        principalTable: "TrainingChecklists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TrainingChecklistRuns_Usuarios_StartedByUsuarioId",
                        column: x => x.StartedByUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CustomerStatementAllocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StatementId = table.Column<int>(type: "integer", nullable: false),
                    MovementId = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    AllocatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerStatementAllocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerStatementAllocations_CustomerAccountMovements_Movem~",
                        column: x => x.MovementId,
                        principalTable: "CustomerAccountMovements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerStatementAllocations_CustomerMonthlyStatements_Stat~",
                        column: x => x.StatementId,
                        principalTable: "CustomerMonthlyStatements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OnboardingStepStates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OnboardingSessionId = table.Column<int>(type: "integer", nullable: false),
                    StepKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DataJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnboardingStepStates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OnboardingStepStates_OnboardingSessions_OnboardingSessionId",
                        column: x => x.OnboardingSessionId,
                        principalTable: "OnboardingSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockCountLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StockCountSessionId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: true),
                    Barcode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    QuickCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ProductName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    RowNumber = table.Column<int>(type: "integer", nullable: false),
                    CurrentVendibleQty = table.Column<decimal>(type: "numeric(12,3)", nullable: false),
                    CurrentReclamoQty = table.Column<decimal>(type: "numeric(12,3)", nullable: false),
                    CurrentMermaQty = table.Column<decimal>(type: "numeric(12,3)", nullable: false),
                    TargetVendibleQty = table.Column<decimal>(type: "numeric(12,3)", nullable: false),
                    TargetReclamoQty = table.Column<decimal>(type: "numeric(12,3)", nullable: false),
                    TargetMermaQty = table.Column<decimal>(type: "numeric(12,3)", nullable: false),
                    DeltaVendibleQty = table.Column<decimal>(type: "numeric(12,3)", nullable: false),
                    DeltaReclamoQty = table.Column<decimal>(type: "numeric(12,3)", nullable: false),
                    DeltaMermaQty = table.Column<decimal>(type: "numeric(12,3)", nullable: false),
                    Error = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockCountLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockCountLines_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_StockCountLines_StockCountSessions_StockCountSessionId",
                        column: x => x.StockCountSessionId,
                        principalTable: "StockCountSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Purchases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SupplierId = table.Column<int>(type: "integer", nullable: true),
                    DocType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DocNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PurchaseDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedById = table.Column<int>(type: "integer", nullable: false),
                    DeviceId = table.Column<int>(type: "integer", nullable: false),
                    StoreId = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    Tax = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    Total = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    CancelReason = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConfirmedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Purchases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Purchases_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Purchases_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Purchases_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Purchases_Usuarios_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Carts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DeviceId = table.Column<int>(type: "integer", nullable: false),
                    StoreId = table.Column<int>(type: "integer", nullable: true),
                    OperatorSessionId = table.Column<int>(type: "integer", nullable: true),
                    TargetCashRegisterDeviceId = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SentToCashierAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConvertedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Carts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Carts_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Carts_Devices_TargetCashRegisterDeviceId",
                        column: x => x.TargetCashRegisterDeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Carts_OperatorSessions_OperatorSessionId",
                        column: x => x.OperatorSessionId,
                        principalTable: "OperatorSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Carts_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CashSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DeviceId = table.Column<int>(type: "integer", nullable: false),
                    StoreId = table.Column<int>(type: "integer", nullable: true),
                    OperatorSessionId = table.Column<int>(type: "integer", nullable: true),
                    Shift = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    OpeningCash = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalCash = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalCard = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalTransfer = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalCredit = table.Column<decimal>(type: "numeric", nullable: false),
                    DeclaredCash = table.Column<decimal>(type: "numeric", nullable: false),
                    DeclaredCard = table.Column<decimal>(type: "numeric", nullable: false),
                    DeclaredTransfer = table.Column<decimal>(type: "numeric", nullable: false),
                    DeclaredCredit = table.Column<decimal>(type: "numeric", nullable: false),
                    OpenedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CloseNotes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CashSessions_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CashSessions_OperatorSessions_OperatorSessionId",
                        column: x => x.OperatorSessionId,
                        principalTable: "OperatorSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CashSessions_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ContainerMovements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    ContainerTypeId = table.Column<int>(type: "integer", nullable: false),
                    Direction = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Qty = table.Column<decimal>(type: "numeric(12,3)", nullable: false),
                    RefType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    RefId = table.Column<int>(type: "integer", nullable: true),
                    CreatedByOperatorSessionId = table.Column<int>(type: "integer", nullable: true),
                    CreatedByUsuarioId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContainerMovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContainerMovements_ContainerTypes_ContainerTypeId",
                        column: x => x.ContainerTypeId,
                        principalTable: "ContainerTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContainerMovements_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContainerMovements_OperatorSessions_CreatedByOperatorSessio~",
                        column: x => x.CreatedByOperatorSessionId,
                        principalTable: "OperatorSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ContainerMovements_Usuarios_CreatedByUsuarioId",
                        column: x => x.CreatedByUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ExternalExchanges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SupplierId = table.Column<int>(type: "integer", nullable: false),
                    ExchangeDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeviceId = table.Column<int>(type: "integer", nullable: false),
                    CreatedByOperatorSessionId = table.Column<int>(type: "integer", nullable: false),
                    CreatedByUsuarioId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalExchanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExternalExchanges_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExternalExchanges_OperatorSessions_CreatedByOperatorSession~",
                        column: x => x.CreatedByOperatorSessionId,
                        principalTable: "OperatorSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExternalExchanges_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExternalExchanges_Usuarios_CreatedByUsuarioId",
                        column: x => x.CreatedByUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SupplierReturns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SupplierId = table.Column<int>(type: "integer", nullable: false),
                    ReturnDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeviceId = table.Column<int>(type: "integer", nullable: false),
                    CreatedByOperatorSessionId = table.Column<int>(type: "integer", nullable: false),
                    CreatedByUsuarioId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierReturns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierReturns_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SupplierReturns_OperatorSessions_CreatedByOperatorSessionId",
                        column: x => x.CreatedByOperatorSessionId,
                        principalTable: "OperatorSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SupplierReturns_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierReturns_Usuarios_CreatedByUsuarioId",
                        column: x => x.CreatedByUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrainingChecklistRunItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TrainingChecklistRunId = table.Column<int>(type: "integer", nullable: false),
                    TrainingChecklistItemId = table.Column<int>(type: "integer", nullable: false),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingChecklistRunItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingChecklistRunItems_TrainingChecklistItems_TrainingCh~",
                        column: x => x.TrainingChecklistItemId,
                        principalTable: "TrainingChecklistItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TrainingChecklistRunItems_TrainingChecklistRuns_TrainingChe~",
                        column: x => x.TrainingChecklistRunId,
                        principalTable: "TrainingChecklistRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PurchaseId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DamagedForClaimQty = table.Column<decimal>(type: "numeric", nullable: false),
                    DiscardQty = table.Column<decimal>(type: "numeric", nullable: false),
                    UpdateSalePrice = table.Column<bool>(type: "boolean", nullable: false),
                    NewSalePrice = table.Column<decimal>(type: "numeric(12,2)", nullable: true),
                    NewPricePerKg = table.Column<decimal>(type: "numeric(12,2)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseItems_Purchases_PurchaseId",
                        column: x => x.PurchaseId,
                        principalTable: "Purchases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseSuggestionLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PurchaseSuggestionId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    SuggestedSupplierId = table.Column<int>(type: "integer", nullable: true),
                    CurrentStock = table.Column<decimal>(type: "numeric(12,3)", nullable: false),
                    MinStock = table.Column<decimal>(type: "numeric(12,3)", nullable: false),
                    AvgDailySales = table.Column<decimal>(type: "numeric(12,3)", nullable: false),
                    TargetCoverageStock = table.Column<decimal>(type: "numeric(12,3)", nullable: false),
                    SuggestedQty = table.Column<decimal>(type: "numeric(12,3)", nullable: false),
                    AcceptedQty = table.Column<decimal>(type: "numeric(12,3)", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedPurchaseId = table.Column<int>(type: "integer", nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseSuggestionLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseSuggestionLines_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseSuggestionLines_PurchaseSuggestions_PurchaseSuggest~",
                        column: x => x.PurchaseSuggestionId,
                        principalTable: "PurchaseSuggestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PurchaseSuggestionLines_Purchases_CreatedPurchaseId",
                        column: x => x.CreatedPurchaseId,
                        principalTable: "Purchases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PurchaseSuggestionLines_Suppliers_SuggestedSupplierId",
                        column: x => x.SuggestedSupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SupplierClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StoreId = table.Column<int>(type: "integer", nullable: true),
                    SupplierId = table.Column<int>(type: "integer", nullable: true),
                    PurchaseId = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    HasReceipt = table.Column<bool>(type: "boolean", nullable: false),
                    ReceiptType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ReceiptNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PickedUpAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreditedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierClaims_Purchases_PurchaseId",
                        column: x => x.PurchaseId,
                        principalTable: "Purchases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SupplierClaims_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SupplierClaims_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CartItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CartId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: true),
                    ProductCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ProductName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    Unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Discount = table.Column<decimal>(type: "numeric", nullable: false),
                    ContainerReturnedNowQty = table.Column<decimal>(type: "numeric(12,3)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CartItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CartItems_Carts_CartId",
                        column: x => x.CartId,
                        principalTable: "Carts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CartItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CashSessionMoneyMovements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CashSessionId = table.Column<int>(type: "integer", nullable: false),
                    StoreId = table.Column<int>(type: "integer", nullable: true),
                    Method = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SignedAmount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Reason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RefType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    RefId = table.Column<int>(type: "integer", nullable: true),
                    CreatedByOperatorSessionId = table.Column<int>(type: "integer", nullable: true),
                    CreatedByUsuarioId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashSessionMoneyMovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CashSessionMoneyMovements_CashSessions_CashSessionId",
                        column: x => x.CashSessionId,
                        principalTable: "CashSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CashSessionMoneyMovements_OperatorSessions_CreatedByOperato~",
                        column: x => x.CreatedByOperatorSessionId,
                        principalTable: "OperatorSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CashSessionMoneyMovements_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CashSessionMoneyMovements_Usuarios_CreatedByUsuarioId",
                        column: x => x.CreatedByUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CigaretteCounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CashSessionId = table.Column<int>(type: "integer", nullable: false),
                    CountDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AdjustmentsApplied = table.Column<bool>(type: "boolean", nullable: false),
                    AdjustmentsAppliedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CigaretteCounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CigaretteCounts_CashSessions_CashSessionId",
                        column: x => x.CashSessionId,
                        principalTable: "CashSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KanbanTasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    KanbanBoardId = table.Column<int>(type: "integer", nullable: false),
                    CashSessionId = table.Column<int>(type: "integer", nullable: false),
                    StoreId = table.Column<int>(type: "integer", nullable: true),
                    KanbanTemplateId = table.Column<int>(type: "integer", nullable: true),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsRequiredForShiftClose = table.Column<bool>(type: "boolean", nullable: false),
                    AssignedToUsuarioId = table.Column<int>(type: "integer", nullable: true),
                    UpdatedByUsuarioId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KanbanTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KanbanTasks_CashSessions_CashSessionId",
                        column: x => x.CashSessionId,
                        principalTable: "CashSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KanbanTasks_KanbanBoards_KanbanBoardId",
                        column: x => x.KanbanBoardId,
                        principalTable: "KanbanBoards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KanbanTasks_KanbanTemplates_KanbanTemplateId",
                        column: x => x.KanbanTemplateId,
                        principalTable: "KanbanTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_KanbanTasks_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_KanbanTasks_Usuarios_AssignedToUsuarioId",
                        column: x => x.AssignedToUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_KanbanTasks_Usuarios_UpdatedByUsuarioId",
                        column: x => x.UpdatedByUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Sales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CartId = table.Column<int>(type: "integer", nullable: false),
                    DeviceId = table.Column<int>(type: "integer", nullable: false),
                    StoreId = table.Column<int>(type: "integer", nullable: true),
                    OperatorSessionId = table.Column<int>(type: "integer", nullable: true),
                    CashSessionId = table.Column<int>(type: "integer", nullable: true),
                    CustomerId = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric", nullable: false),
                    Discount = table.Column<decimal>(type: "numeric", nullable: false),
                    PromoDiscount = table.Column<decimal>(type: "numeric", nullable: false),
                    ManualDiscount = table.Column<decimal>(type: "numeric", nullable: false),
                    CigaretteSurcharge = table.Column<decimal>(type: "numeric", nullable: false),
                    Tax = table.Column<decimal>(type: "numeric", nullable: false),
                    Total = table.Column<decimal>(type: "numeric", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ExternalTicketId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedOffline = table.Column<bool>(type: "boolean", nullable: false),
                    OfflineSource = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sales_Carts_CartId",
                        column: x => x.CartId,
                        principalTable: "Carts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Sales_CashSessions_CashSessionId",
                        column: x => x.CashSessionId,
                        principalTable: "CashSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Sales_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Sales_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Sales_OperatorSessions_OperatorSessionId",
                        column: x => x.OperatorSessionId,
                        principalTable: "OperatorSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Sales_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TimePunches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    CashSessionId = table.Column<int>(type: "integer", nullable: true),
                    DeviceId = table.Column<int>(type: "integer", nullable: false),
                    OperatorSessionId = table.Column<int>(type: "integer", nullable: true),
                    PunchType = table.Column<string>(type: "text", nullable: false),
                    IsOpen = table.Column<bool>(type: "boolean", nullable: false),
                    PunchTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsAdjusted = table.Column<bool>(type: "boolean", nullable: false),
                    AdjustedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AdjustedById = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimePunches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TimePunches_CashSessions_CashSessionId",
                        column: x => x.CashSessionId,
                        principalTable: "CashSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TimePunches_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TimePunches_OperatorSessions_OperatorSessionId",
                        column: x => x.OperatorSessionId,
                        principalTable: "OperatorSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TimePunches_Usuarios_AdjustedById",
                        column: x => x.AdjustedById,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TimePunches_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExternalExchangeLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ExternalExchangeId = table.Column<int>(type: "integer", nullable: false),
                    Direction = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    Qty = table.Column<decimal>(type: "numeric(12,3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalExchangeLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExternalExchangeLines_ExternalExchanges_ExternalExchangeId",
                        column: x => x.ExternalExchangeId,
                        principalTable: "ExternalExchanges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExternalExchangeLines_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupplierReturnLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SupplierReturnId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    Qty = table.Column<decimal>(type: "numeric(12,3)", nullable: false),
                    UnitCostSnapshot = table.Column<decimal>(type: "numeric(12,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierReturnLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierReturnLines_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierReturnLines_SupplierReturns_SupplierReturnId",
                        column: x => x.SupplierReturnId,
                        principalTable: "SupplierReturns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SupplierClaimItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SupplierClaimId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(12,3)", nullable: false),
                    UnitCostSnapshot = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierClaimItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierClaimItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierClaimItems_SupplierClaims_SupplierClaimId",
                        column: x => x.SupplierClaimId,
                        principalTable: "SupplierClaims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SupplierCredits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SupplierId = table.Column<int>(type: "integer", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    RemainingAmount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    SupplierClaimId = table.Column<int>(type: "integer", nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierCredits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierCredits_SupplierClaims_SupplierClaimId",
                        column: x => x.SupplierClaimId,
                        principalTable: "SupplierClaims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SupplierCredits_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CigaretteCountLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CigaretteCountId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    SystemQtyAtCount = table.Column<decimal>(type: "numeric(12,3)", nullable: false),
                    CountedQty = table.Column<decimal>(type: "numeric(12,3)", nullable: false),
                    AdjustmentApplied = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CigaretteCountLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CigaretteCountLines_CigaretteCounts_CigaretteCountId",
                        column: x => x.CigaretteCountId,
                        principalTable: "CigaretteCounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CigaretteCountLines_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GeneratedTasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CashSessionId = table.Column<int>(type: "integer", nullable: false),
                    KanbanTemplateId = table.Column<int>(type: "integer", nullable: false),
                    GenerationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Shift = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    KanbanTaskId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeneratedTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GeneratedTasks_CashSessions_CashSessionId",
                        column: x => x.CashSessionId,
                        principalTable: "CashSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GeneratedTasks_KanbanTasks_KanbanTaskId",
                        column: x => x.KanbanTaskId,
                        principalTable: "KanbanTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GeneratedTasks_KanbanTemplates_KanbanTemplateId",
                        column: x => x.KanbanTemplateId,
                        principalTable: "KanbanTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KanbanChecklistItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    KanbanTaskId = table.Column<int>(type: "integer", nullable: false),
                    Text = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    IsDone = table.Column<bool>(type: "boolean", nullable: false),
                    DoneAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DoneByUsuarioId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KanbanChecklistItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KanbanChecklistItems_KanbanTasks_KanbanTaskId",
                        column: x => x.KanbanTaskId,
                        principalTable: "KanbanTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KanbanChecklistItems_Usuarios_DoneByUsuarioId",
                        column: x => x.DoneByUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "KanbanComments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    KanbanTaskId = table.Column<int>(type: "integer", nullable: false),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    Text = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KanbanComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KanbanComments_KanbanTasks_KanbanTaskId",
                        column: x => x.KanbanTaskId,
                        principalTable: "KanbanTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KanbanComments_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SaleItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SaleId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: true),
                    ProductCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ProductName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    Unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Discount = table.Column<decimal>(type: "numeric", nullable: false),
                    PromoDiscount = table.Column<decimal>(type: "numeric", nullable: false),
                    ManualDiscount = table.Column<decimal>(type: "numeric", nullable: false),
                    CigaretteSurcharge = table.Column<decimal>(type: "numeric", nullable: false),
                    HasManualPrice = table.Column<bool>(type: "boolean", nullable: false),
                    PromotionId = table.Column<int>(type: "integer", nullable: true),
                    PromotionType = table.Column<string>(type: "text", nullable: true),
                    ContainerTypeId = table.Column<int>(type: "integer", nullable: true),
                    ContainerOwedQty = table.Column<decimal>(type: "numeric(12,3)", nullable: false),
                    ContainerDepositAmountSnapshot = table.Column<decimal>(type: "numeric(12,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaleItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SaleItems_ContainerTypes_ContainerTypeId",
                        column: x => x.ContainerTypeId,
                        principalTable: "ContainerTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SaleItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SaleItems_Sales_SaleId",
                        column: x => x.SaleId,
                        principalTable: "Sales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SalePayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SaleId = table.Column<int>(type: "integer", nullable: false),
                    PaymentMethod = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    Reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Provider = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    ExternalReference = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    ConfirmedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConfirmNotes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalePayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalePayments_Sales_SaleId",
                        column: x => x.SaleId,
                        principalTable: "Sales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SaleReturns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OriginalSaleId = table.Column<int>(type: "integer", nullable: false),
                    CashSessionId = table.Column<int>(type: "integer", nullable: false),
                    StoreId = table.Column<int>(type: "integer", nullable: true),
                    RefundPreference = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RefundTotal = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    ReturnedSubtotal = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    ReturnedCigaretteSurchargeShare = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    CustomerId = table.Column<int>(type: "integer", nullable: true),
                    CustomerAlias = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedByOperatorSessionId = table.Column<int>(type: "integer", nullable: false),
                    CreatedByUsuarioId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaleReturns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SaleReturns_CashSessions_CashSessionId",
                        column: x => x.CashSessionId,
                        principalTable: "CashSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SaleReturns_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SaleReturns_OperatorSessions_CreatedByOperatorSessionId",
                        column: x => x.CreatedByOperatorSessionId,
                        principalTable: "OperatorSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SaleReturns_Sales_OriginalSaleId",
                        column: x => x.OriginalSaleId,
                        principalTable: "Sales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SaleReturns_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SaleReturns_Usuarios_CreatedByUsuarioId",
                        column: x => x.CreatedByUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockMovements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    StoreId = table.Column<int>(type: "integer", nullable: true),
                    Bucket = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DeltaQty = table.Column<decimal>(type: "numeric(12,3)", nullable: false),
                    MovementType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    PurchaseId = table.Column<int>(type: "integer", nullable: true),
                    SaleId = table.Column<int>(type: "integer", nullable: true),
                    SupplierClaimId = table.Column<int>(type: "integer", nullable: true),
                    OperatorSessionId = table.Column<int>(type: "integer", nullable: true),
                    DeviceId = table.Column<int>(type: "integer", nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockMovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockMovements_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockMovements_Purchases_PurchaseId",
                        column: x => x.PurchaseId,
                        principalTable: "Purchases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_StockMovements_Sales_SaleId",
                        column: x => x.SaleId,
                        principalTable: "Sales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_StockMovements_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TimePunchAdjustments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TimePunchId = table.Column<int>(type: "integer", nullable: false),
                    AdjustedById = table.Column<int>(type: "integer", nullable: false),
                    OriginalPunchTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NewPunchTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimePunchAdjustments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TimePunchAdjustments_TimePunches_TimePunchId",
                        column: x => x.TimePunchId,
                        principalTable: "TimePunches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TimePunchAdjustments_Usuarios_AdjustedById",
                        column: x => x.AdjustedById,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupplierCreditApplications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PurchaseId = table.Column<int>(type: "integer", nullable: false),
                    SupplierCreditId = table.Column<int>(type: "integer", nullable: false),
                    AppliedAmount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierCreditApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierCreditApplications_Purchases_PurchaseId",
                        column: x => x.PurchaseId,
                        principalTable: "Purchases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SupplierCreditApplications_SupplierCredits_SupplierCreditId",
                        column: x => x.SupplierCreditId,
                        principalTable: "SupplierCredits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SaleReturnLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SaleReturnId = table.Column<int>(type: "integer", nullable: false),
                    OriginalSaleItemId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: true),
                    QtyReturned = table.Column<decimal>(type: "numeric(12,3)", nullable: false),
                    Condition = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    LineRefundAmount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    IsCigarette = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaleReturnLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SaleReturnLines_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SaleReturnLines_SaleItems_OriginalSaleItemId",
                        column: x => x.OriginalSaleItemId,
                        principalTable: "SaleItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SaleReturnLines_SaleReturns_SaleReturnId",
                        column: x => x.SaleReturnId,
                        principalTable: "SaleReturns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_CreatedAt",
                table: "AuditEvents",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_CreatedAt_EventType",
                table: "AuditEvents",
                columns: new[] { "CreatedAt", "EventType" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_EventType",
                table: "AuditEvents",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_StoreId",
                table: "AuditEvents",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_Success",
                table: "AuditEvents",
                column: "Success");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_UsuarioId",
                table: "AuditEvents",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_CartId",
                table: "CartItems",
                column: "CartId");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_ProductCode",
                table: "CartItems",
                column: "ProductCode");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_ProductId",
                table: "CartItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Carts_CreatedAt",
                table: "Carts",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Carts_DeviceId",
                table: "Carts",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_Carts_DeviceId_Status",
                table: "Carts",
                columns: new[] { "DeviceId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Carts_OperatorSessionId",
                table: "Carts",
                column: "OperatorSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Carts_Status",
                table: "Carts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Carts_StoreId",
                table: "Carts",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_Carts_TargetCashRegisterDeviceId",
                table: "Carts",
                column: "TargetCashRegisterDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_CashSessionMoneyMovements_CashSessionId",
                table: "CashSessionMoneyMovements",
                column: "CashSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_CashSessionMoneyMovements_CreatedAt",
                table: "CashSessionMoneyMovements",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CashSessionMoneyMovements_CreatedByOperatorSessionId",
                table: "CashSessionMoneyMovements",
                column: "CreatedByOperatorSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_CashSessionMoneyMovements_CreatedByUsuarioId",
                table: "CashSessionMoneyMovements",
                column: "CreatedByUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_CashSessionMoneyMovements_Method",
                table: "CashSessionMoneyMovements",
                column: "Method");

            migrationBuilder.CreateIndex(
                name: "IX_CashSessionMoneyMovements_StoreId",
                table: "CashSessionMoneyMovements",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_CashSessionMoneyMovements_Type",
                table: "CashSessionMoneyMovements",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_CashSessions_DeviceId",
                table: "CashSessions",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_CashSessions_DeviceId_Status",
                table: "CashSessions",
                columns: new[] { "DeviceId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CashSessions_OpenedAt",
                table: "CashSessions",
                column: "OpenedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CashSessions_OperatorSessionId",
                table: "CashSessions",
                column: "OperatorSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_CashSessions_Status",
                table: "CashSessions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CashSessions_StoreId",
                table: "CashSessions",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_CigaretteCountLines_CigaretteCountId",
                table: "CigaretteCountLines",
                column: "CigaretteCountId");

            migrationBuilder.CreateIndex(
                name: "IX_CigaretteCountLines_ProductId",
                table: "CigaretteCountLines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_CigaretteCounts_CashSessionId",
                table: "CigaretteCounts",
                column: "CashSessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CigaretteCounts_CountDate",
                table: "CigaretteCounts",
                column: "CountDate");

            migrationBuilder.CreateIndex(
                name: "IX_ContainerMovements_ContainerTypeId",
                table: "ContainerMovements",
                column: "ContainerTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ContainerMovements_CreatedAt",
                table: "ContainerMovements",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ContainerMovements_CreatedByOperatorSessionId",
                table: "ContainerMovements",
                column: "CreatedByOperatorSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ContainerMovements_CreatedByUsuarioId",
                table: "ContainerMovements",
                column: "CreatedByUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_ContainerMovements_CustomerId",
                table: "ContainerMovements",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_ContainerMovements_Direction",
                table: "ContainerMovements",
                column: "Direction");

            migrationBuilder.CreateIndex(
                name: "IX_ContainerTypes_IsActive",
                table: "ContainerTypes",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ContainerTypes_Name",
                table: "ContainerTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAccountMovements_AllocatedStatementId",
                table: "CustomerAccountMovements",
                column: "AllocatedStatementId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAccountMovements_CreatedAt",
                table: "CustomerAccountMovements",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAccountMovements_CustomerId",
                table: "CustomerAccountMovements",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAccountMovements_CustomerId_MovementType",
                table: "CustomerAccountMovements",
                columns: new[] { "CustomerId", "MovementType" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAccountMovements_MovementType",
                table: "CustomerAccountMovements",
                column: "MovementType");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerMonthlyStatements_CustomerId_Year_Month",
                table: "CustomerMonthlyStatements",
                columns: new[] { "CustomerId", "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerMonthlyStatements_DueDate",
                table: "CustomerMonthlyStatements",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerMonthlyStatements_IsPaid",
                table: "CustomerMonthlyStatements",
                column: "IsPaid");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_CreatedAt",
                table: "Customers",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_DNI",
                table: "Customers",
                column: "DNI");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_IsActive",
                table: "Customers",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_IsAnonymous",
                table: "Customers",
                column: "IsAnonymous");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerStatementAllocations_MovementId",
                table: "CustomerStatementAllocations",
                column: "MovementId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerStatementAllocations_StatementId",
                table: "CustomerStatementAllocations",
                column: "StatementId");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_CreatedAt",
                table: "Devices",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_ParentCashRegisterDeviceId",
                table: "Devices",
                column: "ParentCashRegisterDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_StoreId",
                table: "Devices",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_TokenHash",
                table: "Devices",
                column: "TokenHash");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_UsuarioId",
                table: "Devices",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_UsuarioId_IsRevoked",
                table: "Devices",
                columns: new[] { "UsuarioId", "IsRevoked" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeExtras_ApprovedById",
                table: "EmployeeExtras",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeExtras_CreatedById",
                table: "EmployeeExtras",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeExtras_ExtraDate",
                table: "EmployeeExtras",
                column: "ExtraDate");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeExtras_UsuarioId",
                table: "EmployeeExtras",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeExtras_UsuarioId_Year_Month",
                table: "EmployeeExtras",
                columns: new[] { "UsuarioId", "Year", "Month" });

            migrationBuilder.CreateIndex(
                name: "IX_ExternalExchangeLines_Direction",
                table: "ExternalExchangeLines",
                column: "Direction");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalExchangeLines_ExternalExchangeId",
                table: "ExternalExchangeLines",
                column: "ExternalExchangeId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalExchangeLines_ProductId",
                table: "ExternalExchangeLines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalExchanges_CreatedAt",
                table: "ExternalExchanges",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalExchanges_CreatedByOperatorSessionId",
                table: "ExternalExchanges",
                column: "CreatedByOperatorSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalExchanges_CreatedByUsuarioId",
                table: "ExternalExchanges",
                column: "CreatedByUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalExchanges_DeviceId",
                table: "ExternalExchanges",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalExchanges_ExchangeDate",
                table: "ExternalExchanges",
                column: "ExchangeDate");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalExchanges_SupplierId",
                table: "ExternalExchanges",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_GeneratedTasks_CashSessionId_GenerationDate_Shift_KanbanTem~",
                table: "GeneratedTasks",
                columns: new[] { "CashSessionId", "GenerationDate", "Shift", "KanbanTemplateId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GeneratedTasks_KanbanTaskId",
                table: "GeneratedTasks",
                column: "KanbanTaskId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GeneratedTasks_KanbanTemplateId",
                table: "GeneratedTasks",
                column: "KanbanTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportJobErrors_ImportJobId",
                table: "ImportJobErrors",
                column: "ImportJobId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportJobErrors_RowNumber",
                table: "ImportJobErrors",
                column: "RowNumber");

            migrationBuilder.CreateIndex(
                name: "IX_ImportJobs_CreatedAt",
                table: "ImportJobs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ImportJobs_ImportType",
                table: "ImportJobs",
                column: "ImportType");

            migrationBuilder.CreateIndex(
                name: "IX_ImportJobs_Mode",
                table: "ImportJobs",
                column: "Mode");

            migrationBuilder.CreateIndex(
                name: "IX_ImportJobs_Status",
                table: "ImportJobs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_KanbanBoards_IsActive",
                table: "KanbanBoards",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_KanbanBoards_Name",
                table: "KanbanBoards",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KanbanChecklistItems_DoneByUsuarioId",
                table: "KanbanChecklistItems",
                column: "DoneByUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_KanbanChecklistItems_KanbanTaskId",
                table: "KanbanChecklistItems",
                column: "KanbanTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_KanbanComments_CreatedAt",
                table: "KanbanComments",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_KanbanComments_KanbanTaskId",
                table: "KanbanComments",
                column: "KanbanTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_KanbanComments_UsuarioId",
                table: "KanbanComments",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_KanbanTasks_AssignedToUsuarioId",
                table: "KanbanTasks",
                column: "AssignedToUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_KanbanTasks_CashSessionId",
                table: "KanbanTasks",
                column: "CashSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_KanbanTasks_CashSessionId_IsRequiredForShiftClose_Status",
                table: "KanbanTasks",
                columns: new[] { "CashSessionId", "IsRequiredForShiftClose", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_KanbanTasks_KanbanBoardId",
                table: "KanbanTasks",
                column: "KanbanBoardId");

            migrationBuilder.CreateIndex(
                name: "IX_KanbanTasks_KanbanTemplateId",
                table: "KanbanTasks",
                column: "KanbanTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_KanbanTasks_Status",
                table: "KanbanTasks",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_KanbanTasks_StoreId",
                table: "KanbanTasks",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_KanbanTasks_UpdatedByUsuarioId",
                table: "KanbanTasks",
                column: "UpdatedByUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_KanbanTemplates_IsActive",
                table: "KanbanTemplates",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_KanbanTemplates_KanbanBoardId",
                table: "KanbanTemplates",
                column: "KanbanBoardId");

            migrationBuilder.CreateIndex(
                name: "IX_KanbanTemplates_Shift",
                table: "KanbanTemplates",
                column: "Shift");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingSessions_CreatedAt",
                table: "OnboardingSessions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingSessions_CreatedByUsuarioId",
                table: "OnboardingSessions",
                column: "CreatedByUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingSessions_Status",
                table: "OnboardingSessions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingSessions_StoreId",
                table: "OnboardingSessions",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingSessions_TenantId",
                table: "OnboardingSessions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingStepStates_OnboardingSessionId",
                table: "OnboardingStepStates",
                column: "OnboardingSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingStepStates_OnboardingSessionId_StepKey",
                table: "OnboardingStepStates",
                columns: new[] { "OnboardingSessionId", "StepKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OperatorSessions_CreatedAt",
                table: "OperatorSessions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_OperatorSessions_ExpiresAt",
                table: "OperatorSessions",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_OperatorSessions_SessionTokenHash",
                table: "OperatorSessions",
                column: "SessionTokenHash");

            migrationBuilder.CreateIndex(
                name: "IX_OperatorSessions_UsuarioId",
                table: "OperatorSessions",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_OperatorSessions_UsuarioId_IsRevoked",
                table: "OperatorSessions",
                columns: new[] { "UsuarioId", "IsRevoked" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentProviderEvents_ExternalReference",
                table: "PaymentProviderEvents",
                column: "ExternalReference");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentProviderEvents_Provider_EventId",
                table: "PaymentProviderEvents",
                columns: new[] { "Provider", "EventId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentProviderEvents_ReceivedAt",
                table: "PaymentProviderEvents",
                column: "ReceivedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollReceipts_CreatedAt",
                table: "PayrollReceipts",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollReceipts_UsuarioId_Year_Month",
                table: "PayrollReceipts",
                columns: new[] { "UsuarioId", "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PriceLists_IsDefault",
                table: "PriceLists",
                column: "IsDefault");

            migrationBuilder.CreateIndex(
                name: "IX_PriceLists_Name",
                table: "PriceLists",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductPrices_PriceListId",
                table: "ProductPrices",
                column: "PriceListId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductPrices_ProductId_PriceListId",
                table: "ProductPrices",
                columns: new[] { "ProductId", "PriceListId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_Barcode",
                table: "Products",
                column: "Barcode");

            migrationBuilder.CreateIndex(
                name: "IX_Products_CatalogStatus",
                table: "Products",
                column: "CatalogStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Products_ContainerTypeId",
                table: "Products",
                column: "ContainerTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_CreatedAt",
                table: "Products",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Products_PreferredSupplierId",
                table: "Products",
                column: "PreferredSupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_QuickCode",
                table: "Products",
                column: "QuickCode");

            migrationBuilder.CreateIndex(
                name: "IX_ProductStocks_ProductId_Bucket",
                table: "ProductStocks",
                columns: new[] { "ProductId", "Bucket" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductStocks_StoreId",
                table: "ProductStocks",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductStocks_UpdatedAt",
                table: "ProductStocks",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionProducts_ProductId",
                table: "PromotionProducts",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionProducts_PromotionId_ProductId",
                table: "PromotionProducts",
                columns: new[] { "PromotionId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Promotions_EndDate",
                table: "Promotions",
                column: "EndDate");

            migrationBuilder.CreateIndex(
                name: "IX_Promotions_IsActive",
                table: "Promotions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Promotions_StartDate",
                table: "Promotions",
                column: "StartDate");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseItems_ProductId",
                table: "PurchaseItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseItems_PurchaseId",
                table: "PurchaseItems",
                column: "PurchaseId");

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_CreatedAt",
                table: "Purchases",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_CreatedById",
                table: "Purchases",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_DeviceId",
                table: "Purchases",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_PurchaseDate",
                table: "Purchases",
                column: "PurchaseDate");

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_Status",
                table: "Purchases",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_StoreId",
                table: "Purchases",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_SupplierId",
                table: "Purchases",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseSuggestionLines_CreatedPurchaseId",
                table: "PurchaseSuggestionLines",
                column: "CreatedPurchaseId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseSuggestionLines_ProductId_Status",
                table: "PurchaseSuggestionLines",
                columns: new[] { "ProductId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseSuggestionLines_PurchaseSuggestionId",
                table: "PurchaseSuggestionLines",
                column: "PurchaseSuggestionId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseSuggestionLines_SuggestedSupplierId",
                table: "PurchaseSuggestionLines",
                column: "SuggestedSupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseSuggestions_GeneratedByUsuarioId",
                table: "PurchaseSuggestions",
                column: "GeneratedByUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseSuggestions_Status",
                table: "PurchaseSuggestions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseSuggestions_StoreId",
                table: "PurchaseSuggestions",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseSuggestions_TenantId_StoreId_GeneratedAt",
                table: "PurchaseSuggestions",
                columns: new[] { "TenantId", "StoreId", "GeneratedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RecurrenceRules_Frequency",
                table: "RecurrenceRules",
                column: "Frequency");

            migrationBuilder.CreateIndex(
                name: "IX_RecurrenceRules_KanbanTemplateId",
                table: "RecurrenceRules",
                column: "KanbanTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleItems_ContainerTypeId",
                table: "SaleItems",
                column: "ContainerTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleItems_ProductCode",
                table: "SaleItems",
                column: "ProductCode");

            migrationBuilder.CreateIndex(
                name: "IX_SaleItems_ProductId",
                table: "SaleItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleItems_SaleId",
                table: "SaleItems",
                column: "SaleId");

            migrationBuilder.CreateIndex(
                name: "IX_SalePayments_ExternalReference",
                table: "SalePayments",
                column: "ExternalReference");

            migrationBuilder.CreateIndex(
                name: "IX_SalePayments_PaymentMethod",
                table: "SalePayments",
                column: "PaymentMethod");

            migrationBuilder.CreateIndex(
                name: "IX_SalePayments_PaymentMethod_Status",
                table: "SalePayments",
                columns: new[] { "PaymentMethod", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SalePayments_SaleId",
                table: "SalePayments",
                column: "SaleId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleReturnLines_OriginalSaleItemId",
                table: "SaleReturnLines",
                column: "OriginalSaleItemId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleReturnLines_ProductId",
                table: "SaleReturnLines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleReturnLines_SaleReturnId",
                table: "SaleReturnLines",
                column: "SaleReturnId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleReturns_CashSessionId",
                table: "SaleReturns",
                column: "CashSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleReturns_CreatedAt",
                table: "SaleReturns",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SaleReturns_CreatedByOperatorSessionId",
                table: "SaleReturns",
                column: "CreatedByOperatorSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleReturns_CreatedByUsuarioId",
                table: "SaleReturns",
                column: "CreatedByUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleReturns_CustomerId",
                table: "SaleReturns",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleReturns_OriginalSaleId",
                table: "SaleReturns",
                column: "OriginalSaleId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleReturns_StoreId",
                table: "SaleReturns",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_CartId",
                table: "Sales",
                column: "CartId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sales_CashSessionId",
                table: "Sales",
                column: "CashSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_CreatedAt",
                table: "Sales",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_CustomerId",
                table: "Sales",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_DeviceId",
                table: "Sales",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_DeviceId_Status",
                table: "Sales",
                columns: new[] { "DeviceId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Sales_ExternalTicketId",
                table: "Sales",
                column: "ExternalTicketId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sales_InvoiceNumber",
                table: "Sales",
                column: "InvoiceNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_OperatorSessionId",
                table: "Sales",
                column: "OperatorSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_Status",
                table: "Sales",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_StoreId",
                table: "Sales",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_Settings_Key",
                table: "Settings",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockCountLines_Barcode",
                table: "StockCountLines",
                column: "Barcode");

            migrationBuilder.CreateIndex(
                name: "IX_StockCountLines_ProductId",
                table: "StockCountLines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_StockCountLines_QuickCode",
                table: "StockCountLines",
                column: "QuickCode");

            migrationBuilder.CreateIndex(
                name: "IX_StockCountLines_StockCountSessionId",
                table: "StockCountLines",
                column: "StockCountSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_StockCountSessions_CreatedAt",
                table: "StockCountSessions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_StockCountSessions_SessionType",
                table: "StockCountSessions",
                column: "SessionType");

            migrationBuilder.CreateIndex(
                name: "IX_StockCountSessions_Status",
                table: "StockCountSessions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_StockCountSessions_StoreId",
                table: "StockCountSessions",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_CreatedAt",
                table: "StockMovements",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_MovementType",
                table: "StockMovements",
                column: "MovementType");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_ProductId",
                table: "StockMovements",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_PurchaseId",
                table: "StockMovements",
                column: "PurchaseId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_SaleId",
                table: "StockMovements",
                column: "SaleId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_StoreId",
                table: "StockMovements",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_Stores_TenantId_Code",
                table: "Stores",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Stores_TenantId_Name",
                table: "Stores",
                columns: new[] { "TenantId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_StoreUsers_StoreId_UsuarioId",
                table: "StoreUsers",
                columns: new[] { "StoreId", "UsuarioId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoreUsers_UsuarioId",
                table: "StoreUsers",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierClaimItems_ProductId",
                table: "SupplierClaimItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierClaimItems_SupplierClaimId",
                table: "SupplierClaimItems",
                column: "SupplierClaimId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierClaims_CreatedAt",
                table: "SupplierClaims",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierClaims_PurchaseId",
                table: "SupplierClaims",
                column: "PurchaseId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierClaims_Status",
                table: "SupplierClaims",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierClaims_StoreId",
                table: "SupplierClaims",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierClaims_SupplierId",
                table: "SupplierClaims",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierCreditApplications_PurchaseId",
                table: "SupplierCreditApplications",
                column: "PurchaseId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierCreditApplications_SupplierCreditId",
                table: "SupplierCreditApplications",
                column: "SupplierCreditId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierCredits_CreatedAt",
                table: "SupplierCredits",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierCredits_SupplierClaimId",
                table: "SupplierCredits",
                column: "SupplierClaimId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierCredits_SupplierId",
                table: "SupplierCredits",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierReturnLines_ProductId",
                table: "SupplierReturnLines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierReturnLines_SupplierReturnId",
                table: "SupplierReturnLines",
                column: "SupplierReturnId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierReturns_CreatedAt",
                table: "SupplierReturns",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierReturns_CreatedByOperatorSessionId",
                table: "SupplierReturns",
                column: "CreatedByOperatorSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierReturns_CreatedByUsuarioId",
                table: "SupplierReturns",
                column: "CreatedByUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierReturns_DeviceId",
                table: "SupplierReturns",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierReturns_ReturnDate",
                table: "SupplierReturns",
                column: "ReturnDate");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierReturns_SupplierId",
                table: "SupplierReturns",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_CUIT",
                table: "Suppliers",
                column: "CUIT");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_IsActive",
                table: "Suppliers",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateChecklistItems_KanbanTemplateId",
                table: "TemplateChecklistItems",
                column: "KanbanTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantBrandingSettings_TenantId",
                table: "TenantBrandingSettings",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Code",
                table: "Tenants",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Name",
                table: "Tenants",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_TimePunchAdjustments_AdjustedById",
                table: "TimePunchAdjustments",
                column: "AdjustedById");

            migrationBuilder.CreateIndex(
                name: "IX_TimePunchAdjustments_CreatedAt",
                table: "TimePunchAdjustments",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TimePunchAdjustments_TimePunchId",
                table: "TimePunchAdjustments",
                column: "TimePunchId");

            migrationBuilder.CreateIndex(
                name: "IX_TimePunches_AdjustedById",
                table: "TimePunches",
                column: "AdjustedById");

            migrationBuilder.CreateIndex(
                name: "IX_TimePunches_CashSessionId",
                table: "TimePunches",
                column: "CashSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_TimePunches_DeviceId",
                table: "TimePunches",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_TimePunches_OperatorSessionId",
                table: "TimePunches",
                column: "OperatorSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_TimePunches_PunchTime",
                table: "TimePunches",
                column: "PunchTime");

            migrationBuilder.CreateIndex(
                name: "IX_TimePunches_UsuarioId",
                table: "TimePunches",
                column: "UsuarioId",
                unique: true,
                filter: "\"IsOpen\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_TimePunches_UsuarioId_PunchType",
                table: "TimePunches",
                columns: new[] { "UsuarioId", "PunchType" });

            migrationBuilder.CreateIndex(
                name: "IX_TrainingChecklistItems_TrainingChecklistId_SortOrder",
                table: "TrainingChecklistItems",
                columns: new[] { "TrainingChecklistId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_TrainingChecklistRunItems_TrainingChecklistItemId",
                table: "TrainingChecklistRunItems",
                column: "TrainingChecklistItemId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingChecklistRunItems_TrainingChecklistRunId_TrainingCh~",
                table: "TrainingChecklistRunItems",
                columns: new[] { "TrainingChecklistRunId", "TrainingChecklistItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainingChecklistRuns_StartedByUsuarioId",
                table: "TrainingChecklistRuns",
                column: "StartedByUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingChecklistRuns_Status",
                table: "TrainingChecklistRuns",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingChecklistRuns_TenantId_Role_StartedAt",
                table: "TrainingChecklistRuns",
                columns: new[] { "TenantId", "Role", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TrainingChecklistRuns_TrainingChecklistId",
                table: "TrainingChecklistRuns",
                column: "TrainingChecklistId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingChecklists_CreatedAt",
                table: "TrainingChecklists",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingChecklists_TenantId_Role_IsActive",
                table: "TrainingChecklists",
                columns: new[] { "TenantId", "Role", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_TenantId",
                table: "Usuarios",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Username",
                table: "Usuarios",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditEvents");

            migrationBuilder.DropTable(
                name: "CartItems");

            migrationBuilder.DropTable(
                name: "CashSessionMoneyMovements");

            migrationBuilder.DropTable(
                name: "CigaretteCountLines");

            migrationBuilder.DropTable(
                name: "ContainerMovements");

            migrationBuilder.DropTable(
                name: "CustomerStatementAllocations");

            migrationBuilder.DropTable(
                name: "EmployeeExtras");

            migrationBuilder.DropTable(
                name: "ExternalExchangeLines");

            migrationBuilder.DropTable(
                name: "GeneratedTasks");

            migrationBuilder.DropTable(
                name: "ImportJobErrors");

            migrationBuilder.DropTable(
                name: "KanbanChecklistItems");

            migrationBuilder.DropTable(
                name: "KanbanComments");

            migrationBuilder.DropTable(
                name: "OnboardingStepStates");

            migrationBuilder.DropTable(
                name: "PaymentProviderEvents");

            migrationBuilder.DropTable(
                name: "PayrollReceipts");

            migrationBuilder.DropTable(
                name: "ProductPrices");

            migrationBuilder.DropTable(
                name: "ProductStocks");

            migrationBuilder.DropTable(
                name: "PromotionProducts");

            migrationBuilder.DropTable(
                name: "PurchaseItems");

            migrationBuilder.DropTable(
                name: "PurchaseSuggestionLines");

            migrationBuilder.DropTable(
                name: "RecurrenceRules");

            migrationBuilder.DropTable(
                name: "SalePayments");

            migrationBuilder.DropTable(
                name: "SaleReturnLines");

            migrationBuilder.DropTable(
                name: "Settings");

            migrationBuilder.DropTable(
                name: "StockCountLines");

            migrationBuilder.DropTable(
                name: "StockMovements");

            migrationBuilder.DropTable(
                name: "StoreUsers");

            migrationBuilder.DropTable(
                name: "SupplierClaimItems");

            migrationBuilder.DropTable(
                name: "SupplierCreditApplications");

            migrationBuilder.DropTable(
                name: "SupplierReturnLines");

            migrationBuilder.DropTable(
                name: "TemplateChecklistItems");

            migrationBuilder.DropTable(
                name: "TenantBrandingSettings");

            migrationBuilder.DropTable(
                name: "TimePunchAdjustments");

            migrationBuilder.DropTable(
                name: "TrainingChecklistRunItems");

            migrationBuilder.DropTable(
                name: "CigaretteCounts");

            migrationBuilder.DropTable(
                name: "CustomerAccountMovements");

            migrationBuilder.DropTable(
                name: "ExternalExchanges");

            migrationBuilder.DropTable(
                name: "ImportJobs");

            migrationBuilder.DropTable(
                name: "KanbanTasks");

            migrationBuilder.DropTable(
                name: "OnboardingSessions");

            migrationBuilder.DropTable(
                name: "PriceLists");

            migrationBuilder.DropTable(
                name: "Promotions");

            migrationBuilder.DropTable(
                name: "PurchaseSuggestions");

            migrationBuilder.DropTable(
                name: "SaleItems");

            migrationBuilder.DropTable(
                name: "SaleReturns");

            migrationBuilder.DropTable(
                name: "StockCountSessions");

            migrationBuilder.DropTable(
                name: "SupplierCredits");

            migrationBuilder.DropTable(
                name: "SupplierReturns");

            migrationBuilder.DropTable(
                name: "TimePunches");

            migrationBuilder.DropTable(
                name: "TrainingChecklistItems");

            migrationBuilder.DropTable(
                name: "TrainingChecklistRuns");

            migrationBuilder.DropTable(
                name: "CustomerMonthlyStatements");

            migrationBuilder.DropTable(
                name: "KanbanTemplates");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "Sales");

            migrationBuilder.DropTable(
                name: "SupplierClaims");

            migrationBuilder.DropTable(
                name: "TrainingChecklists");

            migrationBuilder.DropTable(
                name: "KanbanBoards");

            migrationBuilder.DropTable(
                name: "ContainerTypes");

            migrationBuilder.DropTable(
                name: "Carts");

            migrationBuilder.DropTable(
                name: "CashSessions");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropTable(
                name: "Purchases");

            migrationBuilder.DropTable(
                name: "OperatorSessions");

            migrationBuilder.DropTable(
                name: "Devices");

            migrationBuilder.DropTable(
                name: "Suppliers");

            migrationBuilder.DropTable(
                name: "Stores");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropTable(
                name: "Tenants");
        }
    }
}
