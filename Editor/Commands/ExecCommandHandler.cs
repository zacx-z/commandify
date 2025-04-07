using UnityEditor;
using UnityEditor.Search;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Commandify
{
    public class ExecCommandHandler : ICommandHandler
    {
        public async Task<string> ExecuteAsync(List<string> args, CommandContext context)
        {
            if (args.Count == 0)
                return "Error: Please specify a menu path or --help/--search for options";

            if (args[0] == "--help")
                return "Usage:\n  exec <menu-path>     Execute menu item\n  exec --search [pattern]  Search menu items matching pattern\n  exec --help         Show this help";

            if (args[0] == "--search")
            {
                string pattern = args.Count > 1 ? string.Join(" ", args.Skip(1)) : "";
                return ListMenuItems(pattern);
            }

            string menuPath = string.Join(" ", args);
            EditorApplication.ExecuteMenuItem(menuPath);
            return $"Executed menu item: {menuPath}";
        }

        private struct MenuItemInfo
        {
            public string menuPath;
        }

        private List<MenuItemInfo> GetAllMenuItems(string searchPattern)
        {
            var items = new List<MenuItemInfo>();
            var context = SearchService.CreateContext("menu");
            context.searchText = $"m:{searchPattern}";
            context.wantsMore = true;

            var searchItems = SearchService.GetItems(context);
            foreach (var item in searchItems)
            {
                items.Add(new MenuItemInfo { menuPath = item.id });
            }

            return items;
        }

        private string ListMenuItems(string pattern)
        {
            var items = GetAllMenuItems(pattern);
            if (items.Count == 0)
                return pattern == "" ? "No menu items found" : $"No menu items found matching '{pattern}'";

            var output = pattern == "" ? "Available menu items:\n" : $"Menu items matching '{pattern}':\n";
            foreach (var item in items.OrderBy(i => i.menuPath))
            {
                output += $"{item.menuPath}\n";
            }
            return output;
        }
    }
}
