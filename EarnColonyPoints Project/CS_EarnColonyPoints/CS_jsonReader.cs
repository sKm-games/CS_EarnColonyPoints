using System.IO;
using Newtonsoft.Json.Linq;

namespace CS_ColonyPoints
{
    public class ConfigReader
    {
        private string configFilePath;
        private JObject configData;

        public ConfigReader(string configFilePath)
        {
            this.configFilePath = configFilePath;
            LoadConfig();
        }

        private void LoadConfig()
        {
            if (File.Exists(configFilePath))
            {
                string jsonContent = File.ReadAllText(configFilePath);
                configData = JObject.Parse(jsonContent);
            }
            else
            {
                throw new FileNotFoundException($"Configuration file not found: {configFilePath}");
            }
        }

        public string GetString(string key)
        {
            return configData[key]?.ToString();
        }

        public int GetInt(string key)
        {
            return configData[key]?.ToObject<int>() ?? 0;
        }

        public bool GetBool(string key)
        {
            return configData[key]?.ToObject<bool>() ?? false;
        }
    }
}