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
        public AppSettings()
        {
            AlwaysOnTop = true;
            AutoStart = false;
        }

        [DataMember(Name = "alwaysOnTop")]
        public bool AlwaysOnTop { get; set; }

        [DataMember(Name = "autoStart")]
        public bool AutoStart { get; set; }

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
            var path = FilePath;
            var directory = Path.GetDirectoryName(path);
            var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                Directory.CreateDirectory(directory);
                File.WriteAllText(temporary, Serialize(this), new UTF8Encoding(false));
                if (File.Exists(path)) File.Replace(temporary, path, null);
                else File.Move(temporary, path);
            }
            finally
            {
                if (File.Exists(temporary)) File.Delete(temporary);
            }
        }
    }
}
