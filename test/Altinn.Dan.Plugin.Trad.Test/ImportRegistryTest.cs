using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net.Http;
using Moq;
using Microsoft.Extensions.Logging;
using Altinn.Dan.Plugin.Trad.Config;
using System;
using System.Linq;
using System.Net;
using Moq.Protected;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Options;

namespace Altinn.Dan.Plugin.Trad.Test
{
    [TestClass]
    public class ImportRegistryTest
    {
        private readonly Mock<IHttpClientFactory> _mockFactory = new();
        private readonly ILoggerFactory _loggerFactory = new LoggerFactory();

        [TestInitialize]
        public void Initialize()
        {
            _mockFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(MakeFakeClient());
        }

        [TestMethod]
        public async Task RegistryImportSerializationTest()
        {

            // Setup 
            var mockCache = new MockCache();
            var options = Options.Create(new ApplicationSettings() { RegistryURL = "http://some_url.blahblah.nope", ApiKey = "secretapikey"});
            var func = new ImportRegistry(_loggerFactory, _mockFactory.Object, options, mockCache);
            var timer = new MyInfo
            {
                ScheduleStatus = new MyScheduleStatus() { Last = DateTime.Now, Next = DateTime.Now, LastUpdated = DateTime.Now }
            };

            // Act
            await func.RunAsync(timer);

            // Assert
            // - 100 should have one authorizedRepresentative: 200
            // - 101 should have two authorizedRepresentatives: 200, 201
            // - 200 should have two isRepresentativeFor: 100, 101
            // - 201 should have one isRepresentativeFor: 101

            var backingStore = mockCache.GetAll();

            var person100 = backingStore[Helpers.GetCacheKeyForSsn("100")];
            Assert.IsTrue(person100.AuthorizedRepresentatives.Count == 1);
            Assert.IsTrue(person100.AuthorizedRepresentatives.First().Ssn == "200");
            Assert.IsNull(person100.IsaAuthorizedRepresentativeFor);

            var person101 = backingStore[Helpers.GetCacheKeyForSsn("101")];
            Assert.IsTrue(person101.AuthorizedRepresentatives.Count == 2);
            Assert.IsTrue(person101.AuthorizedRepresentatives.Any(x => x.Ssn == "200"));
            Assert.IsTrue(person101.AuthorizedRepresentatives.Any(x => x.Ssn == "201"));
            Assert.IsNull(person101.IsaAuthorizedRepresentativeFor);

            var person200 = backingStore[Helpers.GetCacheKeyForSsn("200")];
            Assert.IsTrue(person200.IsaAuthorizedRepresentativeFor.Count == 2);
            Assert.IsTrue(person200.IsaAuthorizedRepresentativeFor.Any(x => x.Ssn == "100"));
            Assert.IsTrue(person200.IsaAuthorizedRepresentativeFor.Any(x => x.Ssn == "101"));
            Assert.IsNull(person200.AuthorizedRepresentatives);

            var person201 = backingStore[Helpers.GetCacheKeyForSsn("201")];
            Assert.IsTrue(person201.IsaAuthorizedRepresentativeFor.Count == 1);
            Assert.IsTrue(person201.IsaAuthorizedRepresentativeFor.First().Ssn == "101");
            Assert.IsNull(person201.AuthorizedRepresentatives);
        }

        private string GetRegistryTestData()
        {
            return @"[
                {
                    ""ssn"": ""200"",
                    ""firstName"": ""x"",
                    ""lastName"": ""y"",
                    ""title"": ""advokatfullmektig"",
                    ""authorizedRepresentatives"": null
                },
                {
                    ""ssn"": ""100"",
                    ""firstName"": ""a"",
                    ""lastName"": ""b"",
                    ""title"": ""advokat"",
                    ""isAssociatedWithAuditedBusiness"": true,
                    ""authorizedRepresentatives"": [
                        {
                            ""ssn"": ""200"",
                            ""firstName"": ""x"",
                            ""lastName"": ""y"",
                            ""title"": ""advokatfullmektig"",
                            ""isAssociatedWithAuditedBusiness"": true,
                            ""authorizedRepresentatives"": null
                        }
                    ]
                },
                {
                    ""ssn"": ""101"",
                    ""firstName"": ""a"",
                    ""lastName"": ""b"",
                    ""title"": ""advokat"",
                    ""isAssociatedWithAuditedBusiness"": false,
                    ""authorizedRepresentatives"": [
                        {
                            ""ssn"": ""200"",
                            ""firstName"": ""x"",
                            ""lastName"": ""y"",
                            ""title"": ""advokatfullmektig"",
                            ""isAssociatedWithAuditedBusiness"": true,
                            ""authorizedRepresentatives"": null
                        },
                        {
                            ""ssn"": ""201"",
                            ""firstName"": ""x"",
                            ""lastName"": ""y"",
                            ""title"": ""advokatfullmektig"",
                            ""isAssociatedWithAuditedBusiness"": false,
                            ""authorizedRepresentatives"": null
                        }
                    ]
                },
                {
                    ""ssn"": ""201"",
                    ""firstName"": ""x"",
                    ""lastName"": ""y"",
                    ""title"": ""advokatfullmektig"",
                    ""authorizedRepresentatives"": null
                }
            ]";
        }

        private HttpClient MakeFakeClient()
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
                    (_, _) =>
                    {
                    });

            return new HttpClient(handler.Object);
        }
    }
}
