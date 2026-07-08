using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Heimatplatz.Api.Core.Data.Migrations.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FederalProvinces",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FederalProvinces", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ForeclosureAuctions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AuctionDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    ObjectDescription = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PostalCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    RegistrationNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CadastralMunicipality = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PlotNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SheetNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TotalArea = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    BuildingArea = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    GardenArea = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    PlotArea = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    YearBuilt = table.Column<int>(type: "integer", nullable: true),
                    NumberOfRooms = table.Column<int>(type: "integer", nullable: true),
                    ZoningDesignation = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BuildingCondition = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EstimatedValue = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    MinimumBid = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    ViewingDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    BiddingDeadline = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    OwnershipShare = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CaseNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Court = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EdictUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    FloorPlanUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SitePlanUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    LongAppraisalUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ShortAppraisalUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ImageUrls = table.Column<string>(type: "text", nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    State = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    PublicationDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FirstSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastScrapedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RemovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ForeclosureAuctions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LegalSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SettingType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ResponsiblePartyJson = table.Column<string>(type: "TEXT", nullable: true),
                    SectionsJson = table.Column<string>(type: "TEXT", nullable: true),
                    Version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EffectiveDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegalSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ListingAnalyses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ImageUrls = table.Column<string>(type: "text", nullable: false),
                    VideoUrls = table.Column<string>(type: "text", nullable: false),
                    DictatedText = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    UserNotes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    ResultJson = table.Column<string>(type: "TEXT", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListingAnalyses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SellerSources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SellerSources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Vorname = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Nachname = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    SellerType = table.Column<int>(type: "integer", nullable: true),
                    CompanyName = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Districts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FederalProvinceId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Districts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Districts_FederalProvinces_FederalProvinceId",
                        column: x => x.FederalProvinceId,
                        principalTable: "FederalProvinces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ForeclosureAuctionChanges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ForeclosureAuctionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChangeType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ChangedFields = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    OldContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    NewContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ForeclosureAuctionChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ForeclosureAuctionChanges_ForeclosureAuctions_ForeclosureAu~",
                        column: x => x.ForeclosureAuctionId,
                        principalTable: "ForeclosureAuctions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NotificationPreferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    FilterMode = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    SelectedLocationsJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false, defaultValue: "[]"),
                    IsHausSelected = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    IsGrundstueckSelected = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    IsZwangsversteigerungSelected = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    IsPrivateSelected = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    IsBrokerSelected = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationPreferences_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PushSubscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceToken = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Platform = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SubscribedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PushSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PushSubscriptions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsRevoked = table.Column<bool>(type: "boolean", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReplacedByTokenId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserFilterPreferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SelectedOrtesJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false, defaultValue: "[]"),
                    SelectedAgeFilter = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    IsHausSelected = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    IsGrundstueckSelected = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    IsZwangsversteigerungSelected = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    IsPrivateSelected = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    IsBrokerSelected = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    ExcludedSellerSourceIdsJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false, defaultValue: "[]"),
                    SelectedSort = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserFilterPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserFilterPreferences_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleType = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Municipalities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PostalCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    DistrictId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Municipalities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Municipalities_Districts_DistrictId",
                        column: x => x.DistrictId,
                        principalTable: "Districts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Properties",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    MunicipalityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    LivingAreaSquareMeters = table.Column<int>(type: "integer", nullable: true),
                    PlotAreaSquareMeters = table.Column<int>(type: "integer", nullable: true),
                    Rooms = table.Column<int>(type: "integer", nullable: true),
                    YearBuilt = table.Column<int>(type: "integer", nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    SellerType = table.Column<int>(type: "integer", nullable: false),
                    SellerName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Features = table.Column<string>(type: "text", nullable: false),
                    ImageUrls = table.Column<string>(type: "text", nullable: false),
                    TypeSpecificData = table.Column<string>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    InquiryType = table.Column<int>(type: "integer", nullable: false),
                    SourceName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SourceId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SourceUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    SourceLastUpdated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Properties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Properties_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Properties_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BlockedProperties",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlockedProperties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BlockedProperties_Properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "Properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BlockedProperties_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Favorites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Favorites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Favorites_Properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "Properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Favorites_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PropertyContactInfos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Source = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    Phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    OriginalListingUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    SourceName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SourceId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropertyContactInfos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PropertyContactInfos_Properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "Properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BlockedProperties_CreatedAt",
                table: "BlockedProperties",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_BlockedProperties_PropertyId",
                table: "BlockedProperties",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_BlockedProperties_UserId",
                table: "BlockedProperties",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_BlockedProperties_UserId_PropertyId",
                table: "BlockedProperties",
                columns: new[] { "UserId", "PropertyId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Districts_FederalProvinceId",
                table: "Districts",
                column: "FederalProvinceId");

            migrationBuilder.CreateIndex(
                name: "IX_Districts_Key",
                table: "Districts",
                column: "Key");

            migrationBuilder.CreateIndex(
                name: "IX_Favorites_CreatedAt",
                table: "Favorites",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Favorites_PropertyId",
                table: "Favorites",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_Favorites_UserId",
                table: "Favorites",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Favorites_UserId_PropertyId",
                table: "Favorites",
                columns: new[] { "UserId", "PropertyId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FederalProvinces_Key",
                table: "FederalProvinces",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ForeclosureAuctionChanges_ChangeType",
                table: "ForeclosureAuctionChanges",
                column: "ChangeType");

            migrationBuilder.CreateIndex(
                name: "IX_ForeclosureAuctionChanges_CreatedAt",
                table: "ForeclosureAuctionChanges",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ForeclosureAuctionChanges_ForeclosureAuctionId",
                table: "ForeclosureAuctionChanges",
                column: "ForeclosureAuctionId");

            migrationBuilder.CreateIndex(
                name: "IX_ForeclosureAuctions_AuctionDate",
                table: "ForeclosureAuctions",
                column: "AuctionDate");

            migrationBuilder.CreateIndex(
                name: "IX_ForeclosureAuctions_Category",
                table: "ForeclosureAuctions",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_ForeclosureAuctions_City",
                table: "ForeclosureAuctions",
                column: "City");

            migrationBuilder.CreateIndex(
                name: "IX_ForeclosureAuctions_CreatedAt",
                table: "ForeclosureAuctions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ForeclosureAuctions_ExternalId",
                table: "ForeclosureAuctions",
                column: "ExternalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ForeclosureAuctions_IsActive",
                table: "ForeclosureAuctions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ForeclosureAuctions_PostalCode",
                table: "ForeclosureAuctions",
                column: "PostalCode");

            migrationBuilder.CreateIndex(
                name: "IX_ForeclosureAuctions_RegistrationNumber",
                table: "ForeclosureAuctions",
                column: "RegistrationNumber");

            migrationBuilder.CreateIndex(
                name: "IX_ForeclosureAuctions_State",
                table: "ForeclosureAuctions",
                column: "State");

            migrationBuilder.CreateIndex(
                name: "IX_ForeclosureAuctions_Status",
                table: "ForeclosureAuctions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_LegalSettings_SettingType_EffectiveDate",
                table: "LegalSettings",
                columns: new[] { "SettingType", "EffectiveDate" });

            migrationBuilder.CreateIndex(
                name: "IX_LegalSettings_SettingType_IsActive",
                table: "LegalSettings",
                columns: new[] { "SettingType", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ListingAnalyses_Status",
                table: "ListingAnalyses",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ListingAnalyses_UserId",
                table: "ListingAnalyses",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Municipalities_DistrictId",
                table: "Municipalities",
                column: "DistrictId");

            migrationBuilder.CreateIndex(
                name: "IX_Municipalities_Key",
                table: "Municipalities",
                column: "Key");

            migrationBuilder.CreateIndex(
                name: "IX_Municipalities_Name",
                table: "Municipalities",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationPreferences_UserId",
                table: "NotificationPreferences",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Properties_CreatedAt",
                table: "Properties",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Properties_MunicipalityId",
                table: "Properties",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_Properties_Price",
                table: "Properties",
                column: "Price");

            migrationBuilder.CreateIndex(
                name: "IX_Properties_SourceName_SourceId",
                table: "Properties",
                columns: new[] { "SourceName", "SourceId" },
                unique: true,
                filter: "\"SourceName\" IS NOT NULL AND \"SourceId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Properties_Type",
                table: "Properties",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Properties_UserId",
                table: "Properties",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyContactInfos_PropertyId",
                table: "PropertyContactInfos",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyContactInfos_PropertyId_DisplayOrder",
                table: "PropertyContactInfos",
                columns: new[] { "PropertyId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_PropertyContactInfos_Type",
                table: "PropertyContactInfos",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_PushSubscriptions_DeviceToken",
                table: "PushSubscriptions",
                column: "DeviceToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PushSubscriptions_UserId",
                table: "PushSubscriptions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Token",
                table: "RefreshTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SellerSources_Name",
                table: "SellerSources",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserFilterPreferences_UserId",
                table: "UserFilterPreferences",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_UserId_RoleType",
                table: "UserRoles",
                columns: new[] { "UserId", "RoleType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BlockedProperties");

            migrationBuilder.DropTable(
                name: "Favorites");

            migrationBuilder.DropTable(
                name: "ForeclosureAuctionChanges");

            migrationBuilder.DropTable(
                name: "LegalSettings");

            migrationBuilder.DropTable(
                name: "ListingAnalyses");

            migrationBuilder.DropTable(
                name: "NotificationPreferences");

            migrationBuilder.DropTable(
                name: "PropertyContactInfos");

            migrationBuilder.DropTable(
                name: "PushSubscriptions");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "SellerSources");

            migrationBuilder.DropTable(
                name: "UserFilterPreferences");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "ForeclosureAuctions");

            migrationBuilder.DropTable(
                name: "Properties");

            migrationBuilder.DropTable(
                name: "Municipalities");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Districts");

            migrationBuilder.DropTable(
                name: "FederalProvinces");
        }
    }
}
