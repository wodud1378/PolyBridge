package com.polybridge.sample.timer

import java.util.Timer
import java.util.TimerTask

class TimerService {

    private var timer: Timer? = null
    private var tickCount = 0
    private var intervalMs: Long = 1000
    private var eventListener: ITimerBridge? = null

    fun setEventListener(listener: ITimerBridge) {
        eventListener = listener
    }

    fun removeEventListener(listener: ITimerBridge) {
        if (eventListener === listener) eventListener = null
    }

    // 동기
    fun isRunning(): Boolean = timer != null

    fun getTickCount(): Int = tickCount

    fun getInterval(): Long = intervalMs

    // 비동기 — 타이머 시작
    fun start(intervalMs: Long, callback: ITimerBridge) {
        if (timer != null) {
            callback.onError("Timer already running")
            return
        }

        this.intervalMs = if (intervalMs > 0) intervalMs else 1000
        tickCount = 0

        timer = Timer().apply {
            scheduleAtFixedRate(object : TimerTask() {
                override fun run() {
                    tickCount++
                    eventListener?.onTick(tickCount.toString())
                }
            }, this@TimerService.intervalMs, this@TimerService.intervalMs)
        }

        eventListener?.onTimerStateChanged("started")
        callback.onSuccess("started")
    }

    // 비동기 — 타이머 정지
    fun stop(callback: ITimerBridge) {
        if (timer == null) {
            callback.onError("Timer not running")
            return
        }

        timer?.cancel()
        timer = null
        eventListener?.onTimerStateChanged("stopped")
        callback.onSuccess("stopped:$tickCount")
    }

    // 비동기 — 리셋
    fun reset(callback: ITimerBridge) {
        timer?.cancel()
        timer = null
        val prevCount = tickCount
        tickCount = 0
        eventListener?.onTimerStateChanged("reset")
        callback.onSuccess("reset:$prevCount")
    }
}
