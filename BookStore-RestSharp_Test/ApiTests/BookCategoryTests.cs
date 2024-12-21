using Newtonsoft.Json.Linq;
using RestSharp;
using System.Net;

namespace ApiTests
{
    [TestFixture]
    public class BookCategoryTests : IDisposable
    {
        private RestClient client;
        private string token;
        Random random;
        public string title;

        [SetUp]
        public void Setup()
        {
            client = new RestClient(GlobalConstants.BaseUrl);
            token = GlobalConstants.AuthenticateUser("john.doe@example.com", "password123");

            Assert.That(token, Is.Not.Null.Or.Empty, "Authentication token should not be null or empty");
            random = new Random();
        }

        [Test]
        public void Test_BookCategoryLifecycle()
        {
            // Step 1: Create a new book category
            var createdRequest = new RestRequest("/category", Method.Post);
            createdRequest.AddHeader("Authorization", $"Bearer {token}");

            title = $"RandomTitle_{random.Next(99, 999)}";

            createdRequest.AddJsonBody(new { title });

            var createdResponse = client.Execute(createdRequest);


            Assert.Multiple(() =>
            {
                Assert.That(createdResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code OK (200)");
                Assert.That(createdResponse.Content, Is.Not.Empty, "Response content should not be empty");
            });
            
            var category = JObject.Parse(createdResponse.Content);

            Assert.That(category["title"]?.ToString(), Is.EqualTo(title));
            Assert.That(category["_id"]?.ToString(), Is.Not.Null.Or.Empty);

            var createdCategoryId = category["_id"]?.ToString();


            // Step 2: Retrieve all book categories and verify the newly created category is present

            var getAllcategory = new RestRequest("/category", Method.Get);
            var getAllCategoryResponse = client.Execute(getAllcategory);

            Assert.Multiple(() =>
            {
                Assert.That(getAllCategoryResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code OK (200)");
                Assert.That(getAllCategoryResponse.Content, Is.Not.Empty, "Response content should not be empty");

            });



            var allCategories = JArray.Parse(getAllCategoryResponse.Content);

            Assert.Multiple(() => 
            {
                Assert.That(allCategories.Type, Is.EqualTo(JTokenType.Array), "The response should be array");
                Assert.That(allCategories.Count, Is.GreaterThan(1));
            });


            // Find Created Category by id to verify the newly created category is present (write by Ivo Petrov)  
            var findCreatedCategory = allCategories.FirstOrDefault(b => b["_id"]?.ToString() == createdCategoryId);

            Assert.That(findCreatedCategory, Is.Not.Null.Or.Empty);

            

            // Step 3: Update the category title


            var updatedRequest = new RestRequest($"/category/{createdCategoryId}", Method.Put);
            updatedRequest.AddHeader("Authorization", $"Bearer {token}");

            title = $"Updated_RandomTitle_{random.Next(99, 999)}";

            updatedRequest.AddJsonBody(new { title });

            var updatedResponse = client.Execute(updatedRequest);


            Assert.Multiple(() =>
            {
                Assert.That(updatedResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code OK (200)");
                Assert.That(updatedResponse.Content, Is.Not.Empty, "Response content should not be empty");
            });


            // Step 4: Verify that the category details have been updated

            var getEditCategoryByIdRequest = new RestRequest($"category/{createdCategoryId}", Method.Get );
            var getEditCategoryByIdResponse = client.Execute(getEditCategoryByIdRequest);

            Assert.Multiple(() =>
            {
                Assert.That(getEditCategoryByIdResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code OK (200)");
                Assert.That(getEditCategoryByIdResponse.Content, Is.Not.Empty, "Response content should not be empty");

                var editCategory = JObject.Parse(getEditCategoryByIdResponse.Content);

                Assert.That(editCategory["title"]?.ToString(), Is.EqualTo(title));

            });




            // Step 5: Delete the category and validate it's no longer accessible

            var deleteRequest = new RestRequest($"/category/{createdCategoryId}", Method.Delete);
            deleteRequest.AddHeader("Authorization", $"Bearer {token}");

            var deleteResponse = client.Execute(deleteRequest);


            Assert.That(getEditCategoryByIdResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code OK (200)");


            // Step 6: Verify that the deleted category cannot be found

            var verifyDeleteRequest = new RestRequest($"category/{createdCategoryId}", Method.Get);
            var verifyDeleteResponse = client.Execute(verifyDeleteRequest);

            Assert.Multiple(() =>
            {
                Assert.That(verifyDeleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code OK (200)");
                Assert.That(verifyDeleteResponse.Content, Is.Not.Empty, "Response content should not be empty");
                Assert.That(verifyDeleteResponse.Content, Is.EqualTo("null"));
            });

        }

        public void Dispose()
        {
            client?.Dispose();
        }
    }
}
