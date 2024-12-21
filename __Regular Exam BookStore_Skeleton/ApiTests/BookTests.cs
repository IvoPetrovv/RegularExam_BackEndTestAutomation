using Newtonsoft.Json.Linq;
using RestSharp;
using System.Diagnostics;
using System.Net;
using static System.Reflection.Metadata.BlobBuilder;

namespace ApiTests
{
    [TestFixture]
    public class BookTests : IDisposable
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

        [Test, Order(1)]
        public void Test_GetAllBooks()
        {
            //Arrange
            var getAllBookRequest = new RestRequest("/book", Method.Get);

            //Act
            var getAllBookResponse = client.Execute(getAllBookRequest);

            //Assert
            Assert.Multiple(() => 
            {
                Assert.That(getAllBookResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code OK (200)");
                Assert.That(getAllBookResponse.Content, Is.Not.Empty.Or.Null, "Response content should not be empty");


                var books = JArray.Parse(getAllBookResponse.Content);

                Assert.That(books.Count, Is.GreaterThan(0), "The books should be more than one");
                Assert.That(books.Type, Is.EqualTo(JTokenType.Array), " Response content should be array ");

                foreach (var book in books)
                {
                    Assert.That(book["title"]?.ToString(), Is.Not.Null.Or.Empty, "Property title should not be empty");
                    Assert.That(book["author"]?.ToString(), Is.Not.Null.Or.Empty, "Property author should not be empty");
                    Assert.That(book["description"]?.ToString(), Is.Not.Null.Or.Empty, "Property description should not be empty");
                    Assert.That(book["price"]?.ToString(), Is.Not.Null.Or.Empty, "Property price should not be empty");
                    Assert.That(book["pages"]?.ToString(), Is.Not.Null.Or.Empty, "Property pages should not be empty");
                    Assert.That(book["category"]?.ToString(), Is.Not.Null.Or.Empty, "Property category should not be empty");
                }

            });
        
        }

        [Test, Order(2)]
        public void Test_GetBookByTitle()
        {
            //Arrange
            var getAllBookRequest = new RestRequest("/book", Method.Get);
            var getAllBookResponse = client.Execute(getAllBookRequest);

            Assert.Multiple(() =>
            {
                Assert.That(getAllBookResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code OK (200)");
                Assert.That(getAllBookResponse.Content, Is.Not.Empty, "Response content should not be empty");

            });

            var books = JArray.Parse(getAllBookResponse.Content);

            var bookByTitle = books.FirstOrDefault(b => b["title"]?.ToString() == "The Great Gatsby");

            Assert.That(bookByTitle, Is.Not.Empty.Or.Null);
            Assert.That(bookByTitle["author"]?.ToString(), Is.EqualTo("F. Scott Fitzgerald"));
           
        }

        [Test, Order(3)]
        public void Test_AddBook()
        {
            //Get all category
            var getAllCategoryRequest = new RestRequest("/category", Method.Get);
            var getAllCategoryResponse = client.Execute(getAllCategoryRequest);

            Assert.Multiple(() =>
            {
                Assert.That(getAllCategoryResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code OK (200)");
                Assert.That(getAllCategoryResponse.Content, Is.Not.Empty, "Response content should not be empty");

            });

            var caregories = JArray.Parse(getAllCategoryResponse.Content);

            Assert.That(caregories, Is.Not.Empty, "Response content should not be empty");

            var categoryId = caregories.First()["_id"]?.ToString();
            Assert.That(categoryId, Is.Not.Empty, "categoryId - Response content should not be empty");

            // Create request for add book
            var createAddBookRequest = new RestRequest("/book", Method.Post);
            createAddBookRequest.AddHeader("Authorization", $"Bearer {token}");

            title = $"Random_{random.Next(99, 999)}";
            var author = "Ivo Petrov";
            var description = "Test description";
            var price = 10;
            var pages = 99;

            createAddBookRequest.AddBody( new
            {
                title,
                author,
                description,
                price,
                pages,
                category = categoryId,

            });

            var createAddBookResponse = client.Execute(createAddBookRequest);

            Assert.Multiple(() =>
            {
                Assert.That(createAddBookResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code OK (200)");
                Assert.That(createAddBookResponse.Content, Is.Not.Empty, "Response content should not be empty");

                var createdBook = JObject.Parse(createAddBookResponse.Content);

                Assert.That(createdBook["title"]?.ToString(), Is.EqualTo(title));
                Assert.That(createdBook["author"]?.ToString(), Is.EqualTo(author));
                Assert.That(createdBook["price"]?.Value<int>(), Is.EqualTo(price));
                Assert.That(createdBook["pages"]?.Value<int>(), Is.EqualTo(pages));
                Assert.That(createdBook["category"], Is.Not.Null);
                Assert.That(createdBook["category"]["_id"]?.ToString(), Is.EqualTo(categoryId));

            });

        }

        [Test, Order(4)]
        public void Test_UpdateBook()
        {
            //Arrange
            var getAllBookRequest = new RestRequest("/book", Method.Get);

            var getAllBookResponse = client.Execute(getAllBookRequest);


            Assert.Multiple(() =>
            {
                Assert.That(getAllBookResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code OK (200)");
                Assert.That(getAllBookResponse.Content, Is.Not.Empty.Or.Null, "Response content should not be empty");


                var books = JArray.Parse(getAllBookResponse.Content);

                Assert.That(books.Count, Is.GreaterThan(0), "The books should be more than one");
                Assert.That(books.Type, Is.EqualTo(JTokenType.Array), " Response content should be array ");

            });

            var allBooks = JArray.Parse(getAllBookResponse.Content);
            Assert.That(allBooks, Is.Not.Empty.Or.Null);

            var bookByTitle = allBooks.FirstOrDefault(b => b["title"]?.ToString() == title);
            var bookIdByTitle = bookByTitle["_id"]?.ToString();

            // Create  update request
            var updatedBookRequest = new RestRequest($"/book/{bookIdByTitle}", Method.Put);
            updatedBookRequest.AddHeader("Authorization", $"Bearer {token}");

            title = $"Updated_Random_{random.Next(99, 999)}";
            var author = "Updated Ivo Petrov";
           

            updatedBookRequest.AddBody(new
            {
                title,
                author,
             
            });

            var updatedBookResponse = client.Execute(updatedBookRequest);


            Assert.Multiple(() =>
            {
                Assert.That(updatedBookResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code OK (200)");
                Assert.That(updatedBookResponse.Content, Is.Not.Empty, "Response content should not be empty");

            });

            var updatedBook = JObject.Parse(updatedBookResponse.Content);

            Assert.Multiple(() => 
            {
                Assert.That(updatedBook["title"]?.ToString(), Is.EqualTo(title));
                Assert.That(updatedBook["author"]?.ToString(), Is.EqualTo(author));

            });


        }
        [Test, Order(5)]
        public void Test_DeleteBook()
        {

            var getAllBookRequest = new RestRequest("/book", Method.Get);

            var getAllBookResponse = client.Execute(getAllBookRequest);


            Assert.Multiple(() =>
            {
                Assert.That(getAllBookResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code OK (200)");
                Assert.That(getAllBookResponse.Content, Is.Not.Empty.Or.Null, "Response content should not be empty");


                var books = JArray.Parse(getAllBookResponse.Content);

                Assert.That(books.Count, Is.GreaterThan(0), "The books should be more than one");
                Assert.That(books.Type, Is.EqualTo(JTokenType.Array), " Response content should be array ");

            });

            var allBooks = JArray.Parse(getAllBookResponse.Content);
            Assert.That(allBooks, Is.Not.Empty.Or.Null);

            var bookByTitle = allBooks.FirstOrDefault(b => b["title"]?.ToString() == title);
            var bookIdByTitle = bookByTitle["_id"]?.ToString();

            //Create delete request

            var deleteBookRequest = new RestRequest($"/book/{bookIdByTitle}", Method.Delete);
            deleteBookRequest.AddHeader("Authorization", $"Bearer {token}");

            var deleteBookResponse = client.Execute(deleteBookRequest);

            Assert.Multiple(() =>
            {
                Assert.That(deleteBookResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code OK (200)");
                Assert.That(deleteBookResponse.Content, Is.Not.Empty, "Response content should not be empty");

            });

            // Check response with delete book Id

            var deleteBookByIdResponse = new RestRequest($"/book/{bookIdByTitle}", Method.Get);
            var deleteResponse = client.Execute(deleteBookByIdResponse);

            Assert.Multiple(() =>
            {
                Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code OK (200)");
                Assert.That(deleteResponse.Content, Is.Not.Empty, "Response content should not be empty");
                Assert.That(deleteResponse.Content, Is.EqualTo("null"));

            });


        }

        public void Dispose()
        {
            client?.Dispose();
        }
    }
}
