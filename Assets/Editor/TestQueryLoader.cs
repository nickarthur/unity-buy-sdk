namespace Shopify.Tests {
    using NUnit.Framework;
    using System;
    using Shopify.Unity;
    using Shopify.Unity.SDK;
    using Shopify.Unity.GraphQL;

    public class TestQueries {
        public static QueryRootQuery Query = new QueryRootQuery().shop(shop => shop.name());
        public static MutationQuery Mutation = new MutationQuery().customerAccessTokenCreate(
            ap => ap.customerAccessToken(at => at.accessToken()),
            input: new CustomerAccessTokenCreateInput("some@email.com", "12345")
        );
        public static QueryRootQuery QueryFail = new QueryRootQuery().shop(shop => shop.description());
        public static MutationQuery MutationFail = new MutationQuery().customerAccessTokenCreate(
            ap => ap.customerAccessToken(at => at.accessToken()),
            input: new CustomerAccessTokenCreateInput("some@emailThatFails.com", "12345")
        );
    }

    public class TestLoader : BaseLoader {
        public TestLoader() : base("someshop.myshopify.com", "1234") {}

        public override void Load(string query, LoaderResponseHandler callback) {
            if (query == TestQueries.QueryFail.ToString() || query == TestQueries.MutationFail.ToString()) {
                callback(null, "Error: 404 Not Found error");
            } else if (query == TestQueries.Query.ToString()) {
                callback(@"{""data"":{""shop"": {""name"": ""test shop""}}}", null);
            } else if (query == TestQueries.Mutation.ToString()) {
                callback(@"{""data"":{""customerAccessTokenCreate"": {""customerAccessToken"": {""accessToken"":""12345""}}}}", null);
            } else {
                throw new Exception("Invalid test data");
            }
        }

        public override void SetHeader(string key, string value) {
            // No-op
        }

        public override string SDKVariantName() {
            return "test-loader";
        }
    }

    [TestFixture]
    public class TestQueryLoader {
        [Test]
        public void CanQueryHandleLoadErrors() {
            QueryResponse response = null;
            QueryLoader queryLoader = new QueryLoader(new TestLoader());

            queryLoader.Query(TestQueries.QueryFail, (QueryResponse r) => {
                response = r;
            });

            Assert.IsNull(response.data);
            Assert.IsNull(response.errors);
            Assert.AreEqual("Error: 404 Not Found error", response.HTTPError);
        }

        [Test]
        public void CanMutationHandleLoadErrors() {
            MutationResponse response = null;
            QueryLoader queryLoader = new QueryLoader(new TestLoader());

            queryLoader.Mutation(TestQueries.MutationFail, (MutationResponse r) => {
                response = r;
            });

            Assert.IsNull(response.data);
            Assert.IsNull(response.errors);
            Assert.AreEqual("Error: 404 Not Found error", response.HTTPError);
        }

        [Test]
        public void CanQuery() {
            QueryResponse response = null;
            QueryLoader queryLoader = new QueryLoader(new TestLoader());

            queryLoader.Query(TestQueries.Query, (QueryResponse r) => {
                response = r;
            });

            Assert.IsNull(response.errors);
            Assert.IsNull(response.HTTPError);
            Assert.IsNotNull(response.data);
            Assert.AreEqual("test shop", response.data.shop().name());
        }

        [Test]
        public void CanQueryUsingLambda() {
            QueryResponse response = null;
            QueryLoader queryLoader = new QueryLoader(new TestLoader());

            queryLoader.Query(
                q => q.shop(shop => shop.name()),
                (QueryResponse r) => {
                    response = r;
                }
            );

            Assert.IsNull(response.errors);
            Assert.IsNull(response.HTTPError);
            Assert.IsNotNull(response.data);
            Assert.AreEqual("test shop", response.data.shop().name());
        }

        [Test]
        public void CanMutate() {
            MutationResponse response = null;
            QueryLoader queryLoader = new QueryLoader(new TestLoader());

            queryLoader.Mutation(TestQueries.Mutation, (MutationResponse r) => {
                response = r;
            });

            Assert.IsNull(response.errors);
            Assert.IsNull(response.HTTPError);
            Assert.IsNotNull(response.data);
            Assert.AreEqual("12345", response.data.customerAccessTokenCreate().customerAccessToken().accessToken());
        }

        [Test]
        public void CanMutateUsingLambda() {
            QueryResponse response = null;
            QueryLoader queryLoader = new QueryLoader(new TestLoader());

            queryLoader.Query(
                q => q.shop(shop => shop.name()),
                (QueryResponse r) => {
                    response = r;
                }
            );

            Assert.IsNull(response.errors);
            Assert.IsNull(response.HTTPError);
            Assert.IsNotNull(response.data);
            Assert.AreEqual("test shop", response.data.shop().name());
        }
    }
}
