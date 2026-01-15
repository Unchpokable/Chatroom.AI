using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Chatroom.AI.Core;

record SseChunk(
    string Id,
    string Object,
    Choice[] Choices
);

record Choice(
    int Index,
    Delta Delta,
    [property: JsonPropertyName("finish_reason")] string? FinishReason
);

record Delta(
    string? Content,
    string? Role
);