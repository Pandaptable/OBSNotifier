﻿using System;

namespace OBSNotifier.Plugins
{
    [Flags]
    public enum DefaultPluginSettings : long
    {
        None = 0,
        /// <summary>
        /// ComboBox with options
        /// </summary>
        Options = 1 << 0,
        /// <summary>
        /// X,Y relative offset Sliders
        /// </summary>
        Offset = 1 << 1,
        /// <summary>
        /// Fade delay Slider
        /// </summary>
        FadeDelay = 1 << 2,
        /// <summary>
        /// Additional data TextBox
        /// </summary>
        AdditionalData = 1 << 3,
        /// <summary>
        /// Custom settings Button
        /// </summary>
        CustomSettings = 1 << 4,
        /// <summary>
        /// Button to fix/format an Additional data string
        /// </summary>
        AdditionalDataFix = 1 << 5,

        All = Options | Offset | FadeDelay | AdditionalData | CustomSettings | AdditionalDataFix,
        AllNoCustomSettings = Options | Offset | FadeDelay | AdditionalData | AdditionalDataFix,
    }
}
