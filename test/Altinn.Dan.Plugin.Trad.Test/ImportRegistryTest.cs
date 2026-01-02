using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Altinn.ApiClients.Maskinporten.Interfaces;
using Altinn.Dan.Plugin.Trad.Config;
using Altinn.Dan.Plugin.Trad.Services;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StackExchange.Redis;

namespace Altinn.Dan.Plugin.Trad.Test
{
    [TestClass]
    public class ImportRegistryTest
    {
        private readonly IHttpClientFactory _fakeHttpFactory = A.Fake<IHttpClientFactory>();
        private readonly IConnectionMultiplexer _fakeConnectionMultiplexer = A.Fake<IConnectionMultiplexer>();
        private readonly IOrganizationService _fakeOrganizationService = A.Fake<IOrganizationService>();
        private readonly ILoggerFactory _loggerFactory = new LoggerFactory();

        [TestInitialize]
        public void Initialize()
        {
            A.CallTo(() => _fakeHttpFactory.CreateClient(A<string>._)).Returns(MakeFakeClient());
        }

        [TestMethod]
        public async Task RegistryImportSerializationTest()
        {

            // Setup 
            var mockCache = new MockCache();
            var options = Options.Create(new ApplicationSettings() { RegistryURL = "http://some_url.blahblah.nope", ApiKey = "secretapikey"});
            var func = new ImportRegistry(
                _loggerFactory,
                _fakeHttpFactory, 
                options, 
                mockCache,
                _fakeConnectionMultiplexer,
                _fakeOrganizationService);

            // Act
            await func.PerformUpdate();

            // Assert

            var backingStore = mockCache.GetAllPeople();

            // - 100 should have one pracice (500), one authorizedRepresentative (200)
            var person100 = backingStore[Helpers.GetCacheKeyForSsn("100")];
            Assert.HasCount(1, person100.Practices);
            Assert.HasCount(1, person100.Practices[0].AuthorizedRepresentatives);
            Assert.IsNull(person100.Practices[0].IsAnAuthorizedRepresentativeFor);

            // - 101 should have two pracices (500, 501), each with two authorizedRepresentatives, three distinct: 200, 201, 202
            var person101 = backingStore[Helpers.GetCacheKeyForSsn("101")];
            Assert.HasCount(2, person101.Practices);
            Assert.HasCount(2, person101.Practices[0].AuthorizedRepresentatives);
            Assert.IsTrue(person101.Practices[0].AuthorizedRepresentatives.Any(x => x.Ssn == "200"));
            Assert.IsTrue(person101.Practices[0].AuthorizedRepresentatives.Any(x => x.Ssn == "201"));
            Assert.IsNull(person101.Practices[0].IsAnAuthorizedRepresentativeFor);
            Assert.HasCount(2, person101.Practices[1].AuthorizedRepresentatives);
            Assert.IsTrue(person101.Practices[1].AuthorizedRepresentatives.Any(x => x.Ssn == "201"));
            Assert.IsTrue(person101.Practices[1].AuthorizedRepresentatives.Any(x => x.Ssn == "202"));
            Assert.IsNull(person101.Practices[1].IsAnAuthorizedRepresentativeFor);

            // - 200 should have one practice (500), with two isRepresentativeFor (100, 101)
            var person200 = backingStore[Helpers.GetCacheKeyForSsn("200")];
            Assert.HasCount(1, person200.Practices);
            Assert.HasCount(2, person200.Practices[0].IsAnAuthorizedRepresentativeFor);
            Assert.IsTrue(person200.Practices[0].IsAnAuthorizedRepresentativeFor.Any(x => x.Ssn == "100"));
            Assert.IsTrue(person200.Practices[0].IsAnAuthorizedRepresentativeFor.Any(x => x.Ssn == "101"));
            Assert.IsNull(person200.Practices[0].AuthorizedRepresentatives);

            // - 201 should have three practices (500, 501, 502), first with one isRepresentativeFor: (101), second with two isRepresentativeFor: (101, 102), last one with one isRepresentativeFor: (102)
            var person201 = backingStore[Helpers.GetCacheKeyForSsn("201")];
            Assert.HasCount(3, person201.Practices);
            Assert.HasCount(1, person201.Practices[0].IsAnAuthorizedRepresentativeFor);
            Assert.IsTrue(person201.Practices[0].IsAnAuthorizedRepresentativeFor.Any(x => x.Ssn == "101"));
            Assert.IsNull(person201.Practices[0].AuthorizedRepresentatives);
            Assert.HasCount(2, person201.Practices[1].IsAnAuthorizedRepresentativeFor);
            Assert.IsTrue(person201.Practices[1].IsAnAuthorizedRepresentativeFor.Any(x => x.Ssn == "101"));
            Assert.IsTrue(person201.Practices[1].IsAnAuthorizedRepresentativeFor.Any(x => x.Ssn == "102"));
            Assert.IsNull(person201.Practices[1].AuthorizedRepresentatives);
            Assert.HasCount(1, person201.Practices[2].IsAnAuthorizedRepresentativeFor);
            Assert.IsTrue(person201.Practices[2].IsAnAuthorizedRepresentativeFor.Any(x => x.Ssn == "102"));
            Assert.IsNull(person201.Practices[2].AuthorizedRepresentatives);

            // - 103 should have one practice (503), with one authorizedRepresentative: (104) who is an advokat
            var person103 = backingStore[Helpers.GetCacheKeyForSsn("103")];
            Assert.HasCount(1, person103.Practices);
            Assert.HasCount(1, person103.Practices[0].AuthorizedRepresentatives);
            Assert.IsTrue(person103.Practices[0].AuthorizedRepresentatives.Any(x => x.Ssn == "104"));

            // - 104 should have two practices (503, 504), first with one isRepresentativeFor: (103), second one with one authorizedRepresentative (204)
            var person104 = backingStore[Helpers.GetCacheKeyForSsn("104")];
            Assert.HasCount(2, person104.Practices);
            Assert.HasCount(1, person104.Practices[0].IsAnAuthorizedRepresentativeFor);
            Assert.IsTrue(person104.Practices[0].IsAnAuthorizedRepresentativeFor.Any(x => x.Ssn == "103"));
            Assert.HasCount(1, person104.Practices[1].AuthorizedRepresentatives);
            Assert.IsTrue(person104.Practices[1].AuthorizedRepresentatives.Any(x => x.Ssn == "204"));

            // TODO check when both isRepresentativeFor and authorizedRepresentative in same practice

        }

        private string GetRegistryTestData()
        {
            return @"[
                {
                    ""ssn"": ""100"",
                    ""firstName"": ""a"",
                    ""lastName"": ""a"",
                    ""title"": ""Advokat"",
                    ""practices"": [
                        {
                            ""orgNumber"": 500,
                            ""auditExcempt"": false,
                            ""authorizedRepresentatives"": [
                                {
                                    ""ssn"": ""200"",
                                    ""firstName"": ""x"",
                                    ""lastName"": ""x"",
                                    ""title"": ""Advokatfullmektig"",
                                    ""practices"": null
                                }
                            ]
                        }
                    ]
                },
                {
                    ""ssn"": ""101"",
                    ""firstName"": ""b"",
                    ""lastName"": ""b"",
                    ""title"": ""Advokat"",
                    ""practices"": [
                        {
                            ""orgNumber"": 500,
                            ""auditExcempt"": false,
                            ""authorizedRepresentatives"": [
                                {
                                    ""ssn"": ""200"",
                                    ""firstName"": ""x"",
                                    ""lastName"": ""x"",
                                    ""title"": ""Advokatfullmektig"",
                                    ""practices"": null
                                },
                                {
                                    ""ssn"": ""201"",
                                    ""firstName"": ""y"",
                                    ""lastName"": ""y"",
                                    ""title"": ""Advokatfullmektig"",
                                    ""practices"": null
                                }
                            ]
                        },
                        {
                            ""orgNumber"": 501,
                            ""auditExcempt"": true,
                            ""authorizedRepresentatives"": [
                                {
                                    ""ssn"": ""201"",
                                    ""firstName"": ""y"",
                                    ""lastName"": ""y"",
                                    ""title"": ""Advokatfullmektig"",
                                    ""practices"": null
                                },
                                {
                                    ""ssn"": ""202"",
                                    ""firstName"": ""z"",
                                    ""lastName"": ""z"",
                                    ""title"": ""Advokatfullmektig"",
                                    ""practices"": null
                                }
                            ]
                        }
                    ]
                },
                {
                    ""ssn"": ""102"",
                    ""firstName"": ""c"",
                    ""lastName"": ""c"",
                    ""title"": ""Advokat"",
                    ""practices"": [
                        {
                            ""orgNumber"": 501,
                            ""auditExcempt"": true,
                            ""authorizedRepresentatives"": [
                                {
                                    ""ssn"": ""201"",
                                    ""firstName"": ""y"",
                                    ""lastName"": ""y"",
                                    ""title"": ""Advokatfullmektig"",
                                    ""practices"": null
                                }
                            ]
                        },
                        {
                            ""orgNumber"": 502,
                            ""auditExcempt"": true,
                            ""authorizedRepresentatives"": [
                                {
                                    ""ssn"": ""201"",
                                    ""firstName"": ""y"",
                                    ""lastName"": ""y"",
                                    ""title"": ""Advokatfullmektig"",
                                    ""practices"": null
                                },
                                {
                                    ""ssn"": ""203"",
                                    ""firstName"": ""zz"",
                                    ""lastName"": ""zz"",
                                    ""title"": ""Advokatfullmektig"",
                                    ""practices"": null
                                }
                            ]
                        }
                    ]
                },
                {
                    ""ssn"": ""103"",
                    ""firstName"": ""c"",
                    ""lastName"": ""c"",
                    ""title"": ""Advokat"",
                    ""practices"": [
                        {
                            ""orgNumber"": 503,
                            ""auditExcempt"": true,
                            ""authorizedRepresentatives"": [
                                {
                                    ""ssn"": ""104"",
                                    ""firstName"": ""y"",
                                    ""lastName"": ""y"",
                                    ""title"": ""Advokat"",
                                    ""practices"": null
                                }
                            ]
                        }
                    ]
                },
                {
                    ""ssn"": ""104"",
                    ""firstName"": ""c"",
                    ""lastName"": ""c"",
                    ""title"": ""Advokat"",
                    ""practices"": [
                        {
                            ""orgNumber"": 504,
                            ""auditExcempt"": true,
                            ""authorizedRepresentatives"": [
                                {
                                    ""ssn"": ""204"",
                                    ""firstName"": ""y"",
                                    ""lastName"": ""y"",
                                    ""title"": ""Advokatfullmektig"",
                                    ""practices"": null
                                }
                            ]
                        }
                    ]
                }

            ]";
        }

        private HttpClient MakeFakeClient()
        {
            return GetHttpClientMock(GetRegistryTestData());
        }

        public static HttpClient GetHttpClientMock(string responseBody = "")
        {
            var handler = new FakeHttpMessageHandler(responseBody);
            return new HttpClient(handler);
        }
    }
    
    public class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _responseBody;

        public FakeHttpMessageHandler(string responseBody = "")
        {
            _responseBody = responseBody;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_responseBody)
            };
            return Task.FromResult(response);
        }
    }
}
