package com.polybridge.sample.login

data class LoginResult(
    val userId: String,
    val displayName: String,
    val token: String,
    val expiresIn: Long
)
