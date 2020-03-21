namespace RageHelper
{
    class Config
    {
        public string RageServer { get; set; }
        public ConfigParam Server { get; set; }
        public ConfigParam Client { get; set; }
    }

    class ConfigParam
    {
        public string[] Filter { get; set; }
        public string[] Exclude { get; set; }
        public string From { get; set; }
        public string To { get; set; }
    }
}
