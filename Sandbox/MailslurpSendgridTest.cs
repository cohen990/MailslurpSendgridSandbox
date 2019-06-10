using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Sandbox
{
    [TestFixture]
    class MailslurpSendgridTest
    {
        private readonly Uri _mailslurpEndpoint = new Uri("https://api.mailslurp.com/");
        private readonly HttpClient _httpClient = new HttpClient();
        private const string MailslurpApiKey = "<your mailslurp api key https://app.mailslurp.com >";
        private const string SendgridApiKey = "<your sendgrid api key https://app.sendgrid.com/ >";

        [Test]
        public async Task CreateInbox_SendEmail_RetrieveEmail()
        {
            // Arrange
            var uniquelyIdentifiableString = Guid.NewGuid().ToString();
            var inbox = await CreateInbox();

            // Act
            await SendEmailTo(inbox.emailAddress, uniquelyIdentifiableString);
            var emailIdentifier = await GetEmailIdentifier(inbox.id);
            var emailResponse = await GetEmail(emailIdentifier.id);
            var emailResponseJson = await emailResponse.Content.ReadAsStringAsync();

            // Assert
            Assert.That(emailResponse.IsSuccessStatusCode);
            Assert.That(emailResponseJson, Does.Contain(uniquelyIdentifiableString));
        }

        private async Task<HttpResponseMessage> GetEmail(string emailId)
        {
            var emailPath = $"/emails/{emailId}";
            var emailUri = new Uri(_mailslurpEndpoint, emailPath);
            var emailRequest = new HttpRequestMessage();

            emailRequest.Headers.Add("x-api-key", MailslurpApiKey);
            emailRequest.RequestUri = emailUri;
            emailRequest.Method = HttpMethod.Get;

            var emailResponse = await _httpClient.SendAsync(emailRequest);
            return emailResponse;
        }

        private async Task<EmailIdentifier> GetEmailIdentifier(string inboxId)
        {
            var inboxContentsPath = $"/inboxes/{inboxId}/emails";
            var inboxContentsUri = new Uri(_mailslurpEndpoint, inboxContentsPath);
            var getInboxContents = new HttpRequestMessage();

            getInboxContents.Headers.Add("x-api-key", MailslurpApiKey);
            getInboxContents.RequestUri = inboxContentsUri;
            getInboxContents.Method = HttpMethod.Get;

            Thread.Sleep(5000);
            var inboxContentsResponse = await _httpClient.SendAsync(getInboxContents);
            var inboxContentsJson = await inboxContentsResponse.Content.ReadAsStringAsync();
            var emailIdentifiers = JsonConvert.DeserializeObject<List<EmailIdentifier>>(inboxContentsJson);
            var emailIdentifier = emailIdentifiers.Single();
            return emailIdentifier;
        }

        private static async Task SendEmailTo(string recipient, string emailContent)
        {
            var client = new SendGridClient(SendgridApiKey);
            var from = new EmailAddress("test@example.com", "Example User");
            var subject = "Sending with SendGrid is Fun";
            var to = new EmailAddress(recipient, "Example User");
            var plainTextContent = "and easy to do anywhere, even with C#";
            var htmlContent = $"<strong>and easy to do anywhere, even with C#</strong>{emailContent}";
            var msg = MailHelper.CreateSingleEmail(@from, to, subject, plainTextContent, htmlContent);
            await client.SendEmailAsync(msg);
        }

        private async Task<CreatedInbox> CreateInbox()
        {
            var listPath = "/inboxes";

            var requestUri = new Uri(_mailslurpEndpoint, listPath);

            var request = new HttpRequestMessage();

            request.Headers.Add("x-api-key", MailslurpApiKey);
            request.RequestUri = requestUri;
            request.Method = HttpMethod.Post;

            var result = await _httpClient.SendAsync(request);
            var responseJson = await result.Content.ReadAsStringAsync();
            var inbox = JsonConvert.DeserializeObject<CreatedInbox>(responseJson);
            return inbox;
        }

        public class CreatedInbox
        {
            public string id { get; set; }
            public string userId { get; set; }
            public DateTime created { get; set; }
            public string emailAddress { get; set; }
        }

        public class EmailIdentifier
        {
            public string id { get; set; }
            public DateTime created { get; set; }
        }
    }
}
