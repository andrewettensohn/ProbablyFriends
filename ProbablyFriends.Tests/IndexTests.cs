using ProbablyFriends.Pages;
using System.Collections.Generic;
using Xunit;

namespace ProbablyFriends.Tests
{
    public class IndexTests
    {
        [Fact]
        public void AreThreeFriendNamesReturned_GetFriendNames()
        {
            IndexModel indexModel = new IndexModel();

            List<string> friendNames = indexModel.GetFriendNames();

            Assert.True(friendNames.Count == 3);
        }
    }
}