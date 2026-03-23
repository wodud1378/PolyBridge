package com.polybridge.sample.payment

import com.polybridge.sample.IBridgeCallback
import org.json.JSONObject
import kotlin.concurrent.thread

class PaymentService {

    private var initialized = false

    fun initialize(callback: IBridgeCallback) {
        thread {
            Thread.sleep(500) // 초기화 시뮬레이션
            initialized = true
            callback.onSuccess("initialized")
        }
    }

    fun purchase(productId: String, amount: Int, callback: IBridgeCallback) {
        if (!initialized) {
            callback.onError("PaymentService not initialized")
            return
        }

        thread {
            Thread.sleep(1000) // 결제 처리 시뮬레이션

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

            callback.onSuccess(json.toString())
        }
    }
}
