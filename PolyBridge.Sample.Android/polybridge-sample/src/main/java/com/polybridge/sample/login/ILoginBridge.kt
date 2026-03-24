package com.polybridge.sample.login

interface ILoginBridge {
    // Callback
    fun onSuccess(result: String)
    fun onError(error: String)

    // Event
    fun onSessionStateChanged(state: String)
}
