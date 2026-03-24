package com.polybridge.sample.payment

interface IPaymentBridge {
    // Callback
    fun onSuccess(result: String)
    fun onError(error: String)

    // Event
    fun onPaymentStateChanged(state: String)
    fun onReceiptReady(transactionId: String, receipt: String)
}
