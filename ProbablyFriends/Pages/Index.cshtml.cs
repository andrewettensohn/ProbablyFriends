using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ProbablyFriends.Pages
{
    public class IndexModel : PageModel
    {
        public List<string> FriendNames { get; set; }

        public IndexModel()
        {
            FriendNames = GetFriendNames();
        }

        public List<string> GetFriendNames()
        {
            return new List<string> { "Matt", "Michael", "Andrew" };
        }
    }
}