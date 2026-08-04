using System;
using Open.Nat;

namespace IC10_Extender.Highlight
{
    public readonly struct Style
    {
        //keys for performing a lookup against a new theme when re-theming
        public readonly string Key;
        public readonly string FallbackKey;

        //styling info
        public readonly string Foreground; 
        public readonly string Background;
        public readonly bool Italic;
        public readonly bool Bold;
        public readonly bool Underline;
        public readonly bool StrikeThrough;

        //data fields that are specific to a particular application of a style. These get propogated during re-theming
        public readonly string Link;
        public readonly Exception Error;
        public readonly IStyledString Tooltip;

        public Style(
            string key,
            string fallbackKey,
            string foreground, 
            string background = null, 
            string link = null,
            bool italic = false, 
            bool bold = false, 
            bool underline = false, 
            bool strikeThrough = false, 
            Exception error = null, 
            IStyledString tooltip = null)
        {
            Key = key;
            FallbackKey = fallbackKey;
            Foreground = foreground;
            Background = background;
            Link = link;
            Italic = italic;
            Bold = bold;
            Underline = underline;
            StrikeThrough = strikeThrough;
            Error = error;
            Tooltip = tooltip;
        }

        public Style(
            Style original, 
            string key = null, 
            string fallbackKey = null,
            string foreground = null, 
            string background = null, 
            string link = null,
            bool? italic = null, 
            bool? bold = null, 
            bool? underline = null, 
            bool? strikeThrough = null,
            Exception error = null,
            IStyledString tooltip = null
         )
        : this(
            key ?? original.Key,
            fallbackKey ?? original.FallbackKey,
            foreground ?? original.Foreground,
            background ?? original.Background,
            link ?? original.Link,
            italic ?? original.Italic,
            bold ?? original.Bold,
            underline ?? original.Underline,
            strikeThrough ?? original.StrikeThrough,
            error ?? original.Error,
            tooltip ?? original.Tooltip
        ) { }

        public Style WithError(Exception error)
        {
            return new Style(this, error: error);
        }

        public Style WithTooltip(IStyledString tooltip)
        {
            return new Style(this, tooltip: tooltip);
        }

        public Style WithLink(string link)
        {
            return new Style(this, link:link);
        }

        public Style WithData(string link, Exception error, IStyledString tooltip)
        {
            return new Style(this, link:link, error: error, tooltip: tooltip);
        }

        public Style WithData(Style dataSource)
        {
            return new Style(this, link:dataSource.Link, error: dataSource.Error, tooltip: dataSource.Tooltip);
        }

        public bool HasData => Link != null || Error != null || Tooltip != null;

        public override string ToString()
        {
            return $"{nameof(Style)}[{nameof(Key)}:\"{Key}\", {nameof(FallbackKey)}:\"{FallbackKey}\", {nameof(Foreground)}:\"{Foreground}\", {nameof(Background)}:\"{Background}\", {nameof(Link)}:\"{Link}\", {nameof(Italic)}:{Italic}, {nameof(Bold)}:{Bold}, {nameof(Underline)}:{Underline}, {nameof(StrikeThrough)}:{StrikeThrough}, {nameof(Tooltip)}:{Tooltip}, {nameof(Error)}:{Error}]";
        }

        public string ApplyVanillaFormat(string text)
        {
            var result = text;
            if (Foreground != null) result = $"<color={Foreground}>{result}</color>";
            if (Link != null) result = $"<link={string.Format(Link, text)}>{result}</link>";
            if (Italic) result = $"<i>{result}</i>";
            if (Bold) result = $"<b>{result}</b>";
            if (Underline) result = $"<u>{result}</u>";
            if (StrikeThrough) result = $"<s>{result}</s>";
            return result;
        }
    }
}
