namespace RageHelper
{
    class Config
    {
        public string RageServer { get; set; }
        public ConfigShell Shell { get; set; }
        public ConfigRule[] Rules { get; set; }
    }

    class ConfigShell
    {
        public string[] Startup { get; set; }
    }

    class ConfigRule
    {
        public string[] Filter { get; set; }
        public string[] Exclude { get; set; }
        public string Observable { get; set; }
        public string Dest { get; set; }
    }
}
