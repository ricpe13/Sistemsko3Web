using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using Octokit;

namespace GithubIssues
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var token = "OVDE SE PISE TOKEN";

            var productInformation = new ProductHeaderValue("GithubIssuesApp");
            var credentials = new Credentials(token);

            var gitHubClient = new GitHubClient(productInformation)
            {
                Credentials = credentials
            };

            var commentsSubject = new ReplaySubject<IssueComment>();

            var commentsObservable = commentsSubject.AsObservable();

            commentsObservable.Subscribe(
                comment => { /* Ne prikazujemo komentare u konzoli */ },
                ex => Console.WriteLine($"Error: {ex.Message}"),
                () => Console.WriteLine("Svi komentari su obradjeni."));

            var server = new HttpListener();
            server.Prefixes.Add("http://localhost:8080/");
            server.Start();
            Console.WriteLine("Server je pokrenut na http://localhost:8080/");

            while (true)
            {
                var context = await server.GetContextAsync();
                Task.Run(() => HandleRequest(context, gitHubClient, commentsSubject));
            }
        }

        static async void HandleRequest(HttpListenerContext context, GitHubClient gitHubClient, ReplaySubject<IssueComment> commentsSubject)
        {
            var request = context.Request;
            var response = context.Response;
            var logMessage = new StringBuilder();

            logMessage.AppendLine($"Primljen zahtev: {request.HttpMethod} {request.Url}");

            try
            {
                if (request.HttpMethod == "GET")
                {
                    var segments = request.Url.Segments;
                    if (segments.Length == 4)
                    {
                        var owner = segments[1].Trim('/');
                        var repo = segments[2].Trim('/');
                        var issueNumberString = segments[3].Trim('/');

                        if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(repo) || !int.TryParse(issueNumberString, out var issueNumber))
                        {
                            logMessage.AppendLine("Nisi uneo neki parametar");
                            response.StatusCode = (int)HttpStatusCode.BadRequest;
                            using var writer = new StreamWriter(response.OutputStream);
                            writer.Write("Nisi uneo neki parametar"); //ispis u pretrazivacu
                        }
                        else
                        {
                            var comments = await GitHubHelper.GetIssueComments(gitHubClient, owner, repo, issueNumber);
                            foreach (var comment in comments)
                            {
                                commentsSubject.OnNext(comment);
                            }
                            commentsSubject.OnCompleted();

                            // Perform topic modeling
                            var documents = comments.Select(c => new Document { Text = c.Body }).ToList();
                            var topicResults = TopicModeling.PerformTopicModeling(documents);

                            var responseString = new StringBuilder();
                            responseString.AppendLine("Komentari:");
                            responseString.AppendLine(string.Join("\n", comments.Select(c => $"Komentarisao {c.User.Login}: {c.Body}")));

                            responseString.AppendLine("\nRezultati topic modelinga:");
                            foreach (var result in topicResults)
                            {
                                responseString.AppendLine($"Komentar: {result.Text}");
                                for (int i = 0; i < result.Topics.Length; i++)
                                {
                                    responseString.AppendLine($"  Tema {i}: {result.Topics[i]}");
                                }
                                responseString.AppendLine();
                            }

                            logMessage.AppendLine("Zahtev uspesno obradjen");
                            response.StatusCode = (int)HttpStatusCode.OK;
                            using var writer = new StreamWriter(response.OutputStream);
                            writer.Write(responseString.ToString());
                        }
                    }
                    else
                    {
                        logMessage.AppendLine("Uneo si previse ili premalo parametara");
                        response.StatusCode = (int)HttpStatusCode.NotFound;
                        using var writer = new StreamWriter(response.OutputStream);
                        writer.Write("Uneo si previse ili premalo parametara");
                    }
                }
                else
                {
                    logMessage.AppendLine("Nije GET metoda");
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    using var writer = new StreamWriter(response.OutputStream);
                    writer.Write("Nije GET metoda");
                }
            }
            catch (Exception ex)
            {
                logMessage.AppendLine($"Doslo je do greske: {ex.Message}");
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                using var writer = new StreamWriter(response.OutputStream);
                writer.Write("Doslo je do greske prilikom obrade zahteva");
            }
            finally
            {
                response.Close();
                Console.WriteLine(logMessage.ToString());
            }
        }
    }
}
