package com.polybridge.sample.login

import com.polybridge.sample.IBridgeCallback
import org.json.JSONObject
import kotlin.concurrent.thread

class LoginService {

    private var initialized = false

    fun initialize(callback: IBridgeCallback) {
        thread {
            Thread.sleep(300) // 초기화 시뮬레이션
            initialized = true
            callback.onSuccess("initialized")
        }
    }

    fun login(username: String, password: String, callback: IBridgeCallback) {
        if (!initialized) {
            callback.onError("LoginService not initialized")
            return
        }

        thread {
            Thread.sleep(800) // 로그인 처리 시뮬레이션

            if (username.isBlank() || password.isBlank()) {
                callback.onError("Username and password are required")
                return@thread
            }

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

            callback.onSuccess(json.toString())
        }
    }
}
