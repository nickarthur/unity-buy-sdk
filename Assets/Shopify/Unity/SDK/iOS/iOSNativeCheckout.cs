#if UNITY_IOS
namespace Shopify.Unity.SDK.iOS {
    using System.Collections.Generic;
    using System.Collections;
    using System.Runtime.InteropServices;
    using System;
    using Shopify.Unity.MiniJSON;
    using Shopify.Unity.SDK;

    partial class iOSNativeCheckout : INativeCheckout {
        [DllImport("__Internal")]
        private static extern bool _CanCheckoutWithApplePay(string supportedPaymentNetworks);

        [DllImport("__Internal")]
        private static extern bool _CanShowApplePaySetup(string supportedPaymentNetworks);

        [DllImport("__Internal")]
        private static extern void _ShowApplePaySetup();

        [DllImport("__Internal")]
        private static extern bool _CreateApplePaySession(
            string unityDelegateObjectName,
            string merchantID,
            string countryCode,
            string currencyCode,
            string serializedSupportedNetworks,
            string serializedSummaryItems,
            string serializedShippingMethods,
            bool requiringShipping);

        [DllImport("__Internal")]
        private static extern void _PresentApplePayAuthorization();

        private CartState CartState;
        private ApplePayEventReceiverBridge ApplePayEventBridge;

        private CheckoutSuccessCallback OnSuccess;
        private CheckoutCancelCallback OnCancelled;
        private CheckoutFailureCallback OnFailure;

        private string StoreName;

        public iOSNativeCheckout(CartState cartState) {
            CartState = cartState;
        }

        /// <summary>
        /// Checks if the device is capable of paying with Apple Pay and checks if the store's payment provider accepts Apple Pay
        /// </summary>
        /// <param name="paymentSettings">The Shop's payment settings</param>
        /// <param name="callback">The callback that will deliver the result to tha caller</param>
        /// <returns>True if the device is capable of paying with Apple Pay and the store accepts payments with Apple Pay</returns>
        public void CanCheckout(PaymentSettings paymentSettings, CanCheckoutWithNativePayCallback callback) {
            if (paymentSettings.supportedDigitalWallets().Contains(DigitalWallet.APPLE_PAY) &&
                _CanCheckoutWithApplePay(SerializedPaymentNetworksFromCardBrands(paymentSettings.acceptedCardBrands()))) {
                callback(true);
            } else {
                callback(false);
            }
        }

        /// <summary>
        /// Checks if the device is capable of setting up Apple Pay
        /// </summary>
        /// <param name="paymentSettings">The Shop's payment settings</param>
        /// <returns>True if the device is capable of setting up Apple Pay </returns>
        public bool CanShowPaymentSetup(PaymentSettings paymentSettings) {
            return _CanShowApplePaySetup(SerializedPaymentNetworksFromCardBrands(paymentSettings.acceptedCardBrands()));
        }

        /// <summary>
        /// Launches the iOS Wallet App, for the user to sign up with Apple Pay
        /// </summary>
        public void ShowPaymentSetup() {
            _ShowApplePaySetup();
        }

        /// <summary>
        /// Starts the process of making a payment through Apple Pay.
        /// </summary>
        /// <remarks>
        ///  Displays a payment interface to the user based on the contents of the Cart
        /// </remarks>
        /// <param name="key">Merchant ID for Apple Pay from the Apple Developer Portal</param>
        /// <param name="shopMetadata">The shop's metadata containing name and payment settings</param>
        /// <param name="success">Delegate method that will be notified upon a successful payment</param>
        /// <param name="failure">Delegate method that will be notified upon a failure during the checkout process</param>
        /// <param name="cancelled">Delegate method that will be notified upon a cancellation during the checkout process</param>
        public void Checkout(string key, ShopMetadata shopMetadata, CheckoutSuccessCallback success, CheckoutCancelCallback cancelled, CheckoutFailureCallback failure) {
            OnSuccess = success;
            OnCancelled = cancelled;
            OnFailure = failure;
            StoreName = shopMetadata.Name;

#if !(SHOPIFY_TEST)

            var checkout = CartState.CurrentCheckout;

            var supportedNetworksString = SerializedPaymentNetworksFromCardBrands(shopMetadata.PaymentSettings.acceptedCardBrands());

            var summaryItems = GetSummaryItemsFromCheckout(checkout);
            var summaryString = Json.Serialize(summaryItems);

            var currencyCodeString = checkout.currencyCode().ToString("G");
            var countryCodeString = shopMetadata.PaymentSettings.countryCode().ToString("G");

            var requiresShipping = checkout.requiresShipping();

            if (ApplePayEventBridge == null) {
                ApplePayEventBridge = GlobalGameObject.AddComponent<ApplePayEventReceiverBridge>();
                ApplePayEventBridge.Receiver = this;
            }

            if (_CreateApplePaySession(GlobalGameObject.Name, key, countryCodeString, currencyCodeString, supportedNetworksString, summaryString, null, requiresShipping)) {
                _PresentApplePayAuthorization();
            } else {
                var error = new ShopifyError(ShopifyError.ErrorType.NativePaymentProcessingError, "Unable to create an Apple Pay payment request. Please check that your merchant ID is valid.");
                OnFailure(error);
            }
#endif
        }

        private List<SummaryItem> GetSummaryItemsFromCheckout(Checkout checkout) {
            if (StoreName == null) {
                throw new InvalidOperationException("No StoreName has been set before calling GetSummaryItemsFromCheckout(). Please call Checkout() first");
            }

            var summaryItems = new List<SummaryItem>();
            summaryItems.Add(new SummaryItem("SUBTOTAL", checkout.subtotalPrice().ToString()));

            if (checkout.requiresShipping()) {
                try {
                    summaryItems.Add(new SummaryItem("SHIPPING", checkout.shippingLine().price().ToString()));
                } catch { }
            }

            summaryItems.Add(new SummaryItem("TAXES", checkout.totalTax().ToString()));

            // We used the store name here instead of TOTAL due to Apple Pay design guidelines. This will read as `Pay storeName`.
            summaryItems.Add(new SummaryItem(StoreName, checkout.totalPrice().ToString()));

            return summaryItems;
        }

        private List<ShippingMethod> GetShippingMethods() {
            var checkout = CartState.CurrentCheckout;
            var shippingMethods = new List<ShippingMethod>();

            try {
                var availableShippingRates = checkout.availableShippingRates().shippingRates();

                foreach (var shippingRate in availableShippingRates) {
                    shippingMethods.Add(new ShippingMethod(shippingRate.title(), shippingRate.price().ToString(), shippingRate.handle()));
                }
            } catch (Exception e) {
                throw new Exception("Attempted to gather information on available shipping rates on CurrentCheckout, but CurrentCheckout do not have those properties queried", e);
            }

            return shippingMethods;
        }

        private string SerializedPaymentNetworksFromCardBrands(List<CardBrand> cardBrands) {
            var paymentNetworks = PaymentNetwork.NetworksFromCardBrands(cardBrands);
            return Json.Serialize(paymentNetworks);
        }
    }
}
#endif
