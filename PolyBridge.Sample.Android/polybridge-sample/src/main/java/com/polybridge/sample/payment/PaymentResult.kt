package com.polybridge.sample.payment

data class PaymentResult(
    val transactionId: String,
    val productId: String,
    val amount: Int,
    val currency: String,
    val status: String
)
