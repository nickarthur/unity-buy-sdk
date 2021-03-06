namespace Shopify.Tests
{
    using System.Collections.Generic;
    using NUnit.Framework;
    using Shopify.Unity;
    using Shopify.Unity.SDK;
    using Shopify.Unity.GraphQL;

    [TestFixture]
    public class TestDefaultCheckoutQueries {
        [Test]
        public void TestCheckoutCreate() {
            MutationQuery query = new MutationQuery();
            List<CheckoutLineItemInput> lineItems = new List<CheckoutLineItemInput>();

            DefaultQueries.checkout.Create(query, lineItems);
            Assert.AreEqual(
                "mutation{checkoutCreate (input:{lineItems:[],allowPartialAddresses:true}){checkout {id webUrl currencyCode requiresShipping subtotalPrice totalTax totalPrice ready lineItems (first:250){edges {node {id variant {id }}cursor }pageInfo {hasNextPage }}}userErrors {field message }}}",
                query.ToString()
            );
        }

        [Test]
        public void TestCheckoutPoll() {
            QueryRootQuery query = new QueryRootQuery();
            string checkoutId = "an-id";

            DefaultQueries.checkout.Poll(query, checkoutId);
            Assert.AreEqual(
                "{node (id:\"an-id\"){__typename ...on Checkout{id webUrl currencyCode requiresShipping subtotalPrice totalTax totalPrice ready }}}",
                query.ToString()
            );
        }

        [Test]
        public void TestPaymentPoll() {
            QueryRootQuery query = new QueryRootQuery();
            string paymentId = "an-id";

            DefaultQueries.checkout.PaymentPoll(query, paymentId);
            Assert.AreEqual(
                "{node (id:\"an-id\"){__typename ...on Payment{checkout {id webUrl currencyCode requiresShipping subtotalPrice totalTax totalPrice ready completedAt }errorMessage id ready }}}",
                query.ToString()
            );
        }

        [Test]
        public void TestAvailableShippingRatesPoll() {
            QueryRootQuery query = new QueryRootQuery();
            string checkoutId = "an-id";

            DefaultQueries.checkout.AvailableShippingRatesPoll(query, checkoutId);
            Assert.AreEqual(
                "{node (id:\"an-id\"){__typename ...on Checkout{id webUrl currencyCode requiresShipping subtotalPrice totalTax totalPrice ready availableShippingRates {shippingRates {handle title price }ready }}}}",
                query.ToString()
            );
        }

        [Test]
        public void TestCheckoutLineItemsAdd() {
            MutationQuery query = new MutationQuery();
            string checkoutId = "an-id";
            List<CheckoutLineItemInput> lineItems = new List<CheckoutLineItemInput>();

            DefaultQueries.checkout.LineItemsAdd(query, checkoutId, lineItems);
            Assert.AreEqual(
                "mutation{checkoutLineItemsAdd (lineItems:[],checkoutId:\"an-id\"){checkout {id webUrl currencyCode requiresShipping subtotalPrice totalTax totalPrice ready lineItems (first:250){edges {node {id variant {id }}cursor }pageInfo {hasNextPage }}}userErrors {field message }}}",
                query.ToString()
            );
        }

        [Test]
        public void TestCheckoutLineItemsRemove() {
            MutationQuery query = new MutationQuery();
            string checkoutId = "an-id";
            List<string> lineItemIds = new List<string>();

            DefaultQueries.checkout.LineItemsRemove(query, checkoutId, lineItemIds);
            Assert.AreEqual(
                "mutation{checkoutLineItemsRemove (checkoutId:\"an-id\",lineItemIds:[]){userErrors {field message }}}",
                query.ToString()
            );
        }

        [Test]
        public void TestCheckoutLineItemsPage() {
            QueryRootQuery query = new QueryRootQuery();
            string checkoutId = "an-id";

            DefaultQueries.checkout.CheckoutLineItemsPage(query, checkoutId, 210, "after-something");
            Assert.AreEqual(
                "{node (id:\"an-id\"){__typename ...on Checkout{lineItems (first:210,after:\"after-something\"){edges {node {id variant {id }}cursor }pageInfo {hasNextPage }}}}}",
                query.ToString()
            );
        }

        [Test]
        public void TestShippingAddressUpdate() {
            MutationQuery query = new MutationQuery();
            string checkoutId = "an-id";

            var addressInput = new MailingAddressInput("123 Test Street", "456", "Toronto", "Shopify", "Canada", "First", "Last", "1234567890", "Ontario", "A1B2C3");
            DefaultQueries.checkout.ShippingAddressUpdate(query, checkoutId, addressInput);

            Assert.AreEqual(
                "mutation{checkoutShippingAddressUpdateV2 (shippingAddress:{address1:\"123 Test Street\",address2:\"456\",city:\"Toronto\",company:\"Shopify\",country:\"Canada\",firstName:\"First\",lastName:\"Last\",phone:\"1234567890\",province:\"Ontario\",zip:\"A1B2C3\"},checkoutId:\"an-id\"){checkout {id webUrl currencyCode requiresShipping subtotalPrice totalTax totalPrice ready }userErrors {field message }}}",
                query.ToString()
            );
        }

        [Test]
        public void TestEmailUpdate() {
            MutationQuery query = new MutationQuery();
            string checkoutId = "an-id";

            DefaultQueries.checkout.EmailUpdate(query, checkoutId, "test@shopify.com");
            Assert.AreEqual(
                "mutation{checkoutEmailUpdateV2 (checkoutId:\"an-id\",email:\"test@shopify.com\"){checkout {email }userErrors {field message }}}",
                query.ToString()
            );
        }

        [Test]
        public void TestShippingLineUpdate() {
            MutationQuery query = new MutationQuery();
            string checkoutId = "an-id";

            DefaultQueries.checkout.ShippingLineUpdate(query, checkoutId, "handle");
            Assert.AreEqual(
                "mutation{checkoutShippingLineUpdate (checkoutId:\"an-id\",shippingRateHandle:\"handle\"){checkout {id webUrl currencyCode requiresShipping subtotalPrice totalTax totalPrice ready shippingLine {handle title price }}userErrors {field message }}}",
                query.ToString()
            );
        }

        [Test]
        public void TestCheckoutCompleteWithTokenizedPaymentV2() {
            MutationQuery query = new MutationQuery();
            string checkoutId = "an-id";

            var billingAddress = new MailingAddressInput("123 Test Street", "456", "Toronto", "Shopify", "Canada", "First", "Last", "1234567890", "Ontario", "A1B2C3");
            var paymentAmount = new MoneyInput(new decimal(1), CurrencyCode.CAD);

            var tokenizedPaymentInputV2 = new TokenizedPaymentInputV2(
                paymentAmount: paymentAmount,
                idempotencyKey: "unique_id",
                billingAddress: billingAddress,
                paymentData: "some_utf8_data_string",
                type: "apple_pay"
            );

            DefaultQueries.checkout.CheckoutCompleteWithTokenizedPaymentV2(query, checkoutId, tokenizedPaymentInputV2);
            Assert.AreEqual(
                "mutation{checkoutCompleteWithTokenizedPaymentV2 (checkoutId:\"an-id\",payment:{paymentAmount:{amount:1,currencyCode:CAD},idempotencyKey:\"unique_id\",billingAddress:{address1:\"123 Test Street\",address2:\"456\",city:\"Toronto\",company:\"Shopify\",country:\"Canada\",firstName:\"First\",lastName:\"Last\",phone:\"1234567890\",province:\"Ontario\",zip:\"A1B2C3\"},type:\"apple_pay\",paymentData:\"some_utf8_data_string\"}){checkout {id webUrl currencyCode requiresShipping subtotalPrice totalTax totalPrice ready }payment {checkout {id webUrl currencyCode requiresShipping subtotalPrice totalTax totalPrice ready completedAt }errorMessage id ready }userErrors {field message }}}",
                query.ToString()
            );
        }
    }
}
