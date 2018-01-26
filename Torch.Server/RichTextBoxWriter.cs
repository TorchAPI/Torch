using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Torch.Server
{
    public class RichTextBoxWriter : TextWriter
    {
        private RichTextBox textbox;
        private StringBuilder _cache = new StringBuilder();
        public RichTextBoxWriter(RichTextBox textbox)
        {
            this.textbox = textbox;
            textbox.Document.Background = new SolidColorBrush(UnpackColor(Console.BackgroundColor));
            textbox.Document.Blocks.Clear();
            textbox.Document.Blocks.Add(new Paragraph {LineHeight = 12});
        }

        public override void Write(char value)
        {
            if (value == '\r')
                return;

            _cache.Append(value);
            if (value == '\n')
            {
                var str = _cache.ToString();
                _cache.Clear();

                var brush = _brushes[Console.ForegroundColor];
                textbox.Dispatcher.BeginInvoke(() =>
                {
                    var p = (Paragraph)textbox.Document.Blocks.FirstBlock;
                    p.Inlines.Add(new Run(str) { Foreground = brush });
                    textbox.ScrollToEnd();
                });
            }

        }

        public override void Write(string value)
        {
            var brush = _brushes[Console.ForegroundColor];
            textbox.Dispatcher.BeginInvoke(() =>
            {
                var p = (Paragraph)textbox.Document.Blocks.FirstBlock;
                p.Inlines.Add(new Run(value) { Foreground = brush });
                textbox.ScrollToEnd();
            });
        }

        public override Encoding Encoding
        {
            get { return Encoding.ASCII; }
        }

        static RichTextBoxWriter()
        {
            foreach (var value in (ConsoleColor[])Enum.GetValues(typeof(ConsoleColor)))
            {
                _brushes.Add(value, new SolidColorBrush(UnpackColor(value)));
            }
        }

        private static Dictionary<ConsoleColor, SolidColorBrush> _brushes = new Dictionary<ConsoleColor, SolidColorBrush>();

        private static Color UnpackColor(ConsoleColor color)
        {
            var colorByte = (byte)color;
            var isBright = (colorByte & 0b1000) >> 3 > 0;
            var brightness = isBright ? (byte)255 : (byte)128;
            var red = (colorByte & 0b0100) >> 2;
            var green = (colorByte & 0b0010) >> 1;
            var blue = (colorByte & 0b0001);

            return new Color
            {
                R = (byte)(brightness * red),
                G = (byte)(brightness * green),
                B = (byte)(brightness * blue),
                A = 255
            };
        }
    }
}