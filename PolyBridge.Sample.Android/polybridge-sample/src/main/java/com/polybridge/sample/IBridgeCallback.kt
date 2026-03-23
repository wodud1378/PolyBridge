package com.polybridge.sample

interface IBridgeCallback {
    fun onSuccess(result: String)
    fun onError(error: String)
}
