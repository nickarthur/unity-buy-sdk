namespace Shopify.Unity {
    using System.Collections.Generic;
    using System.Text;
    using System;
    using Shopify.Unity.GraphQL;
    using Shopify.Unity.SDK;

#if UNITY_IOS
    using Shopify.Unity.SDK.iOS;
#elif UNITY_ANDROID
    using Shopify.Unity.SDK.Android;
#endif

    /// <summary>
    /// This exception is thrown when interacting with a cart to add, update, or delete line items and no matching
    /// variant could be found.
    /// </summary>
    public class NoMatchingVariantException : Exception {
        public NoMatchingVariantException(string message) : base(message) { }
    }

    /// <summary>
    /// Wraps around a shipping address and shipping identifier. Used for sending the final checkout fields in one GraphQL query.
    /// </summary>
    public struct ShippingFields {
        public MailingAddressInput ShippingAddress;
        public string ShippingIdentifier;

        public ShippingFields(MailingAddressInput shippingAddress, string shippingIdentifier) {
            ShippingIdentifier = shippingIdentifier;
            ShippingAddress = shippingAddress;
        }
    }

    /// <summary>
    /// Data struct containing metadata associated with a Shop.
    /// </summary>
    public struct ShopMetadata {
        public readonly string Name;
        public readonly PaymentSettings PaymentSettings;

        public ShopMetadata(string name, PaymentSettings paymentSettings) {
            Name = name;
            PaymentSettings = paymentSettings;
        }
    }

    /// <summary>
    /// Manages line items for an order. Can also be used to generate a web checkout link to check out in browser.
    /// </summary>
    public class Cart {
        /// <summary>
        /// Current <see ref="CartLineItems">line items </see> for this <see ref="Cart">Cart </see>.
        /// </summary>
        public CartLineItems LineItems {
            get {
                return State.LineItems;
            }
        }

        /// <summary>
        /// A list of all the user errors with the current checkout. For example, when a user enters invalid information
        /// in a field during checkout, the API will return a list of user errors which gets stored here. In general, a
        /// failure callback is invoked and the developer can reference this field to handle these issues.
        /// </summary>
        /// <returns>A list of <see ref="UserError">user errors</see> for this <see ref="Cart">Cart </see></returns>
        public List<UserError> UserErrors {
            get {
                return State.UserErrors;
            }
        }

        private bool IsSaved {
            get {
                return IsCreated && LineItems.IsSaved;
            }
        }

        private bool IsCreated {
            get {
                return CurrentCheckout != null;
            }
        }

        public Checkout CurrentCheckout {
            get {
                return State.CurrentCheckout;
            }
        }

        private WebCheckout WebCheckout;
        private INativeCheckout NativeCheckout;
        private ShopMetadata? ShopMetadata;

        private ShopifyClient Client;

#if (SHOPIFY_TEST)
        public CartState State;
#else
        private CartState State;
#endif

        /// <summary>
        /// Constructs a new cart using a <see ref="ShopifyClient">ShopifyClient </see>. Typically, carts won't be
        /// instantiated directly, but will rather be instatiated using <see ref="ShopifyClient.Cart">ShopifyBuy.Client().Cart() </see>.
        /// </summary>
        /// <param name="client">client associated with this cart</param>
        /// \code
        /// // Example that creates a cart using a ShopifyClient and checks how many line items it has
        /// // (spoiler: it has 0 line items since the cart was just created).
        /// ShopifyClient client = new ShopifyClient(accessToken, shopDomain);
        ///
        /// Cart cart = new Cart(client);
        ///
        /// Debug.Log(cart.LineItems.All().Count);
        /// \endcode
        public Cart(ShopifyClient client) {
            State = new CartState(client);
            Client = client;

#if UNITY_EDITOR || UNITY_STANDALONE
            WebCheckout = new UnityWebCheckout(this, client);
            NativeCheckout = null;
#elif UNITY_IOS
            WebCheckout = new iOSWebCheckout(this, client);
            NativeCheckout = new iOSNativeCheckout(State);
#elif UNITY_ANDROID
            WebCheckout = new AndroidWebCheckout(this, client);
            NativeCheckout = null;
#else
            WebCheckout = null;
            NativeCheckout = null;
#endif
        }

        /// <summary>
        /// Resets the cart by removing all line items and resets all internal state.
        /// </summary>
        public void Reset() {
            State.Reset();
        }

        /// <summary>
        /// Returns the current sub total for the cart
        /// </summary>
        public decimal Subtotal() {
            return State.Subtotal();
        }

        /// <summary>
        /// Returns the web URL for checking out the contents of this Cart. For presenting the user with a web view that
        /// loads this URL see <see cref="Cart.CheckoutWithWebView">CheckoutWithWebView</see>.
        /// </summary>
        /// <param name="success">called when the checkout url was successfully generated.</param>
        /// <param name="failure">called when generating the checkout url failed.</param>
        public void GetWebCheckoutLink(GetWebCheckoutLinkSuccessCallback success, GetWebCheckoutLinkFailureCallback failure) {
            State.CheckoutSave(error => {
                if (error != null) {
                    failure(error);
                    return;
                }

                success(CurrentCheckout.webUrl());
            });
        }

        /// <summary>
        /// Launches a platform-specific web view screen with the Cart's web checkout link loaded. This can be used to perform
        /// a cart checkout from within your application instead of being directed to an external web application. Typically this
        /// can be used as a fallback measure in cases where the user's device doesn't support native pay methods.
        /// </summary>
        /// <param name="success">called when the web checkout screen has been dismissed and the checkout was successful.</param>
        /// <param name="cancelled">called when the web checkout screen was dismissed before completing a checkout.</param>
        /// <param name="failure">called when an error was encountered after the web checkout screen has been dismissed.</param>
        public void CheckoutWithWebView(CheckoutSuccessCallback success, CheckoutCancelCallback cancelled, CheckoutFailureCallback failure) {
            if (WebCheckout == null) {
                throw new PlatformNotSupportedException("Sorry. We haven't implemented web checkout for this platform yet.");
            }

            GetWebCheckoutLink(url => {
                WebCheckout.Checkout(url, success, cancelled, failure);
            }, error => {
                failure(error);
            });
        }

        /// <summary>
        /// Launches a platform-specific native pay UI for checking out the Cart's contents. Currently supported native pay options
        /// include:
        ///     - iOS: Launches Apple Pay.
        /// </summary>
        /// <exception cref="System.PlatformNotSupportedException">Thrown when invoking this method on an unsupported platform.</exception>
        /// <param name="key">Vendor-specific key for identifying the merchant. For iOS devices this is the merchant ID.</param>
        /// <param name="success">called whenever the checkout process has completed successfully.</param>
        /// <param name="cancelled">called whenever the user cancels out of the native pay flow.</param>
        /// <param name="failure">called whenever an error or failure is encountered during native pay.</param>
        public void CheckoutWithNativePay(string key, CheckoutSuccessCallback success, CheckoutCancelCallback cancelled, CheckoutFailureCallback failure) {
            if (NativeCheckout == null) {
                throw new PlatformNotSupportedException("Sorry. We haven't implemented native payments for this platform yet.");
            }

            State.CheckoutSave(error => {
                if (error != null) {
                    failure(error);
                } else {
                    GetShopMetadata((metadata, metadataError) => {
                        if (metadataError != null) {
                            failure(metadataError);
                        } else {
                            NativeCheckout.Checkout(key, metadata.Value, success, cancelled, failure);
                        }
                    });
                }
            });
        }

        /// <summary>
        /// Determine whether the user can be shown to setup their native payment solution
        /// </summary>
        /// <param name="callback">
        /// Closure that is invoked with the result.
        /// True is returned if the user can be shown a native payment app to setup their payment card
        /// </param>
        public void CanShowNativePaySetup(CanShowNativePaySetupCallback callback) {
            if (NativeCheckout != null) {
                GetShopMetadata((metadata, error) => {
                    if (error != null) {
                        callback(false);
                    } else {
                        callback(NativeCheckout.CanShowPaymentSetup(metadata.Value.PaymentSettings));
                    }
                });
            } else {
                callback(false);
            }
        }

        /// <summary>
        /// Launch a native payment app to prompt the user to setup their payment card
        /// </summary>
        /// <exception cref="PlatformNotSupportedException">Thrown when the device does not support this feature</exception>
        public void ShowNativePaySetup() {
            if (NativeCheckout != null) {
                NativeCheckout.ShowPaymentSetup();
            } else {
                throw new PlatformNotSupportedException("Sorry. We haven't implemented native payments for this platform yet.");
            }
        }

        /// <summary>
        /// Determine whether the user can checkout by paying with their native payment solution
        /// </summary>
        /// <param name="callback">
        /// Closure that is invoked with the result.
        /// True is returned if the user is able to pay with their native payment solution
        /// </param>
        public void CanCheckoutWithNativePay(CanCheckoutWithNativePayCallback callback) {
            if (NativeCheckout != null) {
                GetShopMetadata((metadata, error) => {
                    if (error != null) {
                        callback(false);
                    } else {
                        NativeCheckout.CanCheckout(metadata.Value.PaymentSettings, callback);
                    }
                });
            } else {
                callback(false);
            }
        }

        private void GetShopMetadata(ShopMetadataHandler callback) {
            if (ShopMetadata != null) {
                callback(ShopMetadata, null);
            } else {
                var query = new QueryRootQuery();
                DefaultQueries.shop.PaymentSettings(query);
                DefaultQueries.shop.Name(query);

                Client.Query(query, (response, error) => {
                    if (error != null) {
                        callback(null, error);
                    } else {
                        var metadata = new ShopMetadata(response.shop().name(), response.shop().paymentSettings());
                        ShopMetadata = metadata;
                        callback(ShopMetadata, null);
                    }
                });
            }
        }
    }
}
