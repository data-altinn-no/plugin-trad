using Microsoft.VisualStudio.TestTools.UnitTesting;
using Altinn.Dan.Plugin.Trad;
using System.Net.Http;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Altinn.Dan.Plugin.Trad.Config;
using System;
using System.Net;
using Newtonsoft.Json;
using Moq.Protected;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Options;
using System.Text;

namespace Altinn.Dan.Plugin.Trad.Test
{
    [TestClass]
    public class RegistryImportTest
    {
        private readonly Mock<IHttpClientFactory> mockFactory = new Mock<IHttpClientFactory>();
        private readonly ILoggerFactory loggerFactory = new LoggerFactory();
        private readonly Mock<IDistributedCache> mockCache = new Mock<IDistributedCache>();

        private readonly HttpClient client = new HttpClient();

        [TestInitialize]
        public void Initialize()
        {
            mockFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(makeFakeClient());
        }

        [TestMethod]
        [Ignore]
        public async Task RegistryImportSerializationTest()
        {
            
            var options = Options.Create(new ApplicationSettings() { RegistryURL = "http://some_url.blahblah.nope", KeyVaultName = "no.such.keyvault", ApiKeySecret = "secretapikey"});
            var response = GetRegistryTestData();

           var func = new ImportRegistry(loggerFactory, mockFactory.Object, options, mockCache.Object);

            var timer = new MyInfo();
            timer.ScheduleStatus = new MyScheduleStatus() { Last = DateTime.Now, Next = DateTime.Now, LastUpdated = DateTime.Now };
            await func.RunAsync(timer);

            var plainTextBytes = Encoding.UTF8.GetBytes("tr-registry-01010023432");
            var key = Convert.ToBase64String(plainTextBytes);

            Assert.IsTrue(response.Contains("01010023432"));
            mockCache.Verify(c => c.Set(key, It.IsAny<byte[]>(), null), Times.Once);
            mockCache.Verify(c => c.Set(It.IsAny<string>(), It.IsAny<byte[]>(), null), Times.Once);
        }

        private string GetRegistryTestData()
        {
            return @"[
                {
                    ""ssn"": ""01010023432"",
                    ""firstName"": ""TestName"",
                    ""lastName"": ""LastTestName"",
                    ""title"": ""Advokat"",
                    ""authorizedRepresentatives"": null
                }
            ]";
        }

        private HttpClient makeFakeClient()
        {
            return GetHttpClientMock(GetRegistryTestData());
        }

        public static HttpClient GetHttpClientMock(string responseBody = "")
        {
            var handler = new Mock<HttpMessageHandler>();

            handler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns(Task<HttpResponseMessage>.Factory.StartNew(() =>
                {
                    return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(responseBody) };
                })).Callback<HttpRequestMessage, CancellationToken>(
                    (r, c) =>
                    {
                    });

            return new HttpClient(handler.Object);
        }
    }
}
