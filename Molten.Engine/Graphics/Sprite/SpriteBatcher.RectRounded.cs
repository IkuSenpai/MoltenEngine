﻿namespace Molten.Graphics
{
    public partial class SpriteBatcher
    {
        public void DrawRoundedRect(RectangleF dest, Color fillColor, float rotation,
            Vector2F origin, float cornerRadius, HlslShader shader = null, uint surfaceSlice = 0)
        {
            RoundedRectStyle style = new RoundedRectStyle()
            {
                FillColor = fillColor,
                BorderColor = fillColor,
                CornerRadius = new CornerInfo(cornerRadius),
                BorderThickness = 0
            };

            DrawRoundedRect(dest, rotation, origin, ref style, shader);
        }

        public void DrawRoundedRect(RectangleF dest, Color fillColor, float rotation,
            Vector2F origin, CornerInfo cornerRadius, HlslShader shader = null, uint surfaceSlice = 0)
        {
            RoundedRectStyle style = new RoundedRectStyle()
            {
                FillColor = fillColor,
                BorderColor = fillColor,
                CornerRadius = cornerRadius,
                BorderThickness = 0
            };

            DrawRoundedRect(dest, rotation, origin, ref style, shader);
        }

        public void DrawRoundedRect(RectangleF dest, Color fillColor, Color borderColor, float rotation, 
            Vector2F origin, float cornerRadius, float borderThickness = 0, HlslShader shader = null, uint surfaceSlice = 0)
        {
            RoundedRectStyle style = new RoundedRectStyle()
            {
                FillColor = fillColor,
                BorderColor = borderColor,
                CornerRadius = new CornerInfo(cornerRadius),
                BorderThickness = borderThickness
            };

            DrawRoundedRect(dest, rotation, origin, ref style, shader);
        }

        public void DrawRoundedRect(RectangleF dest, Color fillColor, Color borderColor, float rotation, 
            Vector2F origin, CornerInfo cornerRadius, float borderThickness = 0, HlslShader shader = null, uint surfaceSlice = 0)
        {
            RoundedRectStyle style = new RoundedRectStyle()
            {
                FillColor = fillColor,
                BorderColor = borderColor,
                CornerRadius = cornerRadius,
                BorderThickness = borderThickness
            };

            DrawRoundedRect(dest, rotation, origin, ref style, shader);
        }

        public void DrawRoundedRect(RectangleF dest, float rotation, Vector2F origin, ref RoundedRectStyle style, HlslShader shader = null, uint surfaceSlice = 0)
        {
            ref CornerInfo corners = ref style.CornerRadius;

            if (!corners.HasRounded())
            {
                RectStyle rectStyle = style.ToRectStyle();
                DrawRect(dest, rotation, origin, ref rectStyle, shader, surfaceSlice);
                return;
            }

            // TODO add support for rotation and origin

            Vector2F tl = dest.TopLeft + corners.TopLeft;
            Vector2F tr = dest.TopRight + new Vector2F(-corners.TopRight, corners.TopRight);
            Vector2F br = dest.BottomRight - corners.BottomRight;
            Vector2F bl = dest.BottomLeft + new Vector2F(corners.BottomLeft, -corners.BottomLeft);

            float topWidth = dest.Width - corners.TopLeft - corners.TopRight;
            float bottomWidth = dest.Width - corners.BottomLeft - corners.BottomRight;
            float leftHeight = dest.Height - corners.TopLeft - corners.BottomLeft;
            float rightHeight = dest.Height - corners.TopRight - corners.BottomRight;

            Ellipse ctl = new Ellipse(tl, corners.TopLeft, 0, MathHelper.Constants<float>.PiHalf);
            Ellipse ctr = new Ellipse(tr, corners.TopRight, MathHelper.Constants<float>.PiHalf, float.Pi);
            Ellipse cbr = new Ellipse(br, corners.BottomRight, float.Pi, MathHelper.Constants<float>.PiHalf * 3f);
            Ellipse cbl = new Ellipse(bl, corners.BottomLeft, MathHelper.Constants<float>.PiHalf * 3f, float.Tau);

            EllipseStyle cornerStyle = new EllipseStyle(style.FillColor, style.BorderColor, style.BorderThickness);
            if (corners.TopLeft > 0)
                DrawEllipse(ref ctl, ref cornerStyle, surfaceSlice);

            if (corners.TopRight > 0)
                DrawEllipse(ref ctr, ref cornerStyle, surfaceSlice);

            if (corners.BottomRight > 0)
                DrawEllipse(ref cbr, ref cornerStyle, surfaceSlice);

            if (corners.BottomLeft > 0)
                DrawEllipse(ref cbl, ref cornerStyle, surfaceSlice);

            RectStyle innerStyle = style.ToRectStyle();
            innerStyle.BorderThickness.Zero();

            float leftEdgeWidth = 0;
            float rightEdgeWidth = 0;

            // Draw left edge
            if (style.FillColor.A > 0)
            {
                if (corners.LeftHasRadius())
                {
                    if (corners.LeftSameRadius())
                    {
                        DrawRect(new RectangleF(dest.X, tl.Y, corners.TopLeft, dest.Height - (corners.TopLeft * 2)), ref innerStyle, surfaceSlice);
                        leftEdgeWidth = corners.TopLeft;
                    }
                    else
                    {
                        if (corners.TopLeft < corners.BottomLeft)
                        {
                            leftEdgeWidth = corners.BottomLeft;
                            float dif = corners.BottomLeft - corners.TopLeft;
                            float leftHeight2 = leftHeight + corners.TopLeft;
                            DrawRect(new RectangleF(dest.X, tl.Y, corners.TopLeft, leftHeight), ref innerStyle, 0, shader, surfaceSlice);
                            DrawRect(new RectangleF(dest.X + corners.TopLeft, dest.Y, dif, leftHeight2), ref innerStyle, 0, shader, surfaceSlice);
                        }
                        else
                        {
                            leftEdgeWidth = corners.TopLeft;
                            float dif = corners.TopLeft - corners.BottomLeft;
                            float leftHeight2 = leftHeight + corners.BottomLeft;
                            DrawRect(new RectangleF(dest.X, tl.Y, corners.BottomLeft, leftHeight), ref innerStyle, 0, shader, surfaceSlice);
                            DrawRect(new RectangleF(dest.X + corners.BottomLeft, dest.Y + corners.TopLeft, dif, leftHeight2), ref innerStyle, 0, shader, surfaceSlice);
                        }
                    }
                }

                // Draw right edge
                if (corners.RightHasRadius())
                {
                    if (corners.RightSameRadius())
                    {
                        DrawRect(new RectangleF(dest.Right - corners.TopRight, tr.Y, corners.TopRight, dest.Height - (corners.TopRight * 2)), ref innerStyle, surfaceSlice);
                        rightEdgeWidth = corners.TopRight;
                    }
                    else
                    {

                        if (corners.TopRight < corners.BottomRight)
                        {
                            rightEdgeWidth = corners.BottomRight;
                            float dif = corners.BottomRight - corners.TopRight;
                            float rightHeight2 = rightHeight + corners.TopRight;
                            DrawRect(new RectangleF(dest.Right - corners.TopRight, tl.Y, corners.TopRight, rightHeight), ref innerStyle, 0, shader, surfaceSlice);
                            DrawRect(new RectangleF(dest.Right - corners.BottomRight, dest.Y, dif, rightHeight2), ref innerStyle, 0, shader, surfaceSlice);
                        }
                        else
                        {
                            rightEdgeWidth = corners.TopRight;
                            float dif = corners.TopRight - corners.BottomRight;
                            float rightHeight2 = rightHeight + corners.BottomRight;
                            DrawRect(new RectangleF(dest.Right - corners.BottomRight, tr.Y, corners.BottomRight, rightHeight), ref innerStyle, 0, shader, surfaceSlice);
                            DrawRect(new RectangleF(dest.Right - corners.TopRight, dest.Y + corners.TopRight, dif, rightHeight2), ref innerStyle, 0, shader, surfaceSlice);
                        }
                    }
                }

                // Draw center
                RectangleF c = new RectangleF(dest.X + leftEdgeWidth, dest.Y, dest.Width - leftEdgeWidth - rightEdgeWidth, dest.Height);
                DrawRect(c, ref innerStyle, 0, shader, surfaceSlice);
            }

            if (style.BorderThickness > 0 && style.BorderColor.A > 0)
            {
                float lo = 0.5f * style.BorderThickness; // Line offset
                Vector2F l = new Vector2F(dest.Left + lo, dest.Top + corners.TopLeft);
                Vector2F r = new Vector2F(dest.Right - lo, dest.Top + corners.TopRight);
                Vector2F t = new Vector2F(dest.Left + corners.TopLeft, dest.Top + lo);
                Vector2F b = new Vector2F(dest.Left + corners.BottomLeft, dest.Bottom - lo);

                LineStyle lineStyle = new LineStyle(style.BorderColor, style.BorderThickness);
                DrawLine(l, l + new Vector2F(0, leftHeight), ref lineStyle, surfaceSlice); // Left
                DrawLine(r, r + new Vector2F(0, rightHeight), ref lineStyle, surfaceSlice); // Right
                DrawLine(t, t + new Vector2F(topWidth, 0), ref lineStyle, surfaceSlice); // Top
                DrawLine(b, b + new Vector2F(bottomWidth, 0), ref lineStyle, surfaceSlice); // Bottom
            }
        }
    }
}
