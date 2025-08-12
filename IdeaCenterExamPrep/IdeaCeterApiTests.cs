using System;
using System.Net;
using System.Text.Json;
using NUnit.Framework;
using RestSharp;
using RestSharp.Authenticators;
using IdeaCenterExamPrep.Models;
using System.Security.Cryptography.X509Certificates;

namespace IdeaCenterExamPrep
{
    [TestFixture]
    public class IdeaCenterApiTests
    {
        private RestClient client;
        private static string lastCreatedIdeaId;

        private const string BaseUrl = "http://softuni-qa-loadbalancer-2137572849.eu-north-1.elb.amazonaws.com:84";

        private const string StaticToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJKd3RTZXJ2aWNlQWNjZXNzVG9rZW4iLCJqdGkiOiJhZDVmYTkzMC02NmYwLTRjZWUtYjkyMi03YjM4YWI1OTI5NzUiLCJpYXQiOiIwOC8xMi8yMDI1IDE5OjIzOjA4IiwiVXNlcklkIjoiMDRkNDNjNzEtMmJmYi00MTlkLWQyOGUtMDhkZGQ0ZTA4YmQ4IiwiRW1haWwiOiJlbXJlZzkyQHRlc3QuY29tIiwiVXNlck5hbWUiOiJlbXJlZzkyIiwiZXhwIjoxNzU1MDQ4MTg4LCJpc3MiOiJJZGVhQ2VudGVyX0FwcF9Tb2Z0VW5pIiwiYXVkIjoiSWRlYUNlbnRlcl9XZWJBUElfU29mdFVuaSJ9.PbmGfXN3ZCwqFGC4wH7mpk1NDUW2XPkqA99YZWEAuZI";

        private const string LoginEmail = "emreg92@test.com";
        private const string LoginPassword = "123456";

        [OneTimeSetUp]

        public void Setup()
        {
            string jwtToken;

            if (!string.IsNullOrEmpty(StaticToken))
            {
               jwtToken = StaticToken;
            }
            else
            {
                jwtToken = GetJwtToken(LoginEmail, LoginPassword);
            }

            var options = new RestClientOptions(BaseUrl)
            {
                Authenticator = new JwtAuthenticator(jwtToken)
            };

            this.client = new RestClient(options);
        }

        private string GetJwtToken(string email, string password)
        {
            var tempCLient = new RestClient(BaseUrl);
            var request = new RestRequest("/api/User/Authentication", Method.Post);
            request.AddJsonBody(new{email,password});

            var response = tempCLient.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = JsonSerializer.Deserialize<JsonElement>(response.Content);
                var token = content.GetProperty("accessToken").GetString();

                if (string.IsNullOrWhiteSpace(token))
                {
                    throw new InvalidOperationException("Failed to retrive JWT token from the response.");
                }
                return token;
            }
            else
            {
                throw new InvalidOperationException($"Failed to authenticate user. Status code: {response.StatusCode}, Response: {response.Content}");
            }


        }
        //All tests here

        [Order(1)]
        [Test]
        public void CreateIdea_WithRequiredFields_ShouldReturnSuccess()
        {
            var ideaRequest = new IdeaDTO
            {
                Title = "Test Idea",
                Description = "This is a test idea description.",
                Url = ""
            };

            var request = new RestRequest("/api/Idea/Create", Method.Post);
            request.AddJsonBody(ideaRequest);
            var response = this.client.Execute(request);
            var createResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(createResponse.Msg, Is.EqualTo("Successfully created!"));
        }

        [Order(2)]
        [Test]
        public void GetAllIdeas_ShouldReturnListOfIdeas()
        {
            var request = new RestRequest("/api/Idea/All", Method.Get);
            var response = this.client.Execute(request);

            var responseItem = JsonSerializer.Deserialize<List<ApiResponseDTO>>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(responseItem, Is.Not.Null);
            Assert.That(responseItem, Is.Not.Empty);

            lastCreatedIdeaId = responseItem.LastOrDefault()?.Id; 
        }

        [Order(3)]
        [Test]

        public void EditExisitingIdea_ShouldSuccess()
        {
            var editRequest = new IdeaDTO
            {
                Title = "Edited Idea",
                Description = "This is an updated test idea description.",
                Url = ""
            };

            var request = new RestRequest($"/api/Idea/Edit",Method.Put);
            request.AddQueryParameter("ideaId", lastCreatedIdeaId);
            request.AddJsonBody(editRequest);
            var response = this.client.Execute(request);
            var editResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(editResponse.Msg, Is.EqualTo("Edited successfully"));
        }

        [Order(4)]
        [Test]

        public void DeleteIdea_ShouldSuccess()
        {
            var request = new RestRequest($"/api/Idea/Delete", Method.Delete);
            request.AddQueryParameter("ideaId", lastCreatedIdeaId);
            var response = this.client.Execute(request);
            

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content, Does.Contain("The idea is deleted!"));
        }

        [Order(5)]
        [Test]

        public void CreateIdea_WithoutRequiredFields_ShouldReturnSuccessAgain()
        {
            var ideaRequest = new IdeaDTO
            {
                Title = "Test Idea with Missing Fields",
                Description = ""
                
            };

            var request = new RestRequest("/api/Idea/Create", Method.Post);
            request.AddJsonBody(ideaRequest);
            var response = this.client.Execute(request);
            var createResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
           

        }

        [Order(6)]
        [Test]

        public void EditNonExistentIdea_ShouldReturnNotFound()
        {
            string nonExistentIdeaId = "123"; 
            var editRequest = new IdeaDTO
            {
                Title = "Edited Non-Existent Idea",
                Description = "This is an updated test idea description.",
                Url = ""
            };
            var request = new RestRequest($"/api/Idea/Edit", Method.Put);
            request.AddQueryParameter("ideaId", nonExistentIdeaId);
            request.AddJsonBody(editRequest);
            var response = this.client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(response.Content, Does.Contain("There is no such idea!"));
        }

        [Order(7)]
        [Test]

        public void DeleteNonExistentIdea_ShouldReturnNotFound()
        {
            string nonExistentIdeaId = "123"; 
            var request = new RestRequest($"/api/Idea/Delete", Method.Delete);
            request.AddQueryParameter("ideaId", nonExistentIdeaId);
            var response = this.client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(response.Content, Does.Contain("There is no such idea!"));
        }


        [OneTimeTearDown]
        public void Teardown()
        {
            this.client?.Dispose();
        }
    }
    
    }