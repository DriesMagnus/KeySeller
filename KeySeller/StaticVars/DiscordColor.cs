using Discord;

namespace KeySeller.StaticVars
{
    public struct DiscordColor
    {
        public Color Red => new Discord.Color(255, 0, 0);
        public Color Green => new Discord.Color(0, 255, 0);
        public Color Orange => new Discord.Color(255, 170, 0);
        public Color LightBlue => new Discord.Color(0, 187, 255);
    }
}