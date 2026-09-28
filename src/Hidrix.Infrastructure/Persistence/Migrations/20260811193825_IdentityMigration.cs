using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hidrix.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class IdentityMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HidrtbRol",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HidrtbRol", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HidrtbUsuario",
                columns: table => new
                {
                    Usua_Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Usua_Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Ciu_Id = table.Column<int>(type: "int", nullable: true),
                    Usua_FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Usua_FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Usua_FechaActualizacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Usua_Activo = table.Column<bool>(type: "bit", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HidrtbUsuario", x => x.Usua_Id);
                    table.ForeignKey(
                        name: "FK_HidrtbUsuario_HidrtbCiudad_Ciu_Id",
                        column: x => x.Ciu_Id,
                        principalTable: "HidrtbCiudad",
                        principalColumn: "Ciu_Id");
                });

            migrationBuilder.CreateTable(
                name: "HidrtbRolClaim",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HidrtbRolClaim", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HidrtbRolClaim_HidrtbRol_RoleId",
                        column: x => x.RoleId,
                        principalTable: "HidrtbRol",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HidrtbUsuarioClaim",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HidrtbUsuarioClaim", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HidrtbUsuarioClaim_HidrtbUsuario_UserId",
                        column: x => x.UserId,
                        principalTable: "HidrtbUsuario",
                        principalColumn: "Usua_Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HidrtbUsuarioLogin",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HidrtbUsuarioLogin", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_HidrtbUsuarioLogin_HidrtbUsuario_UserId",
                        column: x => x.UserId,
                        principalTable: "HidrtbUsuario",
                        principalColumn: "Usua_Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HidrtbUsuarioRol",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HidrtbUsuarioRol", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_HidrtbUsuarioRol_HidrtbRol_RoleId",
                        column: x => x.RoleId,
                        principalTable: "HidrtbRol",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HidrtbUsuarioRol_HidrtbUsuario_UserId",
                        column: x => x.UserId,
                        principalTable: "HidrtbUsuario",
                        principalColumn: "Usua_Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HidrtbUsuarioToken",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HidrtbUsuarioToken", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_HidrtbUsuarioToken_HidrtbUsuario_UserId",
                        column: x => x.UserId,
                        principalTable: "HidrtbUsuario",
                        principalColumn: "Usua_Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "HidrtbRol",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_HidrtbRolClaim_RoleId",
                table: "HidrtbRolClaim",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "HidrtbUsuario",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_HidrtbUsuario_Ciu_Id",
                table: "HidrtbUsuario",
                column: "Ciu_Id");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "HidrtbUsuario",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_HidrtbUsuarioClaim_UserId",
                table: "HidrtbUsuarioClaim",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_HidrtbUsuarioLogin_UserId",
                table: "HidrtbUsuarioLogin",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_HidrtbUsuarioRol_RoleId",
                table: "HidrtbUsuarioRol",
                column: "RoleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HidrtbRolClaim");

            migrationBuilder.DropTable(
                name: "HidrtbUsuarioClaim");

            migrationBuilder.DropTable(
                name: "HidrtbUsuarioLogin");

            migrationBuilder.DropTable(
                name: "HidrtbUsuarioRol");

            migrationBuilder.DropTable(
                name: "HidrtbUsuarioToken");

            migrationBuilder.DropTable(
                name: "HidrtbRol");

            migrationBuilder.DropTable(
                name: "HidrtbUsuario");
        }
    }
}
