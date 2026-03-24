package com.polybridge.sample.payment

import org.json.JSONObject
import kotlin.concurrent.thread

class PaymentService {

    private var initialized = false
    private var eventListener: IPaymentBridge? = null

    fun setEventListener(listener: IPaymentBridge) {
        eventListener = listener
    }

    fun removeEventListener(listener: IPaymentBridge) {
        if (eventListener === listener) eventListener = null
    }

    fun initialize(callback: IPaymentBridge) {
        thread {
            Thread.sleep(500)
            initialized = true
            eventListener?.onPaymentStateChanged("initialized")
            callback.onSuccess("initialized")
        }
    }

    fun purchase(productId: String, amount: Int, callback: IPaymentBridge) {
        if (!initialized) {
            callback.onError("PaymentService not initialized")
            return
        }

        eventListener?.onPaymentStateChanged("processing")

        thread {
            Thread.sleep(1000)

            val result = PaymentResult(
                transactionId = "txn_${System.currentTimeMillis()}",
                productId = productId,
                amount = amount,
                currency = "KRW",
                status = "completed"
            )

            val json = JSONObject().apply {
                put("transactionId", result.transactionId)
                put("productId", result.productId)
                put("amount", result.amount)
                put("currency", result.currency)
                put("status", result.status)
            }

            eventListener?.onPaymentStateChanged("completed")
            eventListener?.onReceiptReady(result.transactionId, json.toString())
            callback.onSuccess(json.toString())
        }
    }
}
