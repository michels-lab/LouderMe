package com.michelslab.louderme.ui

import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.geometry.Rect
import androidx.compose.ui.geometry.RoundRect
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.Path
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp

enum class SocialNetwork {
    INSTAGRAM,
    FACEBOOK,
    LINKEDIN,
    GITHUB,
    EMAIL,
}

@Composable
fun CanonicalWaveform(
    modifier: Modifier = Modifier,
    color: Color = LouderMeColors.Cyan,
    alpha: Float = 1f,
) {
    Canvas(modifier = modifier) {
        val sx = size.width / 1024f
        val sy = size.height / 1024f
        val path = Path().apply {
            moveTo(120f * sx, 595f * sy)
            cubicTo(205f * sx, 625f * sy, 242f * sx, 700f * sy, 310f * sx, 704f * sy)
            cubicTo(392f * sx, 709f * sy, 394f * sx, 412f * sy, 486f * sx, 405f * sy)
            cubicTo(574f * sx, 397f * sy, 586f * sx, 651f * sy, 660f * sx, 643f * sy)
            cubicTo(735f * sx, 634f * sy, 725f * sx, 329f * sy, 807f * sx, 332f * sy)
            cubicTo(868f * sx, 334f * sy, 889f * sx, 464f * sy, 906f * sx, 525f * sy)
        }
        drawPath(
            path = path,
            color = color.copy(alpha = alpha),
            style = Stroke(
                width = (94f * minOf(sx, sy)).coerceAtLeast(2f),
            ),
        )
        drawCircle(
            color = color.copy(alpha = alpha),
            radius = 47f * minOf(sx, sy),
            center = Offset(881f * sx, 605f * sy),
        )
    }
}

@Composable
fun EqWavePreview(
    gainsDb: List<Float>,
    modifier: Modifier = Modifier,
    color: Color = LouderMeColors.Cyan,
) {
    Canvas(modifier = modifier) {
        if (gainsDb.isEmpty()) return@Canvas

        val centerY = size.height / 2f
        val usable = size.height * 0.34f
        val step = if (gainsDb.size == 1) 0f else size.width / (gainsDb.size - 1)
        val points = gainsDb.mapIndexed { index, gain ->
            Offset(
                x = index * step,
                y = centerY - (gain.coerceIn(-10f, 10f) / 10f) * usable,
            )
        }

        val path = Path().apply {
            moveTo(points.first().x, points.first().y)
            for (i in 1 until points.size) {
                val previous = points[i - 1]
                val current = points[i]
                val midX = (previous.x + current.x) / 2f
                cubicTo(midX, previous.y, midX, current.y, current.x, current.y)
            }
        }

        drawLine(
            color = LouderMeColors.LineStrong,
            start = Offset(0f, centerY),
            end = Offset(size.width, centerY),
            strokeWidth = 1.5f,
        )
        drawPath(path, color, style = Stroke(width = 3f))
        points.forEach {
            drawCircle(color = color, radius = 4.5f, center = it)
            drawCircle(color = LouderMeColors.Bg, radius = 2f, center = it)
        }
    }
}

@Composable
fun SocialGlyph(
    network: SocialNetwork,
    modifier: Modifier = Modifier,
) {
    val shape = RoundedCornerShape(9.dp)
    Box(
        modifier = modifier
            .size(34.dp)
            .clip(shape)
            .background(LouderMeColors.Surface3)
            .border(1.dp, LouderMeColors.LineStrong, shape),
        contentAlignment = Alignment.Center,
    ) {
        when (network) {
            SocialNetwork.INSTAGRAM -> {
                Canvas(Modifier.size(20.dp)) {
                    val stroke = Stroke(width = 2.1f)
                    drawRoundRect(
                        color = Color.White,
                        topLeft = Offset(2f, 2f),
                        size = androidx.compose.ui.geometry.Size(size.width - 4f, size.height - 4f),
                        cornerRadius = androidx.compose.ui.geometry.CornerRadius(5f, 5f),
                        style = stroke,
                    )
                    drawCircle(
                        color = Color.White,
                        radius = size.minDimension * 0.19f,
                        center = center,
                        style = stroke,
                    )
                    drawCircle(
                        color = Color.White,
                        radius = 1.5f,
                        center = Offset(size.width * 0.75f, size.height * 0.25f),
                    )
                }
            }

            SocialNetwork.FACEBOOK -> Text(
                "f",
                color = Color.White,
                fontSize = 22.sp,
                fontWeight = FontWeight.Black,
            )

            SocialNetwork.LINKEDIN -> Text(
                "in",
                color = Color.White,
                fontSize = 14.sp,
                fontWeight = FontWeight.Black,
            )

            SocialNetwork.GITHUB -> {
                Canvas(Modifier.size(21.dp)) {
                    val headRadius = size.minDimension * 0.34f
                    val headCenter = Offset(size.width / 2f, size.height * 0.48f)
                    drawCircle(Color.White, radius = headRadius, center = headCenter)

                    val ears = Path().apply {
                        moveTo(size.width * 0.23f, size.height * 0.31f)
                        lineTo(size.width * 0.27f, size.height * 0.08f)
                        lineTo(size.width * 0.42f, size.height * 0.22f)
                        close()
                        moveTo(size.width * 0.77f, size.height * 0.31f)
                        lineTo(size.width * 0.73f, size.height * 0.08f)
                        lineTo(size.width * 0.58f, size.height * 0.22f)
                        close()
                    }
                    drawPath(ears, Color.White)
                    drawRoundRect(
                        color = Color.White,
                        topLeft = Offset(size.width * 0.37f, size.height * 0.64f),
                        size = androidx.compose.ui.geometry.Size(size.width * 0.26f, size.height * 0.25f),
                        cornerRadius = androidx.compose.ui.geometry.CornerRadius(5f, 5f),
                    )
                }
            }

            SocialNetwork.EMAIL -> {
                Canvas(Modifier.size(21.dp)) {
                    val stroke = Stroke(width = 2f)
                    val rect = Rect(2f, 4f, size.width - 2f, size.height - 4f)
                    drawRoundRect(
                        color = Color.White,
                        topLeft = rect.topLeft,
                        size = rect.size,
                        cornerRadius = androidx.compose.ui.geometry.CornerRadius(3f, 3f),
                        style = stroke,
                    )
                    drawLine(Color.White, rect.topLeft, center, strokeWidth = 2f)
                    drawLine(Color.White, rect.topRight, center, strokeWidth = 2f)
                }
            }
        }
    }
}
