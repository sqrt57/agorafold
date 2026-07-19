using System.ComponentModel.DataAnnotations;

namespace AgoraFold.Mvc.Models.Conversations;

public class ConversationReplyViewModel
{
    [Required]
    [StringLength(4000)]
    public string Body { get; set; } = "";
}
