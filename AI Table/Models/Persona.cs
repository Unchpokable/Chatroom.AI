using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chatroom.AI.Models;

internal sealed class Persona
{
    public required string ApiModelName { get; set; }
    public required string ModelNameAlias { get; set; }
    public required string AvatarName { get; set; }

    public string SpecialInstructions { get; set; } = string.Empty;
    public string Personality { get; set; } = string.Empty;
}
