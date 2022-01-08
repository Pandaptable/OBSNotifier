﻿using OBSNotifier.Plugins;
using OBSWebsocketDotNet;
using OBSWebsocketDotNet.Types;
using System;
using System.Collections.Generic;

namespace OBSNotifier
{
    internal class NotificationDescriptionAttribute : Attribute
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public NotificationDescriptionAttribute(string name, string desc = null)
        {
            Name = name;
            Description =desc;
        }
    }

    [Flags]
    public enum NotificationType : ulong
    {
        None = 0,

        [NotificationDescription("Connected to OBS")]
        Connected = 1L << 0,
        [NotificationDescription("Disconnected from OBS")]
        Disconnected = 1L << 1,

        [NotificationDescription("Replay Started")]
        ReplayStarted = 1L << 2,
        [NotificationDescription("Replay Stopped")]
        ReplayStopped = 1L << 3,
        [NotificationDescription("Replay Saved")]
        ReplaySaved = 1L << 4,

        [NotificationDescription("Recording Started")]
        RecordingStarted = 1L << 5,
        [NotificationDescription("Recording Stopped")]
        RecordingStopped = 1L << 6,
        [NotificationDescription("Recording Paused")]
        RecordingPaused = 1L << 7,
        [NotificationDescription("Recording Resumed")]
        RecordingResumed = 1L << 8,

        [NotificationDescription("Streaming Started")]
        StreamingStarted = 1L << 9,
        [NotificationDescription("Streaming Stopped")]
        StreamingStopped = 1L << 10,

        [NotificationDescription("Virtual Camera Started")]
        VirtualCameraStarted = 1L << 11,
        [NotificationDescription("Virtual Camera Stopped")]
        VirtualCameraStopped = 1L << 12,

        [NotificationDescription("Scene Switched", "Current: {0}")]
        SceneSwitched = 1L << 24,
        [NotificationDescription("Scene Collection Switched", "Current: {0}")]
        SceneCollectionSwitched = 1L << 25,

        [NotificationDescription("Profile Switched", "Current: {0}")]
        ProfileSwitched = 1L << 32,

        [NotificationDescription("Audio is Muted", "Source: {0}")]
        AudioSourceMuted = 1L << 34,
        [NotificationDescription("Audio is Turned On", "Source: {0}")]
        AudioSourceUnmuted = 1L << 35,

        Minimal = ReplaySaved |
            RecordingStarted | RecordingStopped |
            StreamingStarted | StreamingStopped |
            SceneSwitched |
            AudioSourceMuted | AudioSourceUnmuted,

        All = Connected | Disconnected |
            ReplayStarted | ReplayStopped | ReplaySaved |
            RecordingPaused | RecordingResumed | RecordingStarted | RecordingStopped |
            StreamingStarted | StreamingStopped |
            VirtualCameraStarted | VirtualCameraStopped |
            SceneSwitched | SceneCollectionSwitched |
            ProfileSwitched |
            AudioSourceMuted | AudioSourceUnmuted,
    }

    internal class NotificationManager
    {
        readonly OBSWebsocket obs;
        readonly App app;

        PluginManager.PluginData CurrentPlugin { get => App.plugins.CurrentPlugin; }

        #region NotifData
        static List<NotificationType> SkipNotifTypes = new List<NotificationType> {
            NotificationType.None,
            NotificationType.Minimal,
            NotificationType.All
        };

        static Dictionary<NotificationType, NotificationDescriptionAttribute> notificationsData;
        public static Dictionary<NotificationType, NotificationDescriptionAttribute> NotificationsData
        {
            get
            {
                if (notificationsData == null)
                {
                    notificationsData = new Dictionary<NotificationType, NotificationDescriptionAttribute>();
                    var type = typeof(NotificationType);
                    foreach (NotificationType e in Enum.GetValues(type))
                    {
                        if (SkipNotifTypes.Contains(e))
                            continue;

                        var member = type.GetMember(e.ToString())[0];
                        notificationsData.Add(e, Attribute.GetCustomAttribute(member, typeof(NotificationDescriptionAttribute)) as NotificationDescriptionAttribute);
                    }
                }

                return notificationsData;
            }
        }
        #endregion

        public NotificationManager(App app, OBSWebsocket obs)
        {
            this.obs = obs;
            this.app = app;

            obs.Connected += Obs_Connected;
            obs.Disconnected += Obs_Disconnected;

            obs.RecordingStateChanged +=Obs_RecordingStateChanged;
            obs.RecordingPaused += Obs_RecordingPaused;
            obs.RecordingResumed += Obs_RecordingResumed;

            obs.StreamingStateChanged += Obs_StreamingStateChanged;

            obs.ReplayBufferStateChanged += Obs_ReplayBufferStateChanged;

            obs.VirtualCameraStarted += Obs_VirtualCameraStarted;
            obs.VirtualCameraStopped += Obs_VirtualCameraStopped;

            obs.SceneChanged += Obs_SceneChanged;
            obs.SceneCollectionChanged += Obs_SceneCollectionChanged;

            obs.ProfileChanged += Obs_ProfileChanged;

            obs.SourceMuteStateChanged += Obs_SourceMuteStateChanged;
        }

        #region Utils
        void InvokeNotif(Action act)
        {
            if (IsDisabled()) return;
            app.InvokeAction(act);
        }

        bool IsDisabled()
        {
            return Settings.Instance.IsPreviewShowing || CurrentPlugin.plugin == null;
        }

        bool IsActive(NotificationType type)
        {
            if (CurrentPlugin.plugin != null)
            {
                var notifs = Settings.Instance.CurrentPluginSettings.ActiveNotificationTypes??App.plugins.CurrentPlugin.plugin.DefaultActiveNotifications;
                return notifs.HasFlag(type);
            }
            return false;
        }

        void ShowNotifDefault(NotificationType type, Func<string, object[], string> formatter, params object[] origData)
        {
            if (IsActive(type))
                CurrentPlugin.plugin.ShowNotification(type, NotificationsData[type].Name, formatter(NotificationsData[type].Description, origData), origData.Length == 0 ? null : origData);
        }

        void ShowNotifDefault(NotificationType type)
        {
            if (IsActive(type))
                CurrentPlugin.plugin.ShowNotification(type, NotificationsData[type].Name, NotificationsData[type].Description);
        }
        #endregion

        #region OBS Connection
        private void Obs_Connected(object sender, EventArgs e)
        {
            InvokeNotif(() => ShowNotifDefault(NotificationType.Connected));
        }

        private void Obs_Disconnected(object sender, EventArgs e)
        {
            InvokeNotif(() => ShowNotifDefault(NotificationType.Disconnected));
        }
        #endregion

        #region Recording
        private void Obs_RecordingPaused(object sender, EventArgs e)
        {
            InvokeNotif(() => ShowNotifDefault(NotificationType.RecordingPaused));
        }

        private void Obs_RecordingResumed(object sender, EventArgs e)
        {
            InvokeNotif(() => ShowNotifDefault(NotificationType.RecordingResumed));
        }

        private void Obs_RecordingStateChanged(OBSWebsocket sender, OutputState type)
        {
            InvokeNotif(() =>
            {
                switch (type)
                {
                    case OutputState.Started:
                        ShowNotifDefault(NotificationType.RecordingStarted);
                        break;
                    case OutputState.Stopped:
                        ShowNotifDefault(NotificationType.RecordingStopped);
                        break;
                }
            });
        }
        #endregion

        #region Streaming
        private void Obs_StreamingStateChanged(OBSWebsocket sender, OutputState type)
        {
            InvokeNotif(() =>
            {
                switch (type)
                {
                    case OutputState.Started:
                        ShowNotifDefault(NotificationType.StreamingStarted);
                        break;
                    case OutputState.Stopped:
                        ShowNotifDefault(NotificationType.StreamingStopped);
                        break;
                }
            });
        }
        #endregion

        #region Replays
        private void Obs_ReplayBufferStateChanged(OBSWebsocket sender, OutputState type)
        {
            InvokeNotif(() =>
            {
                switch (type)
                {
                    case OutputState.Started:
                        ShowNotifDefault(NotificationType.ReplayStarted);
                        break;
                    case OutputState.Stopped:
                        ShowNotifDefault(NotificationType.ReplayStopped);
                        break;
                    case OutputState.Saved:
                        ShowNotifDefault(NotificationType.ReplaySaved);
                        break;
                }
            });
        }
        #endregion

        #region Virtual Camera
        private void Obs_VirtualCameraStarted(object sender, EventArgs e)
        {
            InvokeNotif(() => ShowNotifDefault(NotificationType.VirtualCameraStarted));
        }

        private void Obs_VirtualCameraStopped(object sender, EventArgs e)
        {
            InvokeNotif(() => ShowNotifDefault(NotificationType.VirtualCameraStopped));
        }
        #endregion

        #region Scenes
        private void Obs_SceneChanged(OBSWebsocket sender, string newSceneName)
        {
            InvokeNotif(() => ShowNotifDefault(NotificationType.SceneSwitched, (s, o) => string.Format(s, o[0]), newSceneName));
        }

        private void Obs_SceneCollectionChanged(object sender, EventArgs e)
        {
            InvokeNotif(() => ShowNotifDefault(NotificationType.SceneCollectionSwitched, (s, o) => string.Format(s, o[0]), obs.GetCurrentSceneCollection()));
        }
        #endregion

        #region Profiles
        private void Obs_ProfileChanged(object sender, EventArgs e)
        {
            InvokeNotif(() => ShowNotifDefault(NotificationType.ProfileSwitched, (s, o) => string.Format(s, o[0]), obs.GetCurrentProfile()));
        }
        #endregion

        #region Audio
        private void Obs_SourceMuteStateChanged(OBSWebsocket sender, string sourceName, bool muted)
        {
            if (muted)
                InvokeNotif(() => ShowNotifDefault(NotificationType.AudioSourceMuted, (s, o) => string.Format(s, o[0]), sourceName));
            else
                InvokeNotif(() => ShowNotifDefault(NotificationType.AudioSourceUnmuted, (s, o) => string.Format(s, o[0]), sourceName));
        }
        #endregion
    }
}
