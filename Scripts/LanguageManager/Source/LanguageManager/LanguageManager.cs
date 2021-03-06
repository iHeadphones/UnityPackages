﻿// comment this line out if you don't have TextMeshPro in your Project

#define TEXT_MESH_PRO

using System;
using System.IO;
using System.Threading;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using FK.JSON;
using FK.Utility;
#if TEXT_MESH_PRO
using TMPro;

#endif

namespace FK.JLoc
{
    /// <summary>
    /// <para>This Language Manager works without being present in any scene. Everything concerning it is static.</para>
    /// <para>It loads the strings from json files in the StreamingAssets folder. You can then set text in different languages either manually or use the language texts that manage language changes automatically</para>
    ///
    /// v1.0 02/2019
    /// Written by Fabian Kober
    /// fabian-kober@gmx.net
    /// </summary>
    public static class LanguageManager
    {
        // ######################## PROPERTIES ######################## //
        /// <summary>
        /// If this is false, the manager did not load and initialize its configuration data yet. You should wait until this is true before doing anything with this manager
        /// </summary>
        public static bool Initialized { get; private set; }

        /// <summary>
        /// True if one or more string files are loaded
        /// </summary>
        public static bool HasStrings => _strings?.Count > 0;

        /// <summary>
        /// If the manager is currently loading one ore more files, this is true
        /// </summary>
        public static bool CurrentlyLoadingStrings => _currentlyLoading > 0;

        /// <summary>
        /// The Language Code of the Current Language
        /// </summary>
        public static string CurrentLanguage
        {
            get { return _currentLanguage; }
            private set
            {
                _currentLanguage = value;
                PlayerPrefs.SetString("Lang", CurrentLanguage);
            }
        }

        /// <summary>
        /// All languages that are available in the strings file
        /// </summary>
        public static string[] Languages
        {
            get
            {
                if (_langs == null)
                {
                    if (_config != null && Initialized)
                        _langs = _config[LANGUAGES_KEY].Keys;
                    else
                        Debug.LogWarning("Trying to access Languages with no config loaded! Either the LanguageManager is not initialized yet or the config does not exist!");
                }

                return _langs;
            }
        }

        /// <summary>
        /// The language the users OS is running in
        /// </summary>
        public static string SystemLanguage
        {
            get
            {
                switch (Application.systemLanguage)
                {
                    case UnityEngine.SystemLanguage.Afrikaans:
                        return "af";
                    case UnityEngine.SystemLanguage.Arabic:
                        return "ar";
                    case UnityEngine.SystemLanguage.Basque:
                        return "eu";
                    case UnityEngine.SystemLanguage.Belarusian:
                        return "be";
                    case UnityEngine.SystemLanguage.Bulgarian:
                        return "bg";
                    case UnityEngine.SystemLanguage.Catalan:
                        return "ca";
                    case UnityEngine.SystemLanguage.Chinese:
                        return "zh";
                    case UnityEngine.SystemLanguage.Czech:
                        return "cs";
                    case UnityEngine.SystemLanguage.Danish:
                        return "da";
                    case UnityEngine.SystemLanguage.Dutch:
                        return "nl";
                    case UnityEngine.SystemLanguage.English:
                        return "en";
                    case UnityEngine.SystemLanguage.Estonian:
                        return "et";
                    case UnityEngine.SystemLanguage.Faroese:
                        return "fo";
                    case UnityEngine.SystemLanguage.Finnish:
                        return "fi";
                    case UnityEngine.SystemLanguage.French:
                        return "fr";
                    case UnityEngine.SystemLanguage.German:
                        return "de";
                    case UnityEngine.SystemLanguage.Greek:
                        return "el";
                    case UnityEngine.SystemLanguage.Hebrew:
                        return "he";
                    case UnityEngine.SystemLanguage.Hungarian:
                        return "hu";
                    case UnityEngine.SystemLanguage.Icelandic:
                        return "is";
                    case UnityEngine.SystemLanguage.Indonesian:
                        return "id";
                    case UnityEngine.SystemLanguage.Italian:
                        return "it";
                    case UnityEngine.SystemLanguage.Japanese:
                        return "ja";
                    case UnityEngine.SystemLanguage.Korean:
                        return "ko";
                    case UnityEngine.SystemLanguage.Latvian:
                        return "lv";
                    case UnityEngine.SystemLanguage.Lithuanian:
                        return "lt";
                    case UnityEngine.SystemLanguage.Norwegian:
                        return "no";
                    case UnityEngine.SystemLanguage.Polish:
                        return "pl";
                    case UnityEngine.SystemLanguage.Portuguese:
                        return "pt";
                    case UnityEngine.SystemLanguage.Romanian:
                        return "ro";
                    case UnityEngine.SystemLanguage.Russian:
                        return "ru";
                    case UnityEngine.SystemLanguage.SerboCroatian:
                        return "sr";
                    case UnityEngine.SystemLanguage.Slovak:
                        return "sk";
                    case UnityEngine.SystemLanguage.Slovenian:
                        return "sl";
                    case UnityEngine.SystemLanguage.Spanish:
                        return "es";
                    case UnityEngine.SystemLanguage.Swedish:
                        return "sv";
                    case UnityEngine.SystemLanguage.Thai:
                        return "th";
                    case UnityEngine.SystemLanguage.Turkish:
                        return "tr";
                    case UnityEngine.SystemLanguage.Ukrainian:
                        return "uk";
                    case UnityEngine.SystemLanguage.Vietnamese:
                        return "vi";
                    case UnityEngine.SystemLanguage.ChineseSimplified:
                        return "zh";
                    case UnityEngine.SystemLanguage.ChineseTraditional:
                        return "zh";
                    case UnityEngine.SystemLanguage.Unknown:
                        return null;
                    default:
                        return null;
                }
            }
        }

        // ######################## PUBLIC VARS ######################## //
        /// <summary>
        /// This callback is invoked every time the language is changed
        /// </summary>
        public static Action<string> OnLanguageChanged;

        /// <summary>
        /// This callback is invoked every time a strings file is loaded
        /// </summary>
        public static Action OnStringFileLoaded;

        /// <summary>
        /// The default category that is used to look up a string when no category is provided
        /// </summary>
        public const string DEFAULT_CATEGORY = "default";

        #region KEYS_AND_NAMES

        /// <summary>
        /// Key of the Object that contains all available languages
        /// </summary>
        public const string LANGUAGES_KEY = "Languages";

        public const string CONFIG_NAME = "LanguageManagerConfig";
        public const string CONFIG_USE_SYSTEM_LANG_DEFAULT_KEY = "UseSystemLanguageAsDefault";
        public const string CONFIG_DEFAULT_LANG_KEY = "DefaultLanguageCode";
        public const string CONFIG_USE_SAVED_LANG_KEY = "UseSavedLanguage";

        #endregion

        // ######################## PRIVATE VARS ######################## //
        /// <summary>
        /// File extension of the strings file
        /// </summary>
        private const string JSON_FILE_EXTENSION = ".json";

        /// <summary>
        /// Escaped Line breaks to replace
        /// </summary>
        private static readonly string[] ESCAPED_LINE_BREAKS = {"\\r\\n", "\\r", "\\n"};

        /// <summary>
        /// The JSON Object containing all strings
        /// </summary>
        private static JSONObject _strings;

        /// <summary>
        /// All config data
        /// </summary>
        private static JSONObject _config;

        /// <summary>
        /// Backing for CurrentLanguage
        /// </summary>
        private static string _currentLanguage;

        /// <summary>
        /// All available Languages
        /// </summary>
        private static string[] _langs;

        /// <summary>
        /// Amount of files that are currently loading
        /// </summary>
        private static int _currentlyLoading = 0;


        // ######################## INITS ######################## //

        #region INIT

        /// <summary>
        /// Loads the config data and initializes the Manager on Application Start
        /// </summary>
        /// <exception cref="NullReferenceException"></exception>
        [RuntimeInitializeOnLoadMethod]
        private static void Init()
        {
            _config = new JSONObject();
#if !UNITY_EDITOR
            CoroutineHost.Instance.StartCoroutine(InitAsync());
#else
            CoroutineHost.StartTrackedCoroutine(InitAsync(), _config, "LanguageManager");
#endif
        }

        /// <summary>
        /// Loads the config async and initializes everything
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NullReferenceException"></exception>
        private static IEnumerator InitAsync()
        {
            // calculate the path to the config file
            string configPath = Path.Combine(Application.streamingAssetsPath, CONFIG_NAME + JSON_FILE_EXTENSION);

#if !UNITY_EDITOR
            yield return CoroutineHost.Instance.StartCoroutine(LoadConfigAsync(configPath));
#else
            yield return CoroutineHost.StartTrackedCoroutine(LoadConfigAsync(configPath), _config, "LanguageManager");
#endif

            // if we have a saved Language in the Player Prefs we load that
            CurrentLanguage = (_config[CONFIG_USE_SAVED_LANG_KEY].BoolValue && PlayerPrefs.HasKey("Lang")) ? PlayerPrefs.GetString("Lang") : null;

            FinishInit();
        }

        /// <summary>
        /// Loads the Config file async
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <exception cref="NullReferenceException"></exception>
        private static IEnumerator LoadConfigAsync(string path)
        {
            yield return JSONObject.LoadFromFileAsync(path, _config);

            // if we have no config file, something is wrong!
            if (_config?.IsNull ?? false)
                throw new NullReferenceException($"Could not load Language Manager Config!");


            // if the config file does not have the Languages Object, we have a problem, abort!
            if (!_config.HasField(LANGUAGES_KEY))
            {
                _config = null;
                throw new NullReferenceException($"Improper structure of config file, could not find property \"{LANGUAGES_KEY}\"");
            }
        }

        /// <summary>
        /// The last steps of the initializing process
        /// </summary>
        private static void FinishInit()
        {
            // if the CurrentLanguage is invalid or not contained in the config file, check our other options
            if (string.IsNullOrEmpty(CurrentLanguage) || !_config[LANGUAGES_KEY].HasField(CurrentLanguage))
            {
                // if we should use the system language and it is contained in the striconfigngs, use that
                if (_config[CONFIG_USE_SYSTEM_LANG_DEFAULT_KEY].BoolValue && _config[LANGUAGES_KEY].HasField(SystemLanguage))
                {
                    CurrentLanguage = SystemLanguage;
                } // if we could not use the system language, use the default language if it is contained in the config
                else if (_config[LANGUAGES_KEY].HasField(_config[CONFIG_DEFAULT_LANG_KEY].StringValue))
                {
                    CurrentLanguage = _config[CONFIG_DEFAULT_LANG_KEY].StringValue;
                }
                else // we are out of options, just use the first language
                {
                    Debug.LogWarning($"Selected language \"{CurrentLanguage}\" does not exist, using \"{Languages[0]}\" instead");
                    CurrentLanguage = Languages[0];
                }
            }

            // show that we are done initializing
            Initialized = true;
        }

        #endregion

        // ######################## FUNCTIONALITY ######################## //

        #region STRINGS_LOADING

        /// <summary>
        /// Loads the strings from a provided file
        /// </summary>
        /// <param name="path">The path to the strings file (Should be inside streaming Assets)</param>
        /// <param name="async">If true, we will load the strings asynchronously in a seperate thread</param>
        /// <param name="unloadOther">If true, all other strings will be unloaded before the new ones are loaded</param>
        public static void LoadStringsFile(string path, bool async = true, bool unloadOther = false)
        {
            ++_currentlyLoading;

            if (unloadOther || _strings == null)
                _strings = new JSONObject();


            // load the file either asynchronously or synchronously
            if (async)
            {
#if !UNITY_EDITOR
                CoroutineHost.Instance.StartCoroutine(LoadStringsAsync(path));
#else
                CoroutineHost.StartTrackedCoroutine(LoadStringsAsync(path), _strings, "LanguageManager");
#endif
            }
            else
            {
                JSONObject subStrings = new JSONObject(JSONObject.Type.OBJECT);

                // load the file synchronously
                try
                {
                    subStrings = JSONObject.LoadFromFile(path);
                }
                catch (FileNotFoundException)
                {
                    Debug.LogError($"Could not load strings from {path} because the File does not exist!");
                    throw;
                }

                ParseStrings(subStrings);
                _strings[path] = subStrings;
                --_currentlyLoading;
                OnStringFileLoaded?.Invoke();
            }
        }

        /// <summary>
        /// Loads the strings asynchronously
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <exception cref="NullReferenceException"></exception>
        private static IEnumerator LoadStringsAsync(string path)
        {
#if UNITY_EDITOR
            // in the editor keep track of how long the loading process takes
            System.Diagnostics.Stopwatch loadWatch = new System.Diagnostics.Stopwatch();
            loadWatch.Start();
#endif
            JSONObject subStrings = new JSONObject(JSONObject.Type.OBJECT);

            // wait for the JSONObject to load
            yield return JSONObject.LoadFromFileAsync(path, subStrings);

            // parse the strings asynchronously
            Thread parseThread = new Thread(() => ParseStrings(subStrings));
            parseThread.Start();

            // wait until parsing is done
            yield return new WaitWhile(() => parseThread.ThreadState == ThreadState.Running);

            _strings[path] = subStrings;
            --_currentlyLoading;
            OnStringFileLoaded?.Invoke();

#if UNITY_EDITOR
            // tell the dev how much time loading took
            Debug.Log($"Loaded strings in {System.Math.Round(loadWatch.Elapsed.TotalMilliseconds)} milliseconds!");
            loadWatch.Reset();
#endif
        }

        /// <summary>
        /// Parses the strings so linebreaks are actual line breaks
        /// </summary>
        public static void ParseStrings(JSONObject strings)
        {
            foreach (JSONObject category in strings)
            {
                foreach (JSONObject languageString in category)
                {
                    for (int i = 0; i < languageString.Count; ++i)
                    {
                        string s = languageString[i].StringValue;
                        for (int j = 0; j < ESCAPED_LINE_BREAKS.Length; ++j)
                        {
                            s = s.Replace(ESCAPED_LINE_BREAKS[j], "\n");
                        }

                        // replace escaped escape characters and Quotation marks
                        s = s.Replace("\\\"", "\"").Replace("\\\\", "\\");

                        languageString.SetField(languageString.GetKeyAt(i), s);
                    }
                }
            }
        }

        /// <summary>
        /// Unloads the strings from the provided file
        /// </summary>
        /// <param name="path">Path of the file that should be unloaded</param>
        public static void UnloadStringsFile(string path)
        {
            if (!_strings.HasField(path))
            {
                Debug.LogWarning($"Cannot unload strings file {path} because it is not loaded");
                return;
            }

            _strings.RemoveField(path);
        }

        #endregion

        /// <summary>
        /// Set the language to the provided Language if it is valid
        /// </summary>
        /// <param name="language">The Language Code of the desired Language</param>
        public static void SetLanguage(string language)
        {
            if (!Initialized)
                throw new NullReferenceException("Trying to set language while LanguageManager is not initialized yet!");

            // make sure we are lower case
            string lowerCaseLang = language.ToLower();

            // if the language is invalid or not contained in the file, we cannot change
            if (string.IsNullOrEmpty(lowerCaseLang) || !_config[LANGUAGES_KEY].HasField(lowerCaseLang))
            {
                Debug.LogError($"Cannot change language because language \"{lowerCaseLang}\" does not exist!");
                return;
            }

            // change language and notify everyone
            CurrentLanguage = lowerCaseLang;
            OnLanguageChanged?.Invoke(CurrentLanguage);
        }


        #region SET_TEXT

        /// <summary>
        /// Sets the text of the provided Text object to the correct one in the current language
        /// </summary>
        /// <param name="textField">Text Field to set the Text on</param>
        /// <param name="name">Name of the text in the strings file</param>
        /// <param name="category">Category of the text in the strings file</param>
        /// <exception cref="NullReferenceException"></exception>
        public static void SetText(Text textField, string name, string category = DEFAULT_CATEGORY)
        {
            // if the text field is null, we cannot do anything
            if (textField == null)
                return;

            string s;
            try
            {
                s = GetString(name, category);
            }
            catch (Exception)
            {
                s = "<MISSING>";
            }

            // set the text
            textField.text = s;
        }

        /// <summary>
        /// Sets the text of the provided Text object to the correct one in any language
        /// </summary>
        /// <param name="textField">Text Field to set the Text on</param>
        /// <param name="name">Name of the text in the strings file</param>
        /// <param name="category">Category of the text in the strings file</param>
        /// <param name="language">The language to use</param>
        /// <exception cref="NullReferenceException"></exception>
        public static void SetText(Text textField, string name, string category, string language)
        {
            // if the text field is null, we cannot do anything
            if (textField == null)
                return;

            string s;
            try
            {
                s = GetString(name, category, language);
            }
            catch (Exception)
            {
                s = "<MISSING>";
            }

            // set the text
            textField.text = s;
        }

#if TEXT_MESH_PRO
        /// <summary>
        /// Sets the text of the provided Text object to the correct one in the current language
        /// </summary>
        /// <param name="textField">Text Field to set the Text on</param>
        /// <param name="name">Name of the text in the strings file</param>
        /// <param name="category">Category of the text in the strings file</param>
        /// <exception cref="NullReferenceException"></exception>
        public static void SetText(TMP_Text textField, string name, string category = DEFAULT_CATEGORY)
        {
            // if the text field is null, we cannot do anything
            if (textField == null)
                return;

            string s;
            try
            {
                s = GetString(name, category);
            }
            catch (Exception)
            {
                s = "<MISSING>";
            }

            // set the text
            textField.text = s;
        }

        /// <summary>
        /// Sets the text of the provided Text object to the correct one in any language
        /// </summary>
        /// <param name="textField">Text Field to set the Text on</param>
        /// <param name="name">Name of the text in the strings file</param>
        /// <param name="category">Category of the text in the strings file</param>
        /// <param name="language">The language to use</param>
        /// <exception cref="NullReferenceException"></exception>
        public static void SetText(TMP_Text textField, string name, string category, string language)
        {
            // if the text field is null, we cannot do anything
            if (textField == null)
                return;

            string s;
            try
            {
                s = GetString(name, category, language);
            }
            catch (Exception)
            {
                s = "<MISSING>";
            }

            // set the text
            textField.text = s;
        }
#endif

        #endregion

        #region GET_STRING

        /// <summary>
        /// Returns the string in the current language
        /// </summary>
        /// <param name="name">Name of the text in the strings file</param>
        /// <param name="category">Category of the text in the strings file</param>
        /// <returns></returns>
        /// <exception cref="NullReferenceException"></exception>
        public static string GetString(string name, string category = DEFAULT_CATEGORY)
        {
            // if we are not initialized, we cannot continue
            if (!Initialized)
                throw new NullReferenceException("Trying to access strings while LanguageManager is not initialized yet!");

            // if we have no strings, we cannot continue, throw an exeption so the dev knows this won't work!
            if (!HasStrings)
                throw new NullReferenceException("Trying to access strings with no string file loaded!");

            // get the string by going through all loaded files and check whether they contain the string
            string s = null;
            int i = 0;
            while (i < _strings.Count && s == null)
            {
                s = _strings[i++][category]?[name]?[CurrentLanguage]?.StringValue;
            }

            // if the string is null, something went wrong, notify the dev with an exeption (I know devs love them)
            if (s == null)
                throw new NullReferenceException($"Could not find string \"{name}\" in language \"{CurrentLanguage}\" in category \"{category}\"");

            return s;
        }

        /// <summary>
        /// Returns the string in the provided language
        /// </summary>
        /// <param name="name">Name of the text in the strings file</param>
        /// <param name="category">Category of the text in the strings file</param>
        /// <param name="language">The language to use</param>
        /// <returns></returns>
        /// <exception cref="NullReferenceException"></exception>
        public static string GetString(string name, string category, string language)
        {
            // if we are not initialized, we cannot continue
            if (!Initialized)
                throw new NullReferenceException("Trying to access strings while LanguageManager is not initialized yet!");

            // if we have no strings, we cannot continue, throw an exeption so the dev knows this won't work!
            if (!HasStrings)
                throw new NullReferenceException("Trying to access strings with no string file loaded!");

            // make sure the language is lower case
            string lowerCaseLang = language.ToLower();

            // get the string by going through all loaded files and check whether they contain the string
            string s = null;
            int i = 0;
            while (i < _strings.Count && s == null)
            {
                s = _strings[i++][category]?[name]?[lowerCaseLang]?.StringValue;
            }

            // if the string is null, something went wrong, notify the dev with an exeption (I know devs love them)
            if (s == null)
                throw new NullReferenceException($"Could not find string \"{name}\" in language \"{lowerCaseLang}\" in category \"{category}\"");

            return s;
        }

        #endregion


        // ######################## UTILITIES ######################## //
        /// <summary>
        /// Returns the Language Name for the provided Language code if it exists in the strings file
        /// </summary>
        /// <param name="languageCode"></param>
        /// <returns></returns>
        public static string GetLanguageDisplayName(string languageCode)
        {
            // if strings is null, we cannot continue
            if (_config == null || !Initialized)
            {
                Debug.LogWarning("Trying to access Languages with no config file loaded! Either the LanguageManager did not load it yet or the file does not exist!");
                return null;
            }

            // if the language does not exist, notify the dev
            if (!_config[LANGUAGES_KEY].HasField(languageCode))
            {
                Debug.LogWarning($"Requested Language \"{languageCode}\" does not exist!");
                return null;
            }

            return _config[LANGUAGES_KEY][languageCode].StringValue;
        }
    }
}