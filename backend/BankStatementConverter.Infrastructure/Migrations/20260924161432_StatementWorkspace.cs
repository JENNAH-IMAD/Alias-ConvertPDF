using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankStatementConverter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class StatementWorkspace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BankStatementProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BankId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Code = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DetectionText = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    TransactionPattern = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    DateFormat = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    DecimalSeparator = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ThousandsSeparator = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    OcrRequired = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankStatementProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BankStatementProfiles_Banks_BankId",
                        column: x => x.BankId,
                        principalTable: "Banks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BankStatements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BankAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    FileHash = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    PageCount = table.Column<int>(type: "integer", nullable: false),
                    PdfType = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    RawText = table.Column<string>(type: "text", nullable: false),
                    Currency = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    PeriodStart = table.Column<DateOnly>(type: "date", nullable: true),
                    PeriodEnd = table.Column<DateOnly>(type: "date", nullable: true),
                    OpeningBalance = table.Column<decimal>(type: "numeric(20,4)", precision: 20, scale: 4, nullable: true),
                    ClosingBalance = table.Column<decimal>(type: "numeric(20,4)", precision: 20, scale: 4, nullable: true),
                    ProfileId = table.Column<Guid>(type: "uuid", nullable: true),
                    ValidatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ValidatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ArchivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ProcessingStartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankStatements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BankStatements_BankAccounts_BankAccountId",
                        column: x => x.BankAccountId,
                        principalTable: "BankAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExportTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Code = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Type = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Encoding = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Delimiter = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    DateFormat = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    DecimalSeparator = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Decimals = table.Column<int>(type: "integer", nullable: false),
                    IncludeHeader = table.Column<bool>(type: "boolean", nullable: false),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExportTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BankTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BankStatementId = table.Column<Guid>(type: "uuid", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    TransactionDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ValueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Reference = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Debit = table.Column<decimal>(type: "numeric(20,4)", precision: 20, scale: 4, nullable: true),
                    Credit = table.Column<decimal>(type: "numeric(20,4)", precision: 20, scale: 4, nullable: true),
                    Balance = table.Column<decimal>(type: "numeric(20,4)", precision: 20, scale: 4, nullable: true),
                    Currency = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    RawText = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ConfidenceScore = table.Column<decimal>(type: "numeric(20,4)", precision: 20, scale: 4, nullable: true),
                    IsValidated = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BankTransactions_BankStatements_BankStatementId",
                        column: x => x.BankStatementId,
                        principalTable: "BankStatements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProcessingHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BankStatementId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessingHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcessingHistories_BankStatements_BankStatementId",
                        column: x => x.BankStatementId,
                        principalTable: "BankStatements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExportTemplateFields",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExportTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceField = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    OutputField = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    Required = table.Column<bool>(type: "boolean", nullable: false),
                    DefaultValue = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExportTemplateFields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExportTemplateFields_ExportTemplates_ExportTemplateId",
                        column: x => x.ExportTemplateId,
                        principalTable: "ExportTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StatementExports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BankStatementId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExportTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    FileHash = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    TemplateSnapshot = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatementExports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StatementExports_BankStatements_BankStatementId",
                        column: x => x.BankStatementId,
                        principalTable: "BankStatements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StatementExports_ExportTemplates_ExportTemplateId",
                        column: x => x.ExportTemplateId,
                        principalTable: "ExportTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BankStatementProfiles_BankId",
                table: "BankStatementProfiles",
                column: "BankId");

            migrationBuilder.CreateIndex(
                name: "IX_BankStatementProfiles_Code_Version",
                table: "BankStatementProfiles",
                columns: new[] { "Code", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BankStatements_BankAccountId_FileHash",
                table: "BankStatements",
                columns: new[] { "BankAccountId", "FileHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BankStatements_CreatedBy_Status_CreatedAt",
                table: "BankStatements",
                columns: new[] { "CreatedBy", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BankStatements_PeriodStart_PeriodEnd",
                table: "BankStatements",
                columns: new[] { "PeriodStart", "PeriodEnd" });

            migrationBuilder.CreateIndex(
                name: "IX_BankTransactions_BankStatementId",
                table: "BankTransactions",
                column: "BankStatementId");

            migrationBuilder.CreateIndex(
                name: "IX_ExportTemplateFields_ExportTemplateId",
                table: "ExportTemplateFields",
                column: "ExportTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_ExportTemplates_Code",
                table: "ExportTemplates",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProcessingHistories_BankStatementId",
                table: "ProcessingHistories",
                column: "BankStatementId");

            migrationBuilder.CreateIndex(
                name: "IX_StatementExports_BankStatementId",
                table: "StatementExports",
                column: "BankStatementId");

            migrationBuilder.CreateIndex(
                name: "IX_StatementExports_ExportTemplateId",
                table: "StatementExports",
                column: "ExportTemplateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BankStatementProfiles");

            migrationBuilder.DropTable(
                name: "BankTransactions");

            migrationBuilder.DropTable(
                name: "ExportTemplateFields");

            migrationBuilder.DropTable(
                name: "ProcessingHistories");

            migrationBuilder.DropTable(
                name: "StatementExports");

            migrationBuilder.DropTable(
                name: "BankStatements");

            migrationBuilder.DropTable(
                name: "ExportTemplates");
        }
    }
}
