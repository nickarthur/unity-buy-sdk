//
//  SerializableTests.swift
//  UnityBuySDK
//
//  Created by Shopify.
//  Copyright © 2017 Shopify Inc. All rights reserved.
//
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to deal
//  in the Software without restriction, including without limitation the rights
//  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//  copies of the Software, and to permit persons to whom the Software is
//  furnished to do so, subject to the following conditions:
//
//  The above copyright notice and this permission notice shall be included in
//  all copies or substantial portions of the Software.
//
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//  THE SOFTWARE.
//

import XCTest
import PassKit
@testable import ProductName

class SerializableTests: XCTestCase {
    
    // ----------------------------------
    //  MARK: - PKPayment -
    //
    func testPaymentSerializable() {

        let payment = Models.createPayment()
        
        // Serialize and record data from result
        let paymentDict         = payment.serializedJSON()
        let billingContactDict  = paymentDict[PaymentField.billingContact.rawValue]     as! JSON
        let shippingContactDict = paymentDict[PaymentField.shippingContact.rawValue]    as! JSON
        let tokenDict           = paymentDict[PaymentField.tokenData.rawValue]          as! JSON
        let shippingIdentifier  = paymentDict[PaymentField.shippingIdentifier.rawValue] as! String
        
        let tokenDataDict              = tokenDict[PaymentTokenField.paymentData.rawValue] as Any
        let tokenData                  = try! JSONSerialization.data(withJSONObject: tokenDataDict)
        let tokenTransactionIdentifier = tokenDict[PaymentTokenField.transactionIdentifier.rawValue] as! String
        
        assertEqual(serializedContact: billingContactDict,  to: payment.billingContact!,  havingMultipleAddresses: false)
        assertEqual(serializedContact: shippingContactDict, to: payment.shippingContact!, havingMultipleAddresses: false)
        
        XCTAssertEqual(shippingIdentifier,         payment.shippingMethod?.identifier)
        XCTAssertEqual(tokenData,                  payment.token.paymentData)
        XCTAssertEqual(tokenTransactionIdentifier, payment.token.transactionIdentifier)
    }
    
    // ----------------------------------
    //  MARK: - PKPaymentToken -
    //
    func testPaymentTokenSerializable() {
        
        let token = Models.createPaymentToken()
        
        // Serialize and record data from result
        let tokenDict     = token.serializedJSON()
        let tokenDataDict = tokenDict[PaymentTokenField.paymentData.rawValue] as Any
        let tokenData     = try! JSONSerialization.data(withJSONObject: tokenDataDict)
        let tokenTransactionIdentifier = tokenDict[PaymentTokenField.transactionIdentifier.rawValue] as! String
        
        XCTAssertEqual(tokenData,                  token.paymentData)
        XCTAssertEqual(tokenTransactionIdentifier, token.transactionIdentifier)
    }
    
    // ----------------------------------
    //  MARK: - PKContact -
    //
    func testContactSerializable() {
        let postalAddress = Models.createPostalAddress()
        let contact       = Models.createContact(with: postalAddress)
        assertContactSerializable(contact, havingMultipleAddresses: false)
    }
    
    func testContactWithMultipleAddressesSerializable() {
        let postalAddress = Models.createMultiplePostalAddresses()
        let contact       = Models.createContact(with: postalAddress)
        assertContactSerializable(contact, havingMultipleAddresses: true)
    }
    
    func testContactWithMultitipleAddressesSerializableEdgeCase() {
        let postalAddress = Models.createMultiplePostalAddressesEdgeCase()
        let contact       = Models.createContact(with: postalAddress)
        assertContactSerializable(contact, havingMultipleAddresses: true)
    }
    
    // ----------------------------------
    //  MARK: - Serializable -
    //
    func testSerializedJSONCorrectness() {
        let postalAddress = Models.createPostalAddress()
        let contact       = Models.createContact(with: postalAddress)
        let json          = contact.serializedJSON();
        
        assertEqual(serializedContact: json, to: contact, havingMultipleAddresses: false)
    }
    
    func testSerializedDataCorrectness() {
        let postalAddress = Models.createPostalAddress()
        let contact       = Models.createContact(with: postalAddress)
        let jsonData      = try! contact.serializedData()
        let json          = try! JSONSerialization.jsonObject(with: jsonData) as! JSON
        
        assertEqual(serializedContact: json, to: contact, havingMultipleAddresses: false)
    }
    
    func testSerializedStringCorrectness() {
        let postalAddress = Models.createPostalAddress()
        let contact       = Models.createContact(with: postalAddress)
        let jsonString    = try! contact.serializedString()
        let jsonData      = jsonString.data(using: .utf8)!
        let json          = try! JSONSerialization.jsonObject(with: jsonData) as! JSON
        
        assertEqual(serializedContact: json, to: contact, havingMultipleAddresses: false)
    }
    
    // ----------------------------------
    //  MARK: - Convenience Asserts -
    //
    func assertEqual(serializedContact: JSON, to contact: PKContact, havingMultipleAddresses: Bool) {
        XCTAssertEqual(serializedContact[ContactField.firstName.rawValue] as? String, contact.name?.givenName);
        XCTAssertEqual(serializedContact[ContactField.lastName.rawValue]  as? String, contact.name?.familyName);
        XCTAssertEqual(serializedContact[ContactField.city.rawValue]      as? String, contact.postalAddress?.city);
        XCTAssertEqual(serializedContact[ContactField.country.rawValue]   as? String, contact.postalAddress?.country);
        XCTAssertEqual(serializedContact[ContactField.province.rawValue]  as? String, contact.postalAddress?.state);
        XCTAssertEqual(serializedContact[ContactField.zip.rawValue]       as? String, contact.postalAddress?.postalCode);
        XCTAssertEqual(serializedContact[ContactField.email.rawValue]     as? String, contact.emailAddress);
        XCTAssertEqual(serializedContact[ContactField.phone.rawValue]     as? String, contact.phoneNumber?.stringValue);
        
        let firstAddress  = serializedContact[ContactField.address1.rawValue] as? String
        let secondAddress = serializedContact[ContactField.address2.rawValue] as? String

        if havingMultipleAddresses {
            XCTAssertEqual(firstAddress! + "\n" + secondAddress!, contact.postalAddress!.street);
        } else {
            XCTAssertEqual(firstAddress, contact.postalAddress?.street);
            XCTAssertNil(secondAddress)
        }
    }
    
    func assertContactSerializable(_ contact: PKContact, havingMultipleAddresses: Bool) {
        let contactDict = contact.serializedJSON()
        assertEqual(serializedContact:contactDict, to: contact, havingMultipleAddresses: havingMultipleAddresses)
    }
}