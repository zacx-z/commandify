using System.Collections.Generic;
using System.Threading.Tasks;

namespace Commandify
{
    public interface ICommandHandler
    {
        Task<string> ExecuteAsync(List<string> args, CommandContext context);
    }
}
