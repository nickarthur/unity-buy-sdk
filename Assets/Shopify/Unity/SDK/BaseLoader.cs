namespace Shopify.Unity.SDK {
    /// <summary>
    /// An abstract base that must be defined by classes that will perform network communication.
    /// </summary>
    public abstract class BaseLoader {
        /// <summary>
        /// Domain of a Shopify store.
        /// </summary>
        public string Domain {
            get {
                return _Domain;
            }
        }

        /// <summary>
        /// Access token used to communicate with a Shopify store.
        /// </summary>
        public string AccessToken {
            get {
                return _AccessToken;
            }
        }

        private string _AccessToken;
        private string _Domain;

        public BaseLoader(string domain, string accessToken) {
            _AccessToken = accessToken;
            _Domain = domain;

            SetHeader("Content-Type", "application/graphql");
            SetHeader("X-SDK-Version", VersionInformation.VERSION);
            SetHeader("X-Shopify-Storefront-Access-Token", accessToken);
            SetHeader("X-SDK-Variant", SDKVariantName());
        }

        /// <summary>
        /// Sends the GraphQL query to the GraphQL endpoint.
        /// </summary>
        /// <param name="query">a GraphQL query</param>
        /// <param name="callback">a callback which will receive a response from the GraphQL query</param>
        public abstract void Load(string query, LoaderResponseHandler callback);

        /// <summary>
        /// Assigns a custom header field to the loader.
        /// </summary>
        /// <param name="key">Header field name</param>
        /// <param name="value">Header field value</param>
        public abstract void SetHeader(string key, string value);

        /// <summary>
        /// Returns the loader's X-SDK-Variant header value. Required by a BaseLoader implementation to
        /// let the server know who is sending the request. 
        /// </summary>
        /// <returns>Identifying name of the loader to be sent to the server using the X-SDK-Variant header.</returns>
        public abstract string SDKVariantName();
    }
}