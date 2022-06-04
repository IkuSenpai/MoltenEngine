﻿// Copyright (c) 2010-2014 SharpDX - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// -----------------------------------------------------------------------------
// Original code from SlimMath project. http://code.google.com/p/slimmath/
// Greetings to SlimDX Group. Original code published with the following license:
// -----------------------------------------------------------------------------
/*
* Copyright (c) 2007-2011 SlimDX Group
* 
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
* 
* The above copyright notice and this permission notice shall be included in
* all copies or substantial portions of the Software.
* 
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
* THE SOFTWARE.
*/

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace Molten
{
    /// <summary>
    /// Represents a three-component color with double-precision floating-point components.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    [Serializable]
    public struct Color3D : IEquatable<Color3D>, IFormattable
    {
        private const string toStringFormat = "Red:{0} Green:{1} Blue:{2}";

        /// <summary>
        /// The Black color (0, 0, 0).
        /// </summary>
        public static readonly Color3D Black = new Color3D(0.0f, 0.0f, 0.0f);

        /// <summary>
        /// The White color (1, 1, 1, 1).
        /// </summary>
        public static readonly Color3D White = new Color3D(1.0f, 1.0f, 1.0f);

        /// <summary>
        /// The red component of the color.
        /// </summary>
        [DataMember]
        public double R;

        /// <summary>
        /// The green component of the color.
        /// </summary>
        [DataMember]
        public double G;

        /// <summary>
        /// The blue component of the color.
        /// </summary>
        [DataMember]
        public double B;

        /// <summary>
        /// Initializes a new instance of the <see cref="Color3D"/> struct.
        /// </summary>
        /// <param name="value">The value that will be assigned to all components.</param>
        public Color3D(double value)
        {
            R = G = B = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Color3D"/> struct.
        /// </summary>
        /// <param name="red">The red component of the color.</param>
        /// <param name="green">The green component of the color.</param>
        /// <param name="blue">The blue component of the color.</param>
        public Color3D(double red, double green, double blue)
        {
            R = red;
            G = green;
            B = blue;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Color3D"/> struct.
        /// </summary>
        /// <param name="value">The red, green, and blue components of the color.</param>
        public Color3D(Vector3D value)
        {
            R = value.X;
            G = value.Y;
            B = value.Z;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Color3D"/> struct.
        /// </summary>
        /// <param name="rgb">A packed integer containing all three color components in RGB order.
        /// The alpha component is ignored.</param>
        public Color3D(int rgb)
        {
            B = ((rgb >> 16) & 255) / 255.0f;
            G = ((rgb >> 8) & 255) / 255.0f;
            R = (rgb & 255) / 255.0f;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Color3D"/> struct.
        /// </summary>
        /// <param name="values">The values to assign to the red, green, and blue components of the color. This must be an array with three elements.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="values"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="values"/> contains more or less than four elements.</exception>
        public Color3D(double[] values)
        {
            if (values == null)
                throw new ArgumentNullException("values");
            if (values.Length != 3)
                throw new ArgumentOutOfRangeException("values", "There must be three and only three input values for Color3D.");

            R = values[0];
            G = values[1];
            B = values[2];
        }

        /// <summary>
        /// Gets or sets the component at the specified index.
        /// </summary>
        /// <value>The value of the red, green, or blue component, depending on the index.</value>
        /// <param name="index">The index of the component to access. Use 0 for the red component, 1 for the green component, and 2 for the blue component.</param>
        /// <returns>The value of the component at the specified index.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the <paramref name="index"/> is out of the range [0, 2].</exception>
        public double this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return R;
                    case 1: return G;
                    case 2: return B;
                }

                throw new ArgumentOutOfRangeException("index", "Indices for Color3D run from 0 to 2, inclusive.");
            }

            set
            {
                switch (index)
                {
                    case 0: R = value; break;
                    case 1: G = value; break;
                    case 2: B = value; break;
                    default: throw new ArgumentOutOfRangeException("index", "Indices for Color3D run from 0 to 2, inclusive.");
                }
            }
        }

        /// <summary>
        /// Converts the color into a packed integer.
        /// </summary>
        /// <returns>A packed integer containing all three color components.
        /// The alpha channel is set to 255.</returns>
        public int ToRgba()
        {
            uint a = 255;
            uint r = (uint) (R * 255.0f) & 255;
            uint g = (uint) (G * 255.0f) & 255;
            uint b = (uint) (B * 255.0f) & 255;

            uint value = r;
            value |= g << 8;
            value |= b << 16;
            value |= a << 24;

            return (int)value;
        }

        /// <summary>
        /// Converts the color into a packed integer.
        /// </summary>
        /// <returns>A packed integer containing all three color components.
        /// The alpha channel is set to 255.</returns>
        public int ToBgra()
        {
            uint a = 255;
            uint r = (uint)(R * 255.0f) & 255;
            uint g = (uint)(G * 255.0f) & 255;
            uint b = (uint)(B * 255.0f) & 255;

            uint value = b;
            value |= g << 8;
            value |= r << 16;
            value |= a << 24;

            return (int)value;
        }

        /// <summary>
        /// Converts the color into a three component vector.
        /// </summary>
        /// <returns>A three component vector containing the red, green, and blue components of the color.</returns>
        public Vector3D ToVector3()
        {
            return new Vector3D(R, G, B);
        }

        /// <summary>
        /// Creates an array containing the elements of the color.
        /// </summary>
        /// <returns>A three-element array containing the components of the color.</returns>
        public double[] ToArray()
        {
            return new double[] { R, G, B };
        }

        /// <summary>
        /// Adds two colors.
        /// </summary>
        /// <param name="left">The first color to add.</param>
        /// <param name="right">The second color to add.</param>
        /// <param name="result">When the method completes, completes the sum of the two colors.</param>
        public static void Add(ref Color3D left, ref Color3D right, out Color3D result)
        {
            result.R = left.R + right.R;
            result.G = left.G + right.G;
            result.B = left.B + right.B;
        }

        /// <summary>
        /// Adds two colors.
        /// </summary>
        /// <param name="left">The first color to add.</param>
        /// <param name="right">The second color to add.</param>
        /// <returns>The sum of the two colors.</returns>
        public static Color3D Add(Color3D left, Color3D right)
        {
            return new Color3D(left.R + right.R, left.G + right.G, left.B + right.B);
        }

        /// <summary>
        /// Subtracts two colors.
        /// </summary>
        /// <param name="left">The first color to subtract.</param>
        /// <param name="right">The second color to subtract.</param>
        /// <param name="result">WHen the method completes, contains the difference of the two colors.</param>
        public static void Subtract(ref Color3D left, ref Color3D right, out Color3D result)
        {
            result.R = left.R - right.R;
            result.G = left.G - right.G;
            result.B = left.B - right.B;
        }

        /// <summary>
        /// Subtracts two colors.
        /// </summary>
        /// <param name="left">The first color to subtract.</param>
        /// <param name="right">The second color to subtract</param>
        /// <returns>The difference of the two colors.</returns>
        public static Color3D Subtract(Color3D left, Color3D right)
        {
            return new Color3D(left.R - right.R, left.G - right.G, left.B - right.B);
        }

        /// <summary>
        /// Modulates two colors.
        /// </summary>
        /// <param name="left">The first color to modulate.</param>
        /// <param name="right">The second color to modulate.</param>
        /// <param name="result">When the method completes, contains the modulated color.</param>
        public static void Modulate(ref Color3D left, ref Color3D right, out Color3D result)
        {
            result.R = left.R * right.R;
            result.G = left.G * right.G;
            result.B = left.B * right.B;
        }

        /// <summary>
        /// Modulates two colors.
        /// </summary>
        /// <param name="left">The first color to modulate.</param>
        /// <param name="right">The second color to modulate.</param>
        /// <returns>The modulated color.</returns>
        public static Color3D Modulate(Color3D left, Color3D right)
        {
            return new Color3D(left.R * right.R, left.G * right.G, left.B * right.B);
        }

        /// <summary>
        /// Scales a color.
        /// </summary>
        /// <param name="value">The color to scale.</param>
        /// <param name="scale">The amount by which to scale.</param>
        /// <param name="result">When the method completes, contains the scaled color.</param>
        public static void Scale(ref Color3D value, double scale, out Color3D result)
        {
            result.R = value.R * scale;
            result.G = value.G * scale;
            result.B = value.B * scale;
        }

        /// <summary>
        /// Scales a color.
        /// </summary>
        /// <param name="value">The color to scale.</param>
        /// <param name="scale">The amount by which to scale.</param>
        /// <returns>The scaled color.</returns>
        public static Color3D Scale(Color3D value, double scale)
        {
            return new Color3D(value.R * scale, value.G * scale, value.B * scale);
        }

        /// <summary>
        /// Negates a color.
        /// </summary>
        /// <param name="value">The color to negate.</param>
        /// <param name="result">When the method completes, contains the negated color.</param>
        public static void Negate(ref Color3D value, out Color3D result)
        {
            result.R = 1.0f - value.R;
            result.G = 1.0f - value.G;
            result.B = 1.0f - value.B;
        }

        /// <summary>
        /// Negates a color.
        /// </summary>
        /// <param name="value">The color to negate.</param>
        /// <returns>The negated color.</returns>
        public static Color3D Negate(Color3D value)
        {
            return new Color3D(1.0f - value.R, 1.0f - value.G, 1.0f - value.B);
        }

        /// <summary>
        /// Restricts a value to be within a specified range.
        /// </summary>
        /// <param name="value">The value to clamp.</param>
        /// <param name="min">The minimum value.</param>
        /// <param name="max">The maximum value.</param>
        /// <param name="result">When the method completes, contains the clamped value.</param>
        public static void Clamp(ref Color3D value, ref Color3D min, ref Color3D max, out Color3D result)
        {
            double red = value.R;
            red = (red > max.R) ? max.R : red;
            red = (red < min.R) ? min.R : red;

            double green = value.G;
            green = (green > max.G) ? max.G : green;
            green = (green < min.G) ? min.G : green;

            double blue = value.B;
            blue = (blue > max.B) ? max.B : blue;
            blue = (blue < min.B) ? min.B : blue;

            result = new Color3D(red, green, blue);
        }

        /// <summary>
        /// Restricts a value to be within a specified range.
        /// </summary>
        /// <param name="value">The value to clamp.</param>
        /// <param name="min">The minimum value.</param>
        /// <param name="max">The maximum value.</param>
        /// <returns>The clamped value.</returns>
        public static Color3D Clamp(Color3D value, Color3D min, Color3D max)
        {
            Color3D result;
            Clamp(ref value, ref min, ref max, out result);
            return result;
        }

        /// <summary>
        /// Performs a linear interpolation between two colors.
        /// </summary>
        /// <param name="start">Start color.</param>
        /// <param name="end">End color.</param>
        /// <param name="amount">Value between 0 and 1 indicating the weight of <paramref name="end"/>.</param>
        /// <param name="result">When the method completes, contains the linear interpolation of the two colors.</param>
        /// <remarks>
        /// Passing <paramref name="amount"/> a value of 0 will cause <paramref name="start"/> to be returned; a value of 1 will cause <paramref name="end"/> to be returned. 
        /// </remarks>
        public static void Lerp(ref Color3D start, ref Color3D end, double amount, out Color3D result)
        {
            result.R = MathHelperDP.Lerp(start.R, end.R, amount);
            result.G = MathHelperDP.Lerp(start.G, end.G, amount);
            result.B = MathHelperDP.Lerp(start.B, end.B, amount);
        }

        /// <summary>
        /// Performs a linear interpolation between two colors.
        /// </summary>
        /// <param name="start">Start color.</param>
        /// <param name="end">End color.</param>
        /// <param name="amount">Value between 0 and 1 indicating the weight of <paramref name="end"/>.</param>
        /// <returns>The linear interpolation of the two colors.</returns>
        /// <remarks>
        /// Passing <paramref name="amount"/> a value of 0 will cause <paramref name="start"/> to be returned; a value of 1 will cause <paramref name="end"/> to be returned. 
        /// </remarks>
        public static Color3D Lerp(Color3D start, Color3D end, double amount)
        {
            Color3D result;
            Lerp(ref start, ref end, amount, out result);
            return result;
        }

        /// <summary>
        /// Performs a cubic interpolation between two colors.
        /// </summary>
        /// <param name="start">Start color.</param>
        /// <param name="end">End color.</param>
        /// <param name="amount">Value between 0 and 1 indicating the weight of <paramref name="end"/>.</param>
        /// <param name="result">When the method completes, contains the cubic interpolation of the two colors.</param>
        public static void SmoothStep(ref Color3D start, ref Color3D end, double amount, out Color3D result)
        {
            amount = MathHelperDP.SmoothStep(amount);
            Lerp(ref start, ref end, amount, out result);
        }

        /// <summary>
        /// Performs a cubic interpolation between two colors.
        /// </summary>
        /// <param name="start">Start color.</param>
        /// <param name="end">End color.</param>
        /// <param name="amount">Value between 0 and 1 indicating the weight of <paramref name="end"/>.</param>
        /// <returns>The cubic interpolation of the two colors.</returns>
        public static Color3D SmoothStep(Color3D start, Color3D end, double amount)
        {
            Color3D result;
            SmoothStep(ref start, ref end, amount, out result);
            return result;
        }

        /// <summary>
        /// Returns a color containing the smallest components of the specified colors.
        /// </summary>
        /// <param name="left">The first source color.</param>
        /// <param name="right">The second source color.</param>
        /// <param name="result">When the method completes, contains an new color composed of the largest components of the source colors.</param>
        public static void Max(ref Color3D left, ref Color3D right, out Color3D result)
        {
            result.R = (left.R > right.R) ? left.R : right.R;
            result.G = (left.G > right.G) ? left.G : right.G;
            result.B = (left.B > right.B) ? left.B : right.B;
        }

        /// <summary>
        /// Returns a color containing the largest components of the specified colors.
        /// </summary>
        /// <param name="left">The first source color.</param>
        /// <param name="right">The second source color.</param>
        /// <returns>A color containing the largest components of the source colors.</returns>
        public static Color3D Max(Color3D left, Color3D right)
        {
            Color3D result;
            Max(ref left, ref right, out result);
            return result;
        }

        /// <summary>
        /// Returns a color containing the smallest components of the specified colors.
        /// </summary>
        /// <param name="left">The first source color.</param>
        /// <param name="right">The second source color.</param>
        /// <param name="result">When the method completes, contains an new color composed of the smallest components of the source colors.</param>
        public static void Min(ref Color3D left, ref Color3D right, out Color3D result)
        {
            result.R = (left.R < right.R) ? left.R : right.R;
            result.G = (left.G < right.G) ? left.G : right.G;
            result.B = (left.B < right.B) ? left.B : right.B;
        }

        /// <summary>
        /// Returns a color containing the smallest components of the specified colors.
        /// </summary>
        /// <param name="left">The first source color.</param>
        /// <param name="right">The second source color.</param>
        /// <returns>A color containing the smallest components of the source colors.</returns>
        public static Color3D Min(Color3D left, Color3D right)
        {
            Color3D result;
            Min(ref left, ref right, out result);
            return result;
        }

        /// <summary>
        /// Adjusts the contrast of a color.
        /// </summary>
        /// <param name="value">The color whose contrast is to be adjusted.</param>
        /// <param name="contrast">The amount by which to adjust the contrast.</param>
        /// <param name="result">When the method completes, contains the adjusted color.</param>
        public static void AdjustContrast(ref Color3D value, double contrast, out Color3D result)
        {
            result.R = 0.5f + contrast * (value.R - 0.5f);
            result.G = 0.5f + contrast * (value.G - 0.5f);
            result.B = 0.5f + contrast * (value.B - 0.5f);
        }

        /// <summary>
        /// Adjusts the contrast of a color.
        /// </summary>
        /// <param name="value">The color whose contrast is to be adjusted.</param>
        /// <param name="contrast">The amount by which to adjust the contrast.</param>
        /// <returns>The adjusted color.</returns>
        public static Color3D AdjustContrast(Color3D value, double contrast)
        {
            return new Color3D(
                0.5f + contrast * (value.R - 0.5f),
                0.5f + contrast * (value.G - 0.5f),
                0.5f + contrast * (value.B - 0.5f));
        }

        /// <summary>
        /// Adjusts the saturation of a color.
        /// </summary>
        /// <param name="value">The color whose saturation is to be adjusted.</param>
        /// <param name="saturation">The amount by which to adjust the saturation.</param>
        /// <param name="result">When the method completes, contains the adjusted color.</param>
        public static void AdjustSaturation(ref Color3D value, double saturation, out Color3D result)
        {
            double grey = value.R * 0.2125f + value.G * 0.7154f + value.B * 0.0721f;

            result.R = grey + saturation * (value.R - grey);
            result.G = grey + saturation * (value.G - grey);
            result.B = grey + saturation * (value.B - grey);
        }

        /// <summary>
        /// Adjusts the saturation of a color.
        /// </summary>
        /// <param name="value">The color whose saturation is to be adjusted.</param>
        /// <param name="saturation">The amount by which to adjust the saturation.</param>
        /// <returns>The adjusted color.</returns>
        public static Color3D AdjustSaturation(Color3D value, double saturation)
        {
            double grey = value.R * 0.2125f + value.G * 0.7154f + value.B * 0.0721f;

            return new Color3D(
                grey + saturation * (value.R - grey),
                grey + saturation * (value.G - grey),
                grey + saturation * (value.B - grey));
        }

        /// <summary>
        /// Computes the premultiplied value of the provided color.
        /// </summary>
        /// <param name="value">The non-premultiplied value.</param>
        /// <param name="alpha">The color alpha.</param>
        /// <param name="result">The premultiplied result.</param>
        public static void Premultiply(ref Color3D value, double alpha, out Color3D result)
        {
            result.R = value.R * alpha;
            result.G = value.G * alpha;
            result.B = value.B * alpha;
        }

        /// <summary>
        /// Computes the premultiplied value of the provided color.
        /// </summary>
        /// <param name="value">The non-premultiplied value.</param>
        /// <param name="alpha">The color alpha.</param>
        /// <returns>The premultiplied color.</returns>
        public static Color3D Premultiply(Color3D value, double alpha)
        {
            Color3D result;
            Premultiply(ref value, alpha, out result);
            return result;
        }

        /// <summary>
        /// Adds two colors.
        /// </summary>
        /// <param name="left">The first color to add.</param>
        /// <param name="right">The second color to add.</param>
        /// <returns>The sum of the two colors.</returns>
        public static Color3D operator +(Color3D left, Color3D right)
        {
            return new Color3D(left.R + right.R, left.G + right.G, left.B + right.B);
        }

        /// <summary>
        /// Assert a color (return it unchanged).
        /// </summary>
        /// <param name="value">The color to assert (unchanged).</param>
        /// <returns>The asserted (unchanged) color.</returns>
        public static Color3D operator +(Color3D value)
        {
            return value;
        }

        /// <summary>
        /// Subtracts two colors.
        /// </summary>
        /// <param name="left">The first color to subtract.</param>
        /// <param name="right">The second color to subtract.</param>
        /// <returns>The difference of the two colors.</returns>
        public static Color3D operator -(Color3D left, Color3D right)
        {
            return new Color3D(left.R - right.R, left.G - right.G, left.B - right.B);
        }

        /// <summary>
        /// Negates a color.
        /// </summary>
        /// <param name="value">The color to negate.</param>
        /// <returns>A negated color.</returns>
        public static Color3D operator -(Color3D value)
        {
            return new Color3D(-value.R, -value.G, -value.B);
        }

        /// <summary>
        /// Scales a color.
        /// </summary>
        /// <param name="scale">The factor by which to scale the color.</param>
        /// <param name="value">The color to scale.</param>
        /// <returns>The scaled color.</returns>
        public static Color3D operator *(double scale, Color3D value)
        {
            return new Color3D(value.R * scale, value.G * scale, value.B * scale);
        }

        /// <summary>
        /// Scales a color.
        /// </summary>
        /// <param name="value">The factor by which to scale the color.</param>
        /// <param name="scale">The color to scale.</param>
        /// <returns>The scaled color.</returns>
        public static Color3D operator *(Color3D value, double scale)
        {
            return new Color3D(value.R * scale, value.G * scale, value.B * scale);
        }

        /// <summary>
        /// Modulates two colors.
        /// </summary>
        /// <param name="left">The first color to modulate.</param>
        /// <param name="right">The second color to modulate.</param>
        /// <returns>The modulated color.</returns>
        public static Color3D operator *(Color3D left, Color3D right)
        {
            return new Color3D(left.R * right.R, left.G * right.G, left.B * right.B);
        }

        /// <summary>
        /// Tests for equality between two objects.
        /// </summary>
        /// <param name="left">The first value to compare.</param>
        /// <param name="right">The second value to compare.</param>
        /// <returns><c>true</c> if <paramref name="left"/> has the same value as <paramref name="right"/>; otherwise, <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Color3D left, Color3D right)
        {
            return left.Equals(ref right);
        }

        /// <summary>
        /// Tests for inequality between two objects.
        /// </summary>
        /// <param name="left">The first value to compare.</param>
        /// <param name="right">The second value to compare.</param>
        /// <returns><c>true</c> if <paramref name="left"/> has a different value than <paramref name="right"/>; otherwise, <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Color3D left, Color3D right)
        {
            return !left.Equals(ref right);
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="Color3D"/> to <see cref="Color4"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static explicit operator Color4D(Color3D value)
        {
            return new Color4D(value.R, value.G, value.B, 1.0);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="Color3D"/> to <see cref="Vector3D"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator Vector3D(Color3D value)
        {
            return new Vector3D(value.R, value.G, value.B);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="Vector3D"/> to <see cref="Color3D"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator Color3D(Vector3D value)
        {
            return new Color3D(value.X, value.Y, value.Z);
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="System.Int32"/> to <see cref="Color3D"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static explicit operator Color3D(int value)
        {
            return new Color3D(value);
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return ToString(CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <param name="format">The format to apply to each channel element (double)</param>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public string ToString(string format)
        {
            return ToString(format, CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <param name="formatProvider">The format provider.</param>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public string ToString(IFormatProvider formatProvider)
        {
            return string.Format(formatProvider, toStringFormat, R, G, B);
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <param name="format">The format to apply to each channel element (double).</param>
        /// <param name="formatProvider">The format provider.</param>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public string ToString(string format, IFormatProvider formatProvider)
        {
            if (format == null)
                return ToString(formatProvider);

            return string.Format(formatProvider,
                                 toStringFormat,
                                 R.ToString(format, formatProvider),
                                 G.ToString(format, formatProvider),
                                 B.ToString(format, formatProvider));
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = R.GetHashCode();
                hashCode = (hashCode * 397) ^ G.GetHashCode();
                hashCode = (hashCode * 397) ^ B.GetHashCode();
                return hashCode;
            }
        }

        /// <summary>
        /// Determines whether the specified <see cref="Color3D"/> is equal to this instance.
        /// </summary>
        /// <param name="other">The <see cref="Color3D"/> to compare with this instance.</param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="Color3D"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(ref Color3D other)
        {
            return R == other.R && G == other.G && B == other.B;
        }

        /// <summary>
        /// Determines whether the specified <see cref="Color3D"/> is equal to this instance.
        /// </summary>
        /// <param name="other">The <see cref="Color3D"/> to compare with this instance.</param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="Color3D"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Color3D other)
        {
            return Equals(ref other);
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
        /// </summary>
        /// <param name="value">The <see cref="System.Object"/> to compare with this instance.</param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object value)
        {
            if (!(value is Color3D))
                return false;

            var strongValue = (Color3D)value;
            return Equals(ref strongValue);
        }


        /// <summary>
        /// Returns a new <see cref="Color3D"/> with the values of the provided color's components assigned based on their index.<para/>
        /// For example, a swizzle input of (2,2,3) on a <see cref="Color3D"/> with RGBA values of 100,20,255, will return a <see cref="Color4"/> with values 20,20,255.
        /// </summary>
        /// <param name="col">The color to use as a source for values.</param>
        /// <param name="rIndex">The axis index of the source color to use for the new red value.</param>
        /// <param name="gIndex">The axis index of the source color to use for the new green value.</param>
        /// <param name="bIndex">The axis index of the source color to use for the new blue value.</param>
        /// <returns></returns>
        public static unsafe Color3D Swizzle(Color4 col, int rIndex, int gIndex, int bIndex)
        {
            return new Color3D()
            {
                R = *(&col.R + (rIndex * sizeof(int))),
                G = *(&col.G + (gIndex * sizeof(int))),
                B = *(&col.B + (bIndex * sizeof(int))),
            };
        }
    }
}