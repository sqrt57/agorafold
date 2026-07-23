using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgoraFold.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddMessageClientMessageId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "client_message_id",
                table: "messages",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_messages_conversation_id_sender_id_client_message_id",
                table: "messages",
                columns: new[] { "conversation_id", "sender_id", "client_message_id" },
                unique: true,
                filter: "client_message_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_messages_conversation_id_sender_id_client_message_id",
                table: "messages");

            migrationBuilder.DropColumn(
                name: "client_message_id",
                table: "messages");
        }
    }
}
