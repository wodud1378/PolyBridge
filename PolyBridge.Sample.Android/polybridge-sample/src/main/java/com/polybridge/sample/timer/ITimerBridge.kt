package com.polybridge.sample.timer

interface ITimerBridge {
    // Callback
    fun onSuccess(result: String)
    fun onError(error: String)

    // Event
    fun onTick(count: String)
    fun onTimerStateChanged(state: String)
}
