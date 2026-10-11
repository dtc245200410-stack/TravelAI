using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelAI.Migrations
{
    /// <inheritdoc />
    public partial class AddAIConversationScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AIConversations_UserId",
                table: "AIConversations");

            migrationBuilder.AddColumn<string>(
                name: "Scope",
                table: "AIConversations",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Travel");

            migrationBuilder.CreateIndex(
                name: "IX_AIConversations_UserId_Scope_UpdatedAt",
                table: "AIConversations",
                columns: new[] { "UserId", "Scope", "UpdatedAt" });
            // Old admin chats used the same table; move them to the admin scope.
            migrationBuilder.Sql("""
                UPDATE c SET c.[Scope] = 'Admin'
                FROM [AIConversations] c
                INNER JOIN [Users] u ON u.[Id] = c.[UserId]
                WHERE u.[Role] = 'Admin';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AIConversations_UserId_Scope_UpdatedAt",
                table: "AIConversations");

            migrationBuilder.DropColumn(
                name: "Scope",
                table: "AIConversations");

            migrationBuilder.CreateIndex(
                name: "IX_AIConversations_UserId",
                table: "AIConversations",
                column: "UserId");
        }
    }
}
