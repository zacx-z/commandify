using System.Collections.Generic;

namespace Commandify
{
    public interface ICommandHandler
    {
        string Execute(List<string> args, CommandContext context);
    }
}
