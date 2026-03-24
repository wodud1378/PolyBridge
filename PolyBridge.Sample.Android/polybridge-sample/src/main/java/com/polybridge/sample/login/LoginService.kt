package com.polybridge.sample.login

import org.json.JSONObject
import kotlin.concurrent.thread

class LoginService {

    private var initialized = false
    private var currentUser: String? = null
    private var eventListener: ILoginBridge? = null

    fun setSessionListener(listener: ILoginBridge) {
        eventListener = listener
    }

    fun removeSessionListener(listener: ILoginBridge) {
        if (eventListener === listener) eventListener = null
    }

    // 동기
    fun isLoggedIn(): Boolean = currentUser != null

    fun getCurrentUser(): String = currentUser ?: ""

    // 비동기 — void
    fun initialize(callback: ILoginBridge) {
        thread {
            Thread.sleep(300)
            initialized = true
            eventListener?.onSessionStateChanged("initialized")
            callback.onSuccess("initialized")
        }
    }

    // 비동기 — 복합 타입
    fun login(username: String, password: String, callback: ILoginBridge) {
        if (!initialized) {
            callback.onError("LoginService not initialized")
            return
        }

        eventListener?.onSessionStateChanged("authenticating")

        thread {
            Thread.sleep(800)

            if (username.isBlank() || password.isBlank()) {
                callback.onError("Username and password are required")
                return@thread
            }

            currentUser = username
            val result = LoginResult(
                userId = "user_${username.hashCode().toUInt()}",
                displayName = username,
                token = "tok_${System.currentTimeMillis()}",
                expiresIn = 3600
            )

            val json = JSONObject().apply {
                put("userId", result.userId)
                put("displayName", result.displayName)
                put("token", result.token)
                put("expiresIn", result.expiresIn)
            }

            eventListener?.onSessionStateChanged("logged_in")
            callback.onSuccess(json.toString())
        }
    }

    // 비동기 — void
    fun logout(callback: ILoginBridge) {
        if (!initialized) {
            callback.onError("LoginService not initialized")
            return
        }

        thread {
            Thread.sleep(200)
            currentUser = null
            eventListener?.onSessionStateChanged("logged_out")
            callback.onSuccess("logged_out")
        }
    }
}
