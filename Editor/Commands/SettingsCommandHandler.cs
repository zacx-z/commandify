using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Commandify
{
    public class SettingsCommandHandler : ICommandHandler
    {
        public string Execute(List<string> args, CommandContext context)
        {
            if (args.Count == 0)
                throw new ArgumentException("No settings subcommand specified");

            string subCommand = args[0];
            var subArgs = args.Skip(1).ToList();

            switch (subCommand.ToLower())
            {
                case "list":
                    return ListSettings(context);
                case "get":
                    return GetSetting(subArgs, context);
                case "set":
                    return SetSetting(subArgs, context);
                default:
                    throw new ArgumentException($"Unknown settings subcommand: {subCommand}");
            }
        }

        private string ListSettings(CommandContext context)
        {
            var settings = new List<string>
            {
                "EditorPrefs:",
                "  autoRefresh (bool)",
                "  companyName (string)",
                "  productName (string)",
                "  scriptingRuntimeVersion (string)",
                "  selectedColorSpace (string)",
                "\nPlayerSettings:",
                "  bundleIdentifier (string)",
                "  bundleVersion (string)",
                "  defaultScreenWidth (int)",
                "  defaultScreenHeight (int)",
                "  fullScreenMode (bool)",
                "  runInBackground (bool)",
                "  defaultIsFullScreen (bool)",
                "  captureSingleScreen (bool)",
                "  usePlayerLog (bool)",
                "  resizableWindow (bool)",
                "  allowFullscreenSwitch (bool)",
                "  visibleInBackground (bool)",
                "  macRetinaSupport (bool)",
                "  defaultWebScreenWidth (int)",
                "  defaultWebScreenHeight (int)",
                "  scriptingBackend (string)",
                "  apiCompatibilityLevel (string)"
            };

            context.SetLastResult(settings);
            return string.Join("\n", settings);
        }

        private string GetSetting(List<string> args, CommandContext context)
        {
            if (args.Count == 0)
                throw new ArgumentException("Setting name required");

            string settingName = args[0];
            settingName = context.ResolveStringReference(settingName);

            object value = GetSettingValue(settingName);
            if (value == null)
                throw new ArgumentException($"Setting not found: {settingName}");

            context.SetLastResult(value);
            return $"{settingName} = {value}";
        }

        private string SetSetting(List<string> args, CommandContext context)
        {
            if (args.Count < 2)
                throw new ArgumentException("Setting name and value required");

            string settingName = args[0];
            settingName = context.ResolveStringReference(settingName);

            string value = args[1];
            value = context.ResolveStringReference(value);

            if (SetSettingValue(settingName, value))
            {
                AssetDatabase.SaveAssets();
                return $"Set {settingName} = {value}";
            }

            throw new ArgumentException($"Failed to set {settingName}");
        }

        private object GetSettingValue(string settingName)
        {
            // EditorPrefs settings
            switch (settingName.ToLower())
            {
                case "autorefresh":
                    return EditorPrefs.GetBool("kAutoRefresh");
                case "companyname":
                    return PlayerSettings.companyName;
                case "productname":
                    return PlayerSettings.productName;
                case "scriptingruntimeversion":
                    return PlayerSettings.GetScriptingBackend(BuildTargetGroup.Standalone).ToString();
                case "selectedcolorspace":
                    return PlayerSettings.colorSpace.ToString();
                case "bundleidentifier":
                    return PlayerSettings.applicationIdentifier;
                case "bundleversion":
                    return PlayerSettings.bundleVersion;
                case "defaultscreenwidth":
                    return PlayerSettings.defaultScreenWidth;
                case "defaultscreenheight":
                    return PlayerSettings.defaultScreenHeight;
                case "fullscreenmode":
                    return PlayerSettings.fullScreenMode.ToString();
                case "runinbackground":
                    return PlayerSettings.runInBackground;
                case "defaultisfullscreen":
                    return PlayerSettings.defaultIsFullScreen;
                case "capturesinglescreen":
                    return PlayerSettings.captureSingleScreen;
                case "useplayerlog":
                    return PlayerSettings.usePlayerLog;
                case "resizablewindow":
                    return PlayerSettings.resizableWindow;
                case "allowfullscreenswitch":
                    return PlayerSettings.allowFullscreenSwitch;
                case "visibleinbackground":
                    return PlayerSettings.visibleInBackground;
                case "macretinasupport":
                    return PlayerSettings.macRetinaSupport;
                case "defaultwebscreenwidth":
                    return PlayerSettings.defaultWebScreenWidth;
                case "defaultwebscreenheight":
                    return PlayerSettings.defaultWebScreenHeight;
                case "scriptingbackend":
                    return PlayerSettings.GetScriptingBackend(BuildTargetGroup.Standalone).ToString();
                case "apicompatibilitylevel":
                    return PlayerSettings.GetApiCompatibilityLevel(BuildTargetGroup.Standalone).ToString();
                default:
                    return null;
            }
        }

        private bool SetSettingValue(string settingName, string value)
        {
            try
            {
                switch (settingName.ToLower())
                {
                    case "autorefresh":
                        EditorPrefs.SetBool("kAutoRefresh", bool.Parse(value));
                        break;
                    case "companyname":
                        PlayerSettings.companyName = value;
                        break;
                    case "productname":
                        PlayerSettings.productName = value;
                        break;
                    case "bundleidentifier":
                        PlayerSettings.applicationIdentifier = value;
                        break;
                    case "bundleversion":
                        PlayerSettings.bundleVersion = value;
                        break;
                    case "defaultscreenwidth":
                        PlayerSettings.defaultScreenWidth = int.Parse(value);
                        break;
                    case "defaultscreenheight":
                        PlayerSettings.defaultScreenHeight = int.Parse(value);
                        break;
                    case "runinbackground":
                        PlayerSettings.runInBackground = bool.Parse(value);
                        break;
                    case "defaultisfullscreen":
                        PlayerSettings.defaultIsFullScreen = bool.Parse(value);
                        break;
                    case "capturesinglescreen":
                        PlayerSettings.captureSingleScreen = bool.Parse(value);
                        break;
                    case "useplayerlog":
                        PlayerSettings.usePlayerLog = bool.Parse(value);
                        break;
                    case "resizablewindow":
                        PlayerSettings.resizableWindow = bool.Parse(value);
                        break;
                    case "allowfullscreenswitch":
                        PlayerSettings.allowFullscreenSwitch = bool.Parse(value);
                        break;
                    case "visibleinbackground":
                        PlayerSettings.visibleInBackground = bool.Parse(value);
                        break;
                    case "macretinasupport":
                        PlayerSettings.macRetinaSupport = bool.Parse(value);
                        break;
                    case "defaultwebscreenwidth":
                        PlayerSettings.defaultWebScreenWidth = int.Parse(value);
                        break;
                    case "defaultwebscreenheight":
                        PlayerSettings.defaultWebScreenHeight = int.Parse(value);
                        break;
                    default:
                        return false;
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
