package com.michelslab.louderme.ui

import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Typography
import androidx.compose.material3.darkColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.TextStyle
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.sp

object LouderMeColors {
    val Bg = Color(0xFF060910)
    val Canvas = Color(0xFF090E17)
    val Surface = Color(0xFF0D1521)
    val Surface2 = Color(0xFF111C2B)
    val Surface3 = Color(0xFF162335)
    val Line = Color(0x21A1B8DC)
    val LineStrong = Color(0x4579BDFF)
    val Text = Color(0xFFF3F7FC)
    val Muted = Color(0xFF8D9CB2)
    val Dim = Color(0xFF58677C)
    val Blue = Color(0xFF5D9CFF)
    val Cyan = Color(0xFF71D7FF)
    val Gold = Color(0xFFEFBD62)
    val Green = Color(0xFF63D1A7)
    val Red = Color(0xFFFF7184)
    val Violet = Color(0xFFAA8CFF)
}

private val LouderMeColorScheme = darkColorScheme(
    primary = LouderMeColors.Blue,
    onPrimary = Color.White,
    secondary = LouderMeColors.Cyan,
    onSecondary = Color(0xFF071018),
    tertiary = LouderMeColors.Gold,
    onTertiary = Color(0xFF241A08),
    background = LouderMeColors.Bg,
    onBackground = LouderMeColors.Text,
    surface = LouderMeColors.Surface,
    onSurface = LouderMeColors.Text,
    surfaceVariant = LouderMeColors.Surface2,
    onSurfaceVariant = LouderMeColors.Muted,
    outline = LouderMeColors.LineStrong,
    error = LouderMeColors.Red,
)

private val LouderMeTypography = Typography(
    headlineLarge = TextStyle(
        fontFamily = FontFamily.SansSerif,
        fontSize = 28.sp,
        lineHeight = 31.sp,
        fontWeight = FontWeight.Bold,
        letterSpacing = (-0.7).sp,
    ),
    headlineMedium = TextStyle(
        fontFamily = FontFamily.SansSerif,
        fontSize = 22.sp,
        lineHeight = 25.sp,
        fontWeight = FontWeight.Bold,
        letterSpacing = (-0.45).sp,
    ),
    titleLarge = TextStyle(
        fontFamily = FontFamily.SansSerif,
        fontSize = 19.sp,
        lineHeight = 23.sp,
        fontWeight = FontWeight.Bold,
    ),
    titleMedium = TextStyle(
        fontFamily = FontFamily.SansSerif,
        fontSize = 15.sp,
        lineHeight = 20.sp,
        fontWeight = FontWeight.SemiBold,
    ),
    bodyMedium = TextStyle(
        fontFamily = FontFamily.SansSerif,
        fontSize = 13.sp,
        lineHeight = 19.sp,
    ),
    bodySmall = TextStyle(
        fontFamily = FontFamily.SansSerif,
        fontSize = 11.sp,
        lineHeight = 16.sp,
    ),
    labelLarge = TextStyle(
        fontFamily = FontFamily.SansSerif,
        fontSize = 11.sp,
        fontWeight = FontWeight.Bold,
    ),
    labelMedium = TextStyle(
        fontFamily = FontFamily.SansSerif,
        fontSize = 10.sp,
        fontWeight = FontWeight.Bold,
    ),
    labelSmall = TextStyle(
        fontFamily = FontFamily.SansSerif,
        fontSize = 9.sp,
        fontWeight = FontWeight.Bold,
    ),
)

@Composable
fun LouderMeTheme(content: @Composable () -> Unit) {
    MaterialTheme(
        colorScheme = LouderMeColorScheme,
        typography = LouderMeTypography,
        content = content,
    )
}
