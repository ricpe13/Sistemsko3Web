using System.Collections.Generic;
using System.Threading.Tasks;
using Octokit;

namespace GithubIssues
{
    public static class GitHubHelper
    {
        public static async Task<IReadOnlyList<IssueComment>> GetIssueComments(GitHubClient client, string owner, string repo, int issueNumber)
        {
            return await client.Issue.Comment.GetAllForIssue(owner, repo, issueNumber);
        }
    }
}
