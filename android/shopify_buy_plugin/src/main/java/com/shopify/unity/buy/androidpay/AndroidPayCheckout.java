package com.shopify.unity.buy.androidpay;

import android.content.Intent;
import android.os.Bundle;
import android.support.annotation.NonNull;
import android.support.annotation.Nullable;
import android.support.annotation.VisibleForTesting;

import com.google.android.gms.common.api.GoogleApiClient;
import com.google.android.gms.identity.intents.model.UserAddress;
import com.google.android.gms.wallet.FullWallet;
import com.google.android.gms.wallet.MaskedWallet;
import com.shopify.buy3.pay.PayCart;
import com.shopify.buy3.pay.PayHelper;
import com.shopify.unity.buy.MessageCenter;
import com.shopify.unity.buy.UnityMessage;
import com.shopify.unity.buy.models.AndroidPayEventResponse;
import com.shopify.unity.buy.models.MailingAddressInput;
import com.shopify.unity.buy.models.PricingLineItems;
import com.shopify.unity.buy.models.ShippingMethod;
import com.shopify.unity.buy.utils.Logger;
import com.shopify.unity.buy.utils.WalletErrorFormatter;

import org.json.JSONException;

import java.util.List;

/**
 * @author Flavio Faria
 */

public final class AndroidPayCheckout implements GoogleApiClient.ConnectionCallbacks {

    @VisibleForTesting enum CheckoutState {
        READY,
        REQUESTING_MASKED_WALLET,
        RECEIVED_MASKED_WALLET
    }

    // Android Pay stuff
    @NonNull private final GoogleApiClient googleApiClient;
    @Nullable private String publicKey;
    @Nullable private MaskedWallet maskedWallet;
    @Nullable private FullWallet fullWallet;

    // Unity stuff
    @NonNull private final MessageCenter messageCenter;

    // Checkout stuff
    @NonNull private CheckoutState currentCheckoutState = CheckoutState.READY;
    @Nullable private PayCart cart;
    @Nullable private List<ShippingMethod> shippingMethods;
    @NonNull private final Listener listener;

    public AndroidPayCheckout(@NonNull GoogleApiClientFactory googleApiClientFactory,
                              @NonNull MessageCenter messageCenter,
                              @NonNull Listener listener) {
        googleApiClient = googleApiClientFactory.newGoogleApiClient(this);
        this.messageCenter = messageCenter;
        this.listener = listener;
    }

    public void startCheckout(@NonNull PayCart cart, @NonNull String publicKey) {
        this.cart = cart;
        this.publicKey = publicKey;
        if (!googleApiClient.isConnected()) {
            googleApiClient.connect();
        }
    }

    public void resume() {
        googleApiClient.connect();
    }

    public void suspend() {
        googleApiClient.disconnect();
    }

    @VisibleForTesting
    @NonNull
    CheckoutState getCurrentCheckoutState() {
        return currentCheckoutState;
    }

    @Override
    public void onConnected(@Nullable Bundle bundle) {
        if (currentCheckoutState == CheckoutState.READY) {
            Logger.debug("Google API Client connected");
            currentCheckoutState = CheckoutState.REQUESTING_MASKED_WALLET;
            PayHelper.requestMaskedWallet(googleApiClient, cart, publicKey);
        }
    }

    @Override
    public void onConnectionSuspended(int i) {
        // TODO
    }

    public void handleWalletResponse(int requestCode, int resultCode, Intent data) {
        PayHelper.handleWalletResponse(requestCode, resultCode, data, new PayHelper.WalletResponseHandler() {
            @Override public void onMaskedWallet(final MaskedWallet maskedWallet) {
                super.onMaskedWallet(maskedWallet);
                AndroidPayCheckout.this.onMaskedWallet(maskedWallet);
            }
            @Override public void onFullWallet(FullWallet fullWallet) {
                super.onFullWallet(fullWallet);
                AndroidPayCheckout.this.onFullWallet(fullWallet);
            }
            @Override public void onWalletError(int requestCode, int errorCode) {
                AndroidPayCheckout.this.onWalletError(requestCode, errorCode);
            }
            @Override public void onWalletRequestCancel(int requestCode) {
                super.onWalletRequestCancel(requestCode);
                AndroidPayCheckout.this.onWalletRequestCancel(requestCode);
            }
        });
    }

    private void onMaskedWallet(final MaskedWallet maskedWallet) {
        if (currentCheckoutState == CheckoutState.REQUESTING_MASKED_WALLET) {
            currentCheckoutState = CheckoutState.RECEIVED_MASKED_WALLET;
        }
        listener.onUpdateShippingAddress(cart, shippingMethods);
        updateMaskedWallet(maskedWallet, new MessageCenter.MessageCallback() {
            @Override public void onResponse(String jsonResponse) {
                AndroidPayCheckout.this.onShippingAddressSynchronized(jsonResponse);
            }
        });
    }

    private void onFullWallet(FullWallet fullWallet) {
        updateFullWallet(fullWallet);
    }

    private void onWalletError(int requestCode, int errorCode) {
        Logger.debug("Wallet error: " + WalletErrorFormatter.errorStringFromCode(errorCode));
        final String errorString = WalletErrorFormatter.errorStringFromCode(errorCode);
        final UnityMessage message = UnityMessage.fromAndroid(errorString);
        messageCenter.sendMessageTo(MessageCenter.Method.ON_ERROR, message);
    }

    private void onWalletRequestCancel(int requestCode) {
        Logger.debug("Wallet canceled");
        final UnityMessage message = UnityMessage.fromAndroid("");
        messageCenter.sendMessageTo(MessageCenter.Method.ON_CANCEL, message);
    }

    @Nullable
    public MaskedWallet getMaskedWallet() {
        return maskedWallet;
    }

    private void updateMaskedWallet(@NonNull MaskedWallet maskedWallet,
                                    @Nullable MessageCenter.MessageCallback callback) {
        Logger.debug("Masked wallet received");
        this.maskedWallet = maskedWallet;
        final UserAddress address = maskedWallet.getBuyerShippingAddress();
        final MailingAddressInput input = new MailingAddressInput(address);
        final UnityMessage msg = UnityMessage.fromAndroid(input.toJsonString());
        messageCenter.sendMessageTo(MessageCenter.Method.ON_UPDATE_SHIPPING_ADDRESS, msg, callback);
    }

    @Nullable
    public FullWallet getFullWallet() {
        return fullWallet;
    }

    private void updateFullWallet(@Nullable FullWallet fullWallet) {
        Logger.debug("Full wallet received");
        this.fullWallet = fullWallet;
    }

    /**
     * Unity callback method that runs whenever the shipping address is updated on the Unity side.
     *
     * @param jsonResponse the {@link AndroidPayEventResponse} represented as a JSON string
     */
    private void onShippingAddressSynchronized(String jsonResponse) {
        Logger.debug("New cart data from Unity: " + jsonResponse);
        try {
            final AndroidPayEventResponse response =
                    AndroidPayEventResponse.fromJsonString(jsonResponse);
            shippingMethods = response.shippingMethods;
            cart = payCartFromEventResponse(response);
            listener.onSynchronizeShippingAddress(cart, shippingMethods);
        } catch (JSONException e) {
            e.printStackTrace();
        }
    }

    /**
     * Creates a new {@link PayCart} populated with data from {@link AndroidPayEventResponse}.
     *
     * @param response the {@code AndroidPayEventResponse} with data to populate the cart
     * @return a new {@code PayCart} built based on the {@code response} argument
     */
    @VisibleForTesting PayCart payCartFromEventResponse(AndroidPayEventResponse response) {
        final PricingLineItems items = response.pricingLineItems;
        return PayCart.builder()
                .merchantName(response.merchantName)
                .currencyCode(response.currencyCode)
                .shippingAddressRequired(response.requiresShipping)
                .subtotal(items.subtotal)
                .shippingPrice(items.shippingPrice)
                .taxPrice(items.taxPrice)
                .totalPrice(items.totalPrice)
                .build();
    }

    /**
     * Requests the full wallet to Google Pay API.
     *
     * @param cart the {@link PayCart} to request a full wallet for
     */
    private void requestFullWallet(PayCart cart) {
        Logger.debug("Requesting full wallet...");
        PayHelper.requestFullWallet(googleApiClient, cart, maskedWallet);
    }

    public interface Listener {
        void onUpdateShippingAddress(@NonNull PayCart payCart,
                                     @NonNull List<ShippingMethod> shippingMethods);
        void onSynchronizeShippingAddress(@NonNull PayCart payCart,
                                          @NonNull List<ShippingMethod> shippingMethods);
    }
}