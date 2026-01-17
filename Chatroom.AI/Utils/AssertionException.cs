using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chatroom.AI.Utils;

public class AssertionException : Exception
{
    public AssertionException()
    {
    }

    public AssertionException(string? message)
        : base(message)
    {
    }

    public AssertionException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
