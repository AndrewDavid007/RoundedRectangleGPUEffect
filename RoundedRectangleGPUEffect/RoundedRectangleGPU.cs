using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Reflection;
using System.Drawing.Text;
using System.Windows.Forms;
using System.IO.Compression;
using System.Drawing.Drawing2D;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Registry = Microsoft.Win32.Registry;
using RegistryKey = Microsoft.Win32.RegistryKey;
using PaintDotNet;
using PaintDotNet.AppModel;
using PaintDotNet.Effects;
using PaintDotNet.Clipboard;
using PaintDotNet.IndirectUI;
using PaintDotNet.Collections;
using PaintDotNet.PropertySystem;
using PaintDotNet.Rendering;
using ColorWheelControl = PaintDotNet.ColorBgra;
using AngleControl = System.Double;
using PanSliderControl = PaintDotNet.Rendering.Vector2Double;
using FolderControl = System.String;
using FilenameControl = System.String;
using ReseedButtonControl = System.Byte;
using RollControl = PaintDotNet.Rendering.Vector3Double;
using IntSliderControl = System.Int32;
using CheckboxControl = System.Boolean;
using TextboxControl = System.String;
using DoubleSliderControl = System.Double;
using ListBoxControl = System.Byte;
using RadioButtonControl = System.Byte;
using MultiLineTextboxControl = System.String;
using LabelComment = System.String;

[assembly: AssemblyTitle("RoundedRectangleGPU plugin for Paint.NET")]
[assembly: AssemblyDescription("Render a Rounded Rectangle")]
[assembly: AssemblyConfiguration("codelab|rounded|andrewdavid|rectangle|")]
[assembly: AssemblyCompany("AndrewDavid")]
[assembly: AssemblyProduct("RoundedRectangleGPU")]
[assembly: AssemblyCopyright("Copyright ©2023 by AndrewDavid")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]
[assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyMetadata("BuiltByCodeLab", "Version=6.10.8685.36729")]
[assembly: SupportedOSPlatform("Windows")]

namespace RoundedRectangleGPUEffect
{
    public class RoundedRectangleGPUSupportInfo : IPluginSupportInfo
    {
        public string Author => base.GetType().Assembly.GetCustomAttribute<AssemblyCopyrightAttribute>().Copyright;
        public string Copyright => base.GetType().Assembly.GetCustomAttribute<AssemblyDescriptionAttribute>().Description;
        public string DisplayName => base.GetType().Assembly.GetCustomAttribute<AssemblyProductAttribute>().Product;
        public Version Version => base.GetType().Assembly.GetName().Version;
        public Uri WebsiteUri => new Uri("https://jmbondrlc007.wixsite.com/andrewdavid");
    }

    [PluginSupportInfo<RoundedRectangleGPUSupportInfo>(DisplayName = "RoundedRectangleGPU")]
    public class RoundedRectangleGPUEffectPlugin : PropertyBasedEffect
    {
        public static string StaticName => "RoundedRectangleGPU";
        public static Image StaticIcon => new Bitmap(typeof(RoundedRectangleGPUEffectPlugin), "RoundedRectangleGPU.png");
        public static string SubmenuName => "AndrewDavid VS2022 Builds";

        public RoundedRectangleGPUEffectPlugin()
            : base(StaticName, StaticIcon, SubmenuName, new EffectOptions { Flags = EffectFlags.Configurable })
        {
        }

        public enum PropertyNames
        {
            Amount1,
            Amount2,
            Amount3,
            Amount4,
            Amount5,
            Amount6
        }


        protected override PropertyCollection OnCreatePropertyCollection()
        {
            ColorBgra PrimaryColor = EnvironmentParameters.PrimaryColor.NewAlpha(byte.MaxValue);
            ColorBgra SecondaryColor = EnvironmentParameters.SecondaryColor.NewAlpha(byte.MaxValue);

            List<Property> props = new List<Property>();

            props.Add(new Int32Property(PropertyNames.Amount1, 50, 0, 500));
            props.Add(new Int32Property(PropertyNames.Amount2, 400, 2, 1000));
            props.Add(new Int32Property(PropertyNames.Amount3, 300, 2, 1000));
            props.Add(new Int32Property(PropertyNames.Amount4, 2, 1, 25));
            props.Add(new Int32Property(PropertyNames.Amount5, ColorBgra.ToOpaqueInt32(PrimaryColor), 0, 0xffffff));
            props.Add(new Int32Property(PropertyNames.Amount6, 255, 0, 255));

            return new PropertyCollection(props);
        }

        protected override ControlInfo OnCreateConfigUI(PropertyCollection props)
        {
            ControlInfo configUI = CreateDefaultConfigUI(props);

            configUI.SetPropertyControlValue(PropertyNames.Amount1, ControlInfoPropertyNames.DisplayName, "Radius");
            configUI.SetPropertyControlValue(PropertyNames.Amount1, ControlInfoPropertyNames.ShowHeaderLine, false);
            configUI.SetPropertyControlValue(PropertyNames.Amount2, ControlInfoPropertyNames.DisplayName, "Width");
            configUI.SetPropertyControlValue(PropertyNames.Amount2, ControlInfoPropertyNames.ShowHeaderLine, false);
            configUI.SetPropertyControlValue(PropertyNames.Amount3, ControlInfoPropertyNames.DisplayName, "Height");
            configUI.SetPropertyControlValue(PropertyNames.Amount3, ControlInfoPropertyNames.ShowHeaderLine, false);
            configUI.SetPropertyControlValue(PropertyNames.Amount4, ControlInfoPropertyNames.DisplayName, "Line Width");
            configUI.SetPropertyControlValue(PropertyNames.Amount4, ControlInfoPropertyNames.ShowHeaderLine, false);
            configUI.SetPropertyControlValue(PropertyNames.Amount5, ControlInfoPropertyNames.DisplayName, "Line Color");
            configUI.SetPropertyControlType(PropertyNames.Amount5, PropertyControlType.ColorWheel);
            configUI.SetPropertyControlValue(PropertyNames.Amount5, ControlInfoPropertyNames.ShowHeaderLine, false);
            configUI.SetPropertyControlValue(PropertyNames.Amount6, ControlInfoPropertyNames.DisplayName, "");
            configUI.SetPropertyControlValue(PropertyNames.Amount6, ControlInfoPropertyNames.ShowHeaderLine, false);

            return configUI;
        }

        protected override void OnCustomizeConfigUIWindowProperties(PropertyCollection props)
        {
            // Change the effect's window title
            props[ControlInfoPropertyNames.WindowTitle].Value = "RoundedRectangleGPU";
            // Add help button to effect UI
            props[ControlInfoPropertyNames.WindowHelpContentType].Value = WindowHelpContentType.PlainText;
            props[ControlInfoPropertyNames.WindowHelpContent].Value = "RoundedRectangleGPU v1.0\nCopyright ©2023 by AndrewDavid\nAll rights reserved.";
            base.OnCustomizeConfigUIWindowProperties(props);
        }

        protected override void OnSetRenderInfo(PropertyBasedEffectConfigToken token, RenderArgs dstArgs, RenderArgs srcArgs)
        {
            Amount1 = token.GetProperty<Int32Property>(PropertyNames.Amount1).Value;
            Amount2 = token.GetProperty<Int32Property>(PropertyNames.Amount2).Value;
            Amount3 = token.GetProperty<Int32Property>(PropertyNames.Amount3).Value;
            Amount4 = token.GetProperty<Int32Property>(PropertyNames.Amount4).Value;
            Amount5 = ColorBgra.FromOpaqueInt32(token.GetProperty<Int32Property>(PropertyNames.Amount5).Value);
            Amount6 = token.GetProperty<Int32Property>(PropertyNames.Amount6).Value;

            base.OnSetRenderInfo(token, dstArgs, srcArgs);
        }

        protected override unsafe void OnRender(Rectangle[] rois, int startIndex, int length)
        {
            if (length == 0) return;
            for (int i = startIndex; i < startIndex + length; ++i)
            {
                Render(DstArgs.Surface,SrcArgs.Surface,rois[i]);
            }
        }

        #region User Entered Code
        // Name: RoundedRectangleGPU
        // Submenu: AndrewDavid VS2022 Builds
        // Author: AndrewDavid
        // Title: RoundedRectangleGPU
        // Version: 1.0
        // Desc: Render a Rounded Rectangle
        // Keywords:codelab|rounded|AndrewDavid|rectangle|
        // URL:https://jmbondrlc007.wixsite.com/andrewdavid
        // Help:https://boltbait.com/pdn/codelab/

        #region UICode
        IntSliderControl Amount1 = 50; // [0,500] Radius
        IntSliderControl Amount2 = 400; // [2,1000] Width
        IntSliderControl Amount3 = 300; // [2,1000] Height
        IntSliderControl Amount4 = 2; // [1,25] Line Width
        ColorWheelControl Amount5 = ColorBgra.FromBgr(0, 0, 0); // [PrimaryColor] Line Color
        IntSliderControl Amount6 = 255; // [0,255]
        #endregion

        void Render(Surface dst, Surface src, Rectangle rect)
        {
            Rectangle selection = EnvironmentParameters.GetSelectionAsPdnRegion().GetBoundsInt();
            float centerX = ((selection.Right - selection.Left) / 2f) + selection.Left;
            float centerY = ((selection.Bottom - selection.Top) / 2f) + selection.Top;
            float left = centerX - Amount2 / 2f;
            float right = centerX + Amount2 / 2f;
            float top = centerY - Amount3 / 2f;
            float bottom = centerY + Amount3 / 2f;
            float radiusMax = Math.Min(Amount2, Amount3) / 2f;
            float radius = (Amount1 > radiusMax) ? radiusMax : Amount1;
            float radiusNub = radius / 2f;

            PointF[] points = new PointF[31];
            points[0] = new PointF(left, top + radius);
            points[1] = new PointF(left, top + radiusNub);
            points[2] = new PointF(left + radiusNub, top);
            points[3] = new PointF(left + radius, top);
            points[4] = new PointF(centerX, top);
            points[5] = new PointF(centerX, top);
            points[6] = new PointF(right - radius, top);
            points[7] = new PointF(right - radiusNub, top);
            points[8] = new PointF(right, top + radiusNub);
            points[9] = new PointF(right, top + radius);
            points[10] = new PointF(right, centerY);
            points[11] = new PointF(right, centerY);
            points[12] = new PointF(right, bottom - radius);
            points[13] = new PointF(right, bottom - radiusNub);
            points[14] = new PointF(right - radiusNub, bottom);
            points[15] = new PointF(right - radius, bottom);
            points[16] = new PointF(centerX, bottom);
            points[17] = new PointF(centerX, bottom);
            points[18] = new PointF(left + radius, bottom);
            points[19] = new PointF(left + radiusNub, bottom);
            points[20] = new PointF(left, bottom - radiusNub);
            points[21] = new PointF(left, bottom - radius);
            points[22] = new PointF(left, centerY);
            points[23] = new PointF(left, centerY);
            points[24] = new PointF(left, top + radius);
            // repeat existing points to prevent a gap
            points[25] = new PointF(left, top + radiusNub);
            points[26] = new PointF(left + radiusNub, top);
            points[27] = new PointF(left + radius, top);
            points[28] = new PointF(centerX, top);
            points[29] = new PointF(centerX, top);
            points[30] = new PointF(right - radius, top);

            dst.CopySurface(src, rect.Location, rect);

            using (RenderArgs ra = new RenderArgs(dst))
            {
                Graphics roundedRect = ra.Graphics;
                roundedRect.SmoothingMode = SmoothingMode.AntiAlias;
                roundedRect.Clip = new Region(rect);

                using (Pen roundedRectPen = new Pen(Color.FromArgb(Amount6, Amount5), Amount4))
                {
                    roundedRect.DrawBeziers(roundedRectPen, points);
                }
            }
        }

        #endregion
    }
}
