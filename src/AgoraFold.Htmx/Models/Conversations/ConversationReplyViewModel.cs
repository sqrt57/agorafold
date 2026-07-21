using System.ComponentModel.DataAnnotations;

namespace AgoraFold.Htmx.Models.Conversations;

public class ConversationReplyViewModel
{
    [Required]
    [StringLength(4000)]
    public string Body { get; set; } = "";
}
