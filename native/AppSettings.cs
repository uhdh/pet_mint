using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace BunnyPet
{
    [DataContract]
    public sealed class AppSettings
    {
        [DataMember(Name = "alwaysOnTop", EmitDefaultValue = false)]
        private bool? alwaysOnTop;

        [DataMember(Name = "autoStart", EmitDefaultValue = false)]
        private bool? autoStart;

        [DataMember(Name = "restRemindersEnabled", EmitDefaultValue = false)]
        private bool? restRemindersEnabled;

        [DataMember(Name = "affinity", EmitDefaultValue = false)]
        private int? affinity;

        [DataMember(Name = "emojiFrequency", EmitDefaultValue = false)]
        private int? emojiFrequency;

        [DataMember(Name = "purrFrequency", EmitDefaultValue = false)]
        private int? purrFrequency;

        public AppSettings()
        {
            alwaysOnTop = false;
            autoStart = false;
            restRemindersEnabled = true;
            affinity = 10;
            emojiFrequency = 0;
            purrFrequency = 0;
        }

        [IgnoreDataMember]
        public bool AlwaysOnTop
        {
            get { return alwaysOnTop ?? false; }
            set { alwaysOnTop = value; }
        }

        [IgnoreDataMember]
        public bool AutoStart
        {
            get { return autoStart ?? false; }
            set { autoStart = value; }
        }

        [IgnoreDataMember]
        public bool RestRemindersEnabled
        {
            get { return restRemindersEnabled ?? true; }
            set { restRemindersEnabled = value; }
        }

        [IgnoreDataMember]
        public int Affinity
        {
            get { return affinity ?? 10; }
            set { affinity = value; }
        }

        [IgnoreDataMember]
        public int EmojiFrequency
        {
            get { return emojiFrequency ?? 0; }
            set { emojiFrequency = Math.Max(0, Math.Min(3, value)); }
        }

        [IgnoreDataMember]
        public int PurrFrequency
        {
            get { return purrFrequency ?? 0; }
            set { purrFrequency = Math.Max(0, Math.Min(3, value)); }
        }

        public static string FilePath
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "MyBunnyDesktopPet",
                    "settings.json");
            }
        }

        public static AppSettings Parse(string text)
        {
            if (String.IsNullOrWhiteSpace(text)) return new AppSettings();
            try
            {
                using (var input = new MemoryStream(Encoding.UTF8.GetBytes(text)))
                {
                    var value = (AppSettings)new DataContractJsonSerializer(typeof(AppSettings)).ReadObject(input);
                    return value ?? new AppSettings();
                }
            }
            catch (Exception)
            {
                return new AppSettings();
            }
        }

        public static string Serialize(AppSettings settings)
        {
            if (settings == null) settings = new AppSettings();
            using (var output = new MemoryStream())
            {
                new DataContractJsonSerializer(typeof(AppSettings)).WriteObject(output, settings);
                return Encoding.UTF8.GetString(output.ToArray());
            }
        }

        public static AppSettings Load()
        {
            try
            {
                return Parse(File.ReadAllText(FilePath));
            }
            catch (Exception)
            {
                return new AppSettings();
            }
        }

        public void Save()
        {
            SaveTo(FilePath);
        }

        public void SaveTo(string path)
        {
            var directory = Path.GetDirectoryName(path);
            var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                if (!String.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                File.WriteAllText(temporary, Serialize(this), new UTF8Encoding(false));
                if (File.Exists(path)) File.Replace(temporary, path, null);
                else File.Move(temporary, path);
            }
            finally
            {
                if (File.Exists(temporary)) File.Delete(temporary);
            }
        }

        public static AppSettings LoadFrom(string path)
        {
            return Parse(File.ReadAllText(path));
        }
    }
}
